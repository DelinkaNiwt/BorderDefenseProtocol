using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace CharacterEditor;

internal static class PawnxTool
{
	internal static bool CanCreateZombieInSpace;

	internal static HashSet<MutantDef> AllMutantDefs => CEditor.API.Get<HashSet<MutantDef>>(EType.MutantDef);

	internal static bool IsAnimal(this Pawn p)
	{
		return p?.kindDef.IsAnimal() ?? false;
	}

	internal static string GetMutantName(this Pawn p)
	{
		return (p.HasMutantTracker() && p.mutant.Def != null) ? p.mutant.Def.LabelCap.RawText : "";
	}

	internal static string GetMutantDefName(this Pawn p)
	{
		return (p.HasMutantTracker() && p.mutant.Def != null) ? p.mutant.Def.defName : "";
	}

	internal static string GetMutantRotStage(this Pawn p)
	{
		return (p.HasMutantTracker() && p.mutant.Def != null) ? Enum.GetName(typeof(RotStage), p.mutant.rotStage) : "";
	}

	internal static string GetMutantDesc(this Pawn p)
	{
		return (p.HasMutantTracker() && p.mutant.Def != null) ? (p.mutant.Def.description + "\n\n" + p.mutant.Def.modContentPack.Name.Colorize(ColorTool.colGray)) : "";
	}

	internal static Pawn GetOtherPawnFromSeparatedString(string s)
	{
		Pawn result = null;
		if (!s.NullOrEmpty())
		{
			result = ((!s.Contains("?")) ? NameTool.GetPawnByNameSingle(NameTool.GetPawnNameFromSeparatedString(s, null)) : NameTool.GetPawnByNameTriple(NameTool.GetPawnNameFromSeparatedString(s)));
		}
		return result;
	}

	internal static void CreatePawnLord(this Pawn newPawn, IntVec3 forcePosition = default(IntVec3))
	{
		if (newPawn == null)
		{
			return;
		}
		Faction faction = newPawn.Faction;
		if (faction == null || faction == Faction.OfPlayer)
		{
			return;
		}
		Lord lord = null;
		try
		{
			if (newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction).Any((Pawn pawn) => pawn != newPawn))
			{
				Pawn p = (Pawn)GenClosest.ClosestThing_Global(newPawn.Position, newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction), 99999f, (Thing thing) => thing != newPawn && ((Pawn)thing).GetLord() != null);
				lord = p.GetLord();
			}
		}
		catch
		{
		}
		if (lord == null)
		{
			LordJob_DefendPoint lordJob = new LordJob_DefendPoint((forcePosition == default(IntVec3)) ? newPawn.Position : forcePosition);
			lord = LordMaker.MakeNewLord(faction, lordJob, Find.CurrentMap);
		}
		lord.AddPawn(newPawn);
	}

	internal static void SpawnOrPassPawn(this Pawn p, Faction f, PresetPawn ppn, IntVec3 pos = default(IntVec3), bool doPlace = false)
	{
		if (CEditor.InStartingScreen)
		{
			if (CEditor.ListName == Label.COLONISTS)
			{
				p.PostCompsAfterCreate(ppn);
			}
			else
			{
				p.PassToWorld(ppn, pos);
			}
		}
		else if (!CEditor.OnMap)
		{
			p.PassToWorld(ppn, pos);
		}
		else if (Event.current.shift)
		{
			CEditor.API.EditorMoveRight();
			PlacingTool.DropPawnWithPod(p, ppn);
		}
		else if (doPlace || (ppn != null && ppn.bPlace) || (ppn == null && Event.current.control))
		{
			CEditor.API.EditorMoveRight();
			PlacingTool.PlaceInCustomPosition(p, ppn);
		}
		else
		{
			PlacingTool.PlaceInPosition(p, ppn, pos);
		}
	}

	internal static void PostCompsAfterCreate(this Pawn p, PresetPawn ppn)
	{
		List<ThingComp> memberValue = p.GetMemberValue<List<ThingComp>>("comps", null);
		if (memberValue != null)
		{
			for (int i = 0; i < memberValue.Count; i++)
			{
				memberValue[i].PostSpawnSetup(respawningAfterLoad: true);
			}
		}
		PostProcess(p, ppn);
	}

	internal static void PassToWorld(this Pawn p, PresetPawn ppn, IntVec3 pos = default(IntVec3))
	{
		if (p == null)
		{
			return;
		}
		if (p.Faction.IsZombie())
		{
			if (!CEditor.InStartingScreen)
			{
				p.ZombieWorldCreationFixInSpace();
			}
			else
			{
				p.SpawnAsZombie(pos);
			}
		}
		else if (!Find.World.worldPawns.Contains(p))
		{
			Find.World.worldPawns.PassToWorld(p);
		}
		Dictionary<string, Faction> dicFactions = CEditor.API.DicFactions;
		if (dicFactions != null)
		{
			Faction value = dicFactions.GetValue(CEditor.ListName);
			if (value != null)
			{
				p.SetFaction(value);
			}
		}
		PostProcess(p, ppn);
	}

	internal static void SpawnPawn(this Pawn newPawn, PresetPawn ppn, IntVec3 pos = default(IntVec3))
	{
		if (newPawn == null || CEditor.InStartingScreen)
		{
			return;
		}
		try
		{
			if (newPawn.Dead && newPawn.Corpse != null && newPawn.Corpse.Spawned)
			{
				newPawn.Corpse.DeSpawn();
			}
			if (newPawn.Spawned)
			{
				newPawn.DeSpawn();
			}
			if (pos == default(IntVec3))
			{
				pos = UI.MouseCell();
				pos.x -= 5;
			}
			if (!pos.InBounds(Find.CurrentMap))
			{
				pos = Find.CurrentMap.AllCells.RandomElement();
			}
			if (newPawn.Faction.IsZombie())
			{
				newPawn.SpawnAsZombie(pos);
			}
			else
			{
				GenSpawn.Spawn(newPawn, pos, Find.CurrentMap, Rot4.South);
				newPawn.CreatePawnLord();
			}
			if (newPawn.Faction == Faction.OfPlayer)
			{
				newPawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
			}
			PostProcess(newPawn, ppn);
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
		}
	}

	internal static void PostProcess(Pawn p, PresetPawn ppn)
	{
		if (p != null)
		{
			if (p.HasPsylink)
			{
				MeditationFocusTypeAvailabilityCache.ClearFor(p);
			}
			StatsReportUtility.Reset();
			List<Pawn> list = CEditor.API.ListOf<Pawn>(EType.Pawns);
			if (!list.Contains(p))
			{
				list.Insert(0, p);
			}
			CEditor.API.Pawn = p;
			if (CEditor.InStartingScreen)
			{
				Notify_CheckStartPawnsListChanged();
			}
			if (ppn != null)
			{
				ppn.PostProcess();
				return;
			}
			Capturer capturer = CEditor.API.Get<Capturer>(EType.Capturer);
			capturer.UpdatePawnGraphic(p);
		}
	}

	internal static void SpawnAsZombie(this Pawn pawn, IntVec3 pos)
	{
		try
		{
			if (pawn.story != null)
			{
				pawn.story.traits.allTraits.Clear();
			}
			pawn.inventory.DestroyAll();
			pawn.equipment.DestroyAllEquipment();
			GenSpawn.Spawn(pawn, pos, Find.CurrentMap, Rot4.South);
			object[] param = new object[2] { pawn, false };
			Reflect.GetAType("ZombieLand", "Tools").CallMethod("ConvertToZombie", param);
			if (pawn.Dead)
			{
				pawn.Delete();
			}
		}
		catch
		{
		}
	}

	internal static void DiscardGeneratedPawn(this Pawn p)
	{
		if (p == null)
		{
			return;
		}
		try
		{
			typeof(PawnGenerator).CallMethod("DiscardGeneratedPawn", new object[1] { p });
		}
		catch
		{
		}
	}

	internal static void Delete(this Pawn p, bool force = false)
	{
		if (p == null)
		{
			return;
		}
		try
		{
			List<Pawn> list = CEditor.API.ListOf<Pawn>(EType.Pawns);
			if (!list.NullOrEmpty() && list.Contains(p))
			{
				if (!force && CEditor.InStartingScreen && list.Count == 1)
				{
					MessageTool.Show("can't delete below the minimum number", MessageTypeDefOf.RejectInput);
					return;
				}
				p.ideo = null;
				list.Remove(p);
				CEditor.API.Pawn = list.FirstOrFallback();
				if (CEditor.InStartingScreen && CEditor.ListName == Label.COLONISTS)
				{
					Notify_CheckStartPawnsListChanged();
				}
			}
			if (p.Dead && p.Corpse != null && p.Corpse.Spawned)
			{
				p.Corpse.DeSpawn();
			}
			if (p.Spawned)
			{
				p.DeSpawn();
			}
			if (!p.Destroyed)
			{
				p.Destroy();
			}
			Find.World.worldPawns.RemoveAndDiscardPawnViaGC(p);
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
		}
	}

	internal static void DeleteAllPawns(string listname, bool onMap, Faction faction)
	{
		List<Pawn> pawnList = GetPawnList(listname, onMap, faction);
		for (int num = pawnList.Count - 1; num >= 0; num--)
		{
			pawnList[num].Delete(force: true);
		}
		Notify_CheckStartPawnsListChanged();
		CEditor.API.UpdateGraphics();
	}

	internal static void Notify_CheckStartPawnsListChanged()
	{
		if (CEditor.InStartingScreen && CEditor.ListName == Label.COLONISTS)
		{
			Find.GameInitData.startingAndOptionalPawns.Clear();
			Find.GameInitData.startingAndOptionalPawns.AddRange(CEditor.API.ListOf<Pawn>(EType.Pawns));
			if (Find.GameInitData.startingPawnCount > Find.GameInitData.startingAndOptionalPawns.Count)
			{
				Find.GameInitData.startingPawnCount = Find.GameInitData.startingAndOptionalPawns.Count;
			}
		}
	}

	internal static bool FactionIsNotInsectNotMecha(Faction f)
	{
		return f != Faction.OfInsects && f != Faction.OfMechanoids;
	}

	internal static bool KeyIsHumanoid(bool notInsectNotMecha, string key)
	{
		return notInsectNotMecha && key != Label.COLONYANIMALS && key != Label.WILDANIMALS;
	}

	internal static bool KeyIsAnimal(bool notInsectNotMecha, string key, Faction f)
	{
		return notInsectNotMecha && key != Label.COLONISTS && (key == Label.COLONYANIMALS || key == Label.WILDANIMALS || (f != null && f.def.defName != "Zombies"));
	}

	internal static bool KeyIsOther(string key, Faction f)
	{
		return f != null && key != Label.COLONISTS && key != Label.COLONYANIMALS;
	}

	internal static bool FactionIsXeno(Faction f)
	{
		return f?.Name.ToLower().Contains("xenomorph") ?? false;
	}

	internal static bool IsOnMap(this Pawn p)
	{
		return p != null && (p.Spawned || (p.Corpse != null && p.Corpse.Spawned));
	}

	internal static List<Pawn> GetPawnList(string key, bool onMap, Faction f = null)
	{
		try
		{
			if (CEditor.InStartingScreen && key == Label.COLONISTS)
			{
				return Find.GameInitData.startingAndOptionalPawns.ListFullCopy();
			}
			SetPawnKindFlags(key, f, out var _, out var humanoid, out var animal, out var other, out var _, out var allFac);
			List<Pawn> list = Find.CurrentMap?.mapPawns?.AllPawns;
			if (list == null)
			{
				list = new List<Pawn>();
			}
			List<Pawn> list2 = Find.World?.worldPawns?.AllPawnsAliveOrDead;
			if (list2 == null)
			{
				list2 = new List<Pawn>();
			}
			List<Pawn> list3 = list.Concat(list2).ToList();
			if (list3 == null)
			{
				list3 = new List<Pawn>();
			}
			return (!CEditor.API.GetO(OptionB.USESORTEDPAWNLIST)) ? list3.Where((Pawn td) => (allFac || td.Faction == f) && ((humanoid && td.RaceProps.Humanlike) || (animal && td.RaceProps.Animal) || (other && !td.RaceProps.Animal && !td.RaceProps.Humanlike)) && (!onMap || td.Spawned || (td.Corpse != null && td.Corpse.Spawned))).ToList() : (from td in list3
				where (allFac || td.Faction == f) && ((humanoid && td.RaceProps.Humanlike) || (animal && td.RaceProps.Animal) || (other && !td.RaceProps.Animal && !td.RaceProps.Humanlike)) && (!onMap || td.Spawned || (td.Corpse != null && td.Corpse.Spawned))
				orderby td.Faction?.Name, td.LabelShort
				select td).ToList();
		}
		catch
		{
			return null;
		}
	}

	internal static void SetPawnKindFlags(string key, Faction f, out bool notInsectNotMecha, out bool humanoid, out bool animal, out bool other, out bool xeno, out bool allFac)
	{
		notInsectNotMecha = FactionIsNotInsectNotMecha(f);
		humanoid = KeyIsHumanoid(notInsectNotMecha, key);
		animal = KeyIsAnimal(notInsectNotMecha, key, f);
		other = KeyIsOther(key, f) && f.def.defName != "Zombies";
		xeno = FactionIsXeno(f);
		allFac = f == null;
	}

	internal static void TeleportPawn(this Pawn pawn, IntVec3 pos = default(IntVec3))
	{
		if (pawn == null)
		{
			return;
		}
		pos = ((pos == default(IntVec3)) ? UI.MouseCell() : pos);
		if (pos.InBounds(Find.CurrentMap))
		{
			if (!pawn.Spawned || !pawn.Position.InBounds(Find.CurrentMap) || (pawn.Dead && (pawn.Corpse == null || pawn.Corpse.Position.InBounds(Find.CurrentMap))))
			{
				pawn.SpawnPawn(null, pos);
			}
			if (pawn.Dead)
			{
				pawn.Corpse.Position = pos;
				return;
			}
			pawn.Position = pos;
			pawn.Notify_Teleported();
			pawn.Position = pos;
			pawn.Notify_Teleported();
		}
	}

	internal static string GetTrainabilityLabel(this Pawn p)
	{
		return "CreatureTrainability".Translate().Formatted(p.def.label) + ": " + p.RaceProps.trainability.LabelCap;
	}

	internal static void RecruitPawn(this Pawn p)
	{
		if (p != null && p.Faction != Faction.OfPlayer && p.RaceProps.Humanlike)
		{
			Pawn pawn = null;
			pawn = ((!CEditor.InStartingScreen) ? GetPawnList(Label.COLONISTS, CEditor.OnMap, Faction.OfPlayer).RandomElement() : Find.GameInitData.startingAndOptionalPawns.RandomElement());
			if (CEditor.InStartingScreen)
			{
				p.SetFaction(Faction.OfPlayer, pawn);
			}
			else
			{
				InteractionWorker_RecruitAttempt.DoRecruit(pawn, p);
			}
			if (CEditor.InStartingScreen)
			{
				Find.GameInitData.startingAndOptionalPawns.Add(p);
				CEditor.API.ListOf<Pawn>(EType.Pawns).Add(p);
			}
			else
			{
				DebugActionsUtility.DustPuffFrom(p);
			}
			p.AllowAllApparel();
		}
	}

	internal static void EnslavePawn(this Pawn p)
	{
		if (p != null && p.Faction != Faction.OfPlayer && p.RaceProps.Humanlike)
		{
			Pawn pawn = null;
			pawn = ((!CEditor.InStartingScreen) ? GetPawnList(Label.COLONISTS, CEditor.OnMap, Faction.OfPlayer).RandomElement() : Find.GameInitData.startingAndOptionalPawns.RandomElement());
			GenGuest.EnslavePrisoner(pawn, p);
			if (CEditor.InStartingScreen)
			{
				Find.GameInitData.startingAndOptionalPawns.Add(p);
				CEditor.API.ListOf<Pawn>(EType.Pawns).Add(p);
			}
			else
			{
				DebugActionsUtility.DustPuffFrom(p);
			}
			p.AllowAllApparel();
		}
	}

	internal static string GetNamesOfUsedMods(string modids)
	{
		HashSet<string> l = ModStringToHashSet(modids);
		return ModListIdsToString(l);
	}

	internal static string GetNamesOfInactiveMods(string modids)
	{
		string text = "";
		HashSet<string> hashSet = ModStringToHashSet(modids);
		try
		{
			foreach (string item in hashSet)
			{
				string text2 = item.ToLower();
				bool flag = false;
				string text3 = text2;
				foreach (ModMetaData allInstalledMod in ModLister.AllInstalledMods)
				{
					if (allInstalledMod.PackageId.ToLower().Equals(text2))
					{
						if (allInstalledMod.Active)
						{
							flag = true;
						}
						else
						{
							text3 = allInstalledMod.Name;
						}
						break;
					}
				}
				if (!flag)
				{
					text = text + text3 + "|";
				}
			}
		}
		catch
		{
		}
		return text.SubstringRemoveLast();
	}

	internal static HashSet<string> ModStringToHashSet(string s)
	{
		if (s.NullOrEmpty())
		{
			return new HashSet<string>();
		}
		HashSet<string> hashSet = new HashSet<string>();
		try
		{
			s = s.Replace("[", "").Replace("]", "");
			string[] array = s.SplitNo("|");
			string[] array2 = array;
			foreach (string item in array2)
			{
				hashSet.Add(item);
			}
		}
		catch
		{
		}
		return hashSet;
	}

	internal static string ModListIdsToString(HashSet<string> l)
	{
		string text = "";
		foreach (string item in l)
		{
			string value = item.ToLower();
			foreach (ModMetaData allInstalledMod in ModLister.AllInstalledMods)
			{
				if (allInstalledMod.PackageId.ToLower().Equals(value))
				{
					text = text + allInstalledMod.Name + "|";
					break;
				}
			}
		}
		return text.SubstringRemoveLast();
	}

	internal static string GetUsedModIds(this Pawn p)
	{
		HashSet<string> hashSet = new HashSet<string>();
		if (p != null)
		{
			try
			{
				hashSet.Add(p.kindDef.modContentPack.PackageId);
				if (p.HasStoryTracker() && p.story.bodyType != null)
				{
					hashSet.Add(p.story.bodyType.modContentPack.PackageId);
				}
				if (p.HasStoryTracker() && p.story.hairDef != null)
				{
					hashSet.Add(p.story.hairDef.modContentPack.PackageId);
				}
				if (p.HasStoryTracker())
				{
					BackstoryDef backstory = p.story.GetBackstory(BackstorySlot.Childhood);
					BackstoryDef backstory2 = p.story.GetBackstory(BackstorySlot.Adulthood);
					if (backstory.nameMaker != null)
					{
						hashSet.Add(backstory.nameMaker.modContentPack.PackageId);
					}
					if (backstory2.nameMaker != null)
					{
						hashSet.Add(backstory2.nameMaker.modContentPack.PackageId);
					}
				}
				if (p.HasEquipmentTracker())
				{
					foreach (ThingWithComps item in p.equipment.AllEquipmentListForReading)
					{
						hashSet.Add(item.def.modContentPack.PackageId);
					}
				}
				if (p.HasApparelTracker())
				{
					foreach (Apparel item2 in p.apparel.WornApparel)
					{
						hashSet.Add(item2.def.modContentPack.PackageId);
					}
				}
				if (p.HasInventoryTracker())
				{
					foreach (Thing item3 in p.inventory.innerContainer)
					{
						hashSet.Add(item3.def.modContentPack.PackageId);
					}
				}
				if (p.HasStyleTracker())
				{
					if (p.style.beardDef != null)
					{
						hashSet.Add(p.style.beardDef.modContentPack.PackageId);
					}
					if (p.style.BodyTattoo != null)
					{
						hashSet.Add(p.style.BodyTattoo.modContentPack.PackageId);
					}
					if (p.style.FaceTattoo != null)
					{
						hashSet.Add(p.style.FaceTattoo.modContentPack.PackageId);
					}
				}
				if (p.HasAbilityTracker())
				{
					foreach (Ability ability in p.abilities.abilities)
					{
						hashSet.Add(ability.def.modContentPack.PackageId);
					}
				}
				if (p.HasTraitTracker())
				{
					foreach (Trait allTrait in p.story.traits.allTraits)
					{
						hashSet.Add(allTrait.def.modContentPack.PackageId);
					}
				}
				if (p.HasHealthTracker())
				{
					foreach (Hediff hediff in p.health.hediffSet.hediffs)
					{
						hashSet.Add(hediff.def.modContentPack.PackageId);
					}
				}
				if (p.HasMemoryTracker())
				{
					foreach (Thought_Memory memory in p.needs.mood.thoughts.memories.Memories)
					{
						hashSet.Add(memory.def.modContentPack.PackageId);
					}
				}
				if (CEditor.IsGradientHairActive)
				{
					hashSet.Add("automatic.gradienthair");
				}
				if (CEditor.IsDualWieldActive)
				{
					hashSet.Add("Roolo.DualWield");
				}
				if (CEditor.IsFacialAnimationActive)
				{
					hashSet.Add("Nals.FacialAnimation");
				}
				if (CEditor.IsAlienRaceActive)
				{
					hashSet.Add("erdelf.HumanoidAlienRaces");
				}
				if (CEditor.IsCombatExtendedActive)
				{
					hashSet.Add("CETeam.CombatExtended");
				}
				if (!p.GetRoyalTitleAsSeparatedString().NullOrEmpty())
				{
					hashSet.Add(RoyalTitleDefOf.Knight.modContentPack.PackageId);
				}
			}
			catch
			{
			}
		}
		return "[" + hashSet.ToList().ListToString() + "]";
	}

	internal static bool IsZombie(this Pawn p)
	{
		return p.Faction.IsZombie();
	}

	internal static void ReplacePawnWithPawnOfSameRace(Gender gender, Faction faction)
	{
		PawnKindDef kindDef = CEditor.API.Pawn.kindDef;
		ThingDef def = CEditor.API.Pawn.def;
		IntVec3 position = CEditor.API.Pawn.Position;
		int num = 10;
		BodyTypeDef bodyTypeDef = ((gender == Gender.Female) ? BodyTypeDefOf.Female : BodyTypeDefOf.Male);
		do
		{
			num--;
			CEditor.API.Pawn.Delete();
			AddOrCreateNewPawn(kindDef, faction, def, null, position);
			if ((gender != Gender.Female || CEditor.API.Pawn.story.bodyType != BodyTypeDefOf.Male) && (gender != Gender.Male || CEditor.API.Pawn.story.bodyType != BodyTypeDefOf.Female))
			{
				if (gender == Gender.None)
				{
					break;
				}
				CEditor.API.Pawn.gender = gender;
			}
		}
		while (CEditor.API.Pawn.gender != gender && num > 0);
		if (CEditor.API.Pawn.HasStyleTracker() && gender == Gender.Female)
		{
			CEditor.API.Pawn.SetBeard(BeardDefOf.NoBeard);
		}
	}

	internal static void ForceGenderizedBodyTypeDef(Gender gender)
	{
		CEditor.API.Pawn.gender = gender;
		if (gender == Gender.Female && CEditor.API.Pawn.story.bodyType == BodyTypeDefOf.Male && IsBodyInList(BodyTypeDefOf.Female))
		{
			CEditor.API.Pawn.SetBody(BodyTypeDefOf.Female);
		}
		else if (gender == Gender.Male && CEditor.API.Pawn.story.bodyType == BodyTypeDefOf.Female && IsBodyInList(BodyTypeDefOf.Male))
		{
			CEditor.API.Pawn.SetBody(BodyTypeDefOf.Male);
		}
	}

	internal static bool IsBodyInList(BodyTypeDef b)
	{
		if (CEditor.API.Pawn == null)
		{
			return false;
		}
		List<BodyTypeDef> bodyDefList = CEditor.API.Pawn.GetBodyDefList();
		if (!bodyDefList.NullOrEmpty())
		{
			return bodyDefList.Contains(b);
		}
		return false;
	}

	public static bool IsCreep(this PawnKindDef pkd)
	{
		return pkd is CreepJoinerFormKindDef || pkd.race.defName == "CreepJoiner" || pkd.defName == "LeatheryStranger" || pkd.defName == "DealMaker" || pkd.defName == "LoneGenius" || pkd.defName == "DarkScholar" || pkd.defName == "CreepDrifter" || pkd.defName == "Blindhealer" || pkd.defName == "TimelessOne" || pkd.defName == "CultEscapee";
	}

	internal static DevelopmentalStage GetRandomDevelopmentalStage()
	{
		int num = CEditor.zufallswert.Next(30);
		if (num >= 8)
		{
			return DevelopmentalStage.Adult;
		}
		return DevelopmentalStage.Child;
	}

	internal static Pawn CreateNewPawn(PawnKindDef pkd, Faction f, ThingDef raceDef, bool forceFaction = false)
	{
		if (pkd == null)
		{
			return null;
		}
		Pawn pawn = null;
		if (!forceFaction)
		{
			f = f.ThisOrDefault();
			bool flag = pkd.IsDroidColonist();
			bool flag2 = f.IsAbomination();
			if (flag || (flag2 && pkd.IsDorfbewohner()))
			{
				f = Faction.OfPlayer;
			}
			if (CEditor.ListName == Label.WILDANIMALS)
			{
				f = null;
			}
		}
		bool flag3 = f.CanCreateRelations(pkd);
		PawnGenerationContext context = ((f == Faction.OfPlayer) ? PawnGenerationContext.PlayerStarter : PawnGenerationContext.NonPlayer);
		FloatRange value = new FloatRange(12.1f, 13f);
		DevelopmentalStage developmentalStage = GetRandomDevelopmentalStage();
		if (pkd.IsCreep())
		{
			developmentalStage = DevelopmentalStage.Adult;
		}
		Faction faction = f;
		DevelopmentalStage developmentalStages = developmentalStage;
		bool tutorialMode = TutorSystem.TutorialMode;
		XenotypeDef forcedXenotype = ((!ModsConfig.BiotechActive) ? XenotypeDefOf.Baseliner : null);
		FloatRange? excludeBiologicalAgeRange = (ModsConfig.BiotechActive ? new FloatRange?(value) : ((FloatRange?)null));
		PawnGenerationRequest request = new PawnGenerationRequest(pkd, faction, context, null, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, tutorialMode, 20f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: true, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, forcedXenotype, null, null, 0f, developmentalStages, null, excludeBiologicalAgeRange);
		if (pkd.IsCreep())
		{
			request.IsCreepJoiner = true;
		}
		int num = 5;
		do
		{
			try
			{
				pawn = PawnGenerator.GeneratePawn(request);
			}
			catch
			{
				num--;
				request = new PawnGenerationRequest(pkd, f, context, null, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false);
				if (pkd.IsCreep())
				{
					request.IsCreepJoiner = true;
				}
			}
		}
		while (pawn == null && num > 0);
		if (pawn.relations != null)
		{
			pawn.relations.everSeenByPlayer = true;
		}
		PawnComponentsUtility.AddComponentsForSpawn(pawn);
		if (CEditor.InStartingScreen)
		{
			Type aType = Reflect.GetAType("Verse", "StartingPawnUtility");
			aType.CallMethod("GeneratePossessions", new object[1] { pawn });
		}
		if (!CEditor.API.GetO(OptionB.CREATERACESPECIFIC))
		{
			try
			{
				if (pawn.HasStoryTracker())
				{
					pawn.Reequip(null, -1, pawnSpecific: true);
				}
				pawn.Redress(null, originalColors: true, -1, pawnSpecific: true);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				string modName = pawn.kindDef.GetModName();
				HashSet<HairDef> hashSet = DefTool.ListByMod<HairDef>(modName);
				if (!hashSet.EnumerableNullOrEmpty())
				{
					pawn.SetHair(hashSet.RandomElement());
				}
			}
			catch
			{
			}
		}
		if (CEditor.API.GetO(OptionB.CREATENUDE))
		{
			pawn.DestroyAllApparel();
		}
		if (CEditor.API.GetO(OptionB.CREATENOWEAPON))
		{
			pawn.DestroyAllEquipment();
		}
		if (CEditor.API.GetO(OptionB.CREATENOINV))
		{
			pawn.DestroyAllItems();
		}
		pawn.FixInvalidTracker();
		pawn.FixInvalidNames();
		pawn.ChangeFaction(f);
		return pawn;
	}

	internal static Pawn GeneratePawn(PawnKindDef pkd, Faction f)
	{
		int num = 10;
		Pawn pawn = null;
		do
		{
			num--;
			try
			{
				pawn = PawnGenerator.GeneratePawn(pkd, f);
			}
			catch
			{
			}
		}
		while (pawn == null && num > 0);
		return pawn;
	}

	internal static Pawn GeneratePawn(PawnGenerationRequest pgr)
	{
		int num = 10;
		Pawn pawn = null;
		do
		{
			num--;
			try
			{
				pawn = PawnGenerator.GeneratePawn(pgr);
			}
			catch
			{
			}
		}
		while (pawn == null && num > 0);
		return pawn;
	}

	internal static PresetPawn ClonePawn(this Pawn p)
	{
		PresetPawn presetPawn = new PresetPawn();
		presetPawn.SavePawn(p, -1);
		presetPawn.GeneratePawn();
		Capturer capturer = CEditor.API.Get<Capturer>(EType.Capturer);
		capturer.UpdatePawnGraphic(p);
		return presetPawn;
	}

	internal static void AddOrCreateExistingPawn(PresetPawn ppn)
	{
		if (ppn != null)
		{
			AddOrCreateNewPawn(ppn.pawn.kindDef, ppn.pawn.Faction, ppn.pawn.def, ppn);
		}
	}

	internal static bool CheckIfSpaceZombie(Faction f)
	{
		if (f.IsZombie() && CEditor.InStartingScreen && !CanCreateZombieInSpace)
		{
			MessageTool.Show("can't create zombies in space!");
			return true;
		}
		return false;
	}

	internal static Pawn AddOrCreateNewPawn(PawnKindDef pkd, Faction f, ThingDef raceDef, PresetPawn ppn, IntVec3 pos = default(IntVec3), bool doPlace = false, Gender gender = Gender.None)
	{
		if (pkd == null)
		{
			return null;
		}
		if (CheckIfSpaceZombie(f))
		{
			return null;
		}
		Pawn pawn = ((ppn == null) ? CreateNewPawn(pkd, f, raceDef) : ppn.pawn);
		if (gender != Gender.None)
		{
			pawn.gender = gender;
		}
		pawn?.SpawnOrPassPawn(f, ppn, pos, doPlace);
		return pawn;
	}

	internal static void SetUnsetMutantByDefName(this Pawn pawn, string mutantDefName, string rotStage)
	{
		try
		{
			MutantDef def = DefTool.GetDef<MutantDef>(mutantDefName);
			Enum.TryParse<RotStage>(rotStage, out var result);
			if (def != null)
			{
				pawn.SetUnsetMutant(def, result);
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetUnsetMutant(this Pawn pawn, MutantDef m, RotStage r = RotStage.Fresh)
	{
		if (pawn.HasMutantTracker() && pawn.mutant.HasTurned)
		{
			pawn.mutant.Revert();
		}
		if (m != null)
		{
			pawn.mutant = new Pawn_MutantTracker(pawn, m, r);
			pawn.mutant.Turn();
			if (Event.current.control)
			{
				MakeSpecialMutant(pawn);
			}
		}
	}

	internal static void MakeSpecialMutant(Pawn pawn)
	{
		int num = CEditor.zufallswert.Next(0, 2);
		for (int i = 0; i < num; i++)
		{
			pawn.AddHediff2(random: true, HediffDefOf.Tentacle);
		}
		num = CEditor.zufallswert.Next(0, 2);
		for (int j = 0; j < num; j++)
		{
			pawn.AddHediff2(random: true, HediffDefOf.FleshWhip);
		}
		num = CEditor.zufallswert.Next(0, 3);
		for (int k = 0; k < num; k++)
		{
			pawn.AddHediff2(random: true, HediffDefOf.GhoulBarbs);
		}
		num = CEditor.zufallswert.Next(0, 50);
		if (num >= 25)
		{
			pawn.AddHediff2(random: true, HediffDefOf.Metalblood);
		}
		num = CEditor.zufallswert.Next(0, 50);
		if (num >= 25)
		{
			pawn.AddHediff2(random: true, DefTool.GetDef<HediffDef>("JuggernautSerum"));
		}
		num = CEditor.zufallswert.Next(0, 50);
		if (num >= 25)
		{
			pawn.AddHediff2(random: true, DefTool.GetDef<HediffDef>("Regeneration"));
		}
		num = CEditor.zufallswert.Next(0, 50);
		if (num >= 25)
		{
			pawn.AddHediff2(random: true, HediffDefOf.RapidRegeneration);
		}
		num = CEditor.zufallswert.Next(0, 50);
		if (num >= 25)
		{
			pawn.AddHediff2(random: true, HediffDefOf.RageSpeed, 1f, null, isPermanent: true, -1, -1, 10);
		}
		num = CEditor.zufallswert.Next(0, 50);
		if (num >= 25)
		{
			List<BodyPartRecord> listOfAllowedBodyPartRecords = pawn.GetListOfAllowedBodyPartRecords(HediffDefOf.VoidTouched);
			pawn.AddHediff2(random: false, HediffDefOf.VoidTouched, 1f, listOfAllowedBodyPartRecords.First());
		}
	}

	internal static void FixInvalidNames(this Pawn p)
	{
		if (p != null && p.Name != null && !p.Name.IsValid && p.Name.GetType() == typeof(NameTriple))
		{
			NameTriple nameTriple = p.Name as NameTriple;
			if (nameTriple.Last.NullOrEmpty())
			{
				p.Name = new NameTriple(nameTriple.First, nameTriple.Nick, CEditor.zufallswert.Next(0, 99).ToString("2"));
			}
		}
	}

	internal static void ZombieWorldCreationFixInSpace(this Pawn p)
	{
	}
}
