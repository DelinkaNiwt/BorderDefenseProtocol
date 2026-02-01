using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CharacterEditor;

public static class RaceTool
{
	internal const string CO_RECALCULATELIFESTAGEINDEX = "RecalculateLifeStageIndex";

	internal const string CO_GROWTH = "growth";

	internal const string CO_CACHEDLIFESTAGEINDEX = "cachedLifeStageIndex";

	public static bool bRecalcTriggered = false;

	private static KeyValuePair<string, BackstoryDef> flippingBackstory = default(KeyValuePair<string, BackstoryDef>);

	public static bool IsBodySize(this LifeStageAge lsa)
	{
		return lsa != null && lsa.def != null && !lsa.def.defName.NullOrEmpty() && lsa.def.defName.StartsWith("SZHumanSize_");
	}

	public static bool IsBodySizeGene(this Gene g)
	{
		return g?.def.IsBodySizeGene() ?? false;
	}

	public static LifeStageDef GetLifeStageDef(this Gene g)
	{
		return g?.def.GetLifeStageDef();
	}

	public static bool CanHaveChangedBodySize(this Pawn p)
	{
		return p != null && p.HasAgeTracker() && p.HasGeneTracker() && p.HasAdultAge() && p.RaceProps.Humanlike && p.DevelopmentalStage == DevelopmentalStage.Adult;
	}

	public static Gene FirstBodySizeGene(this Pawn p)
	{
		return (p == null || p.genes == null || p.genes.GenesListForReading.NullOrEmpty()) ? null : p.genes.GenesListForReading.FirstOrFallback((Gene x) => x.IsBodySizeGene());
	}

	public static bool HasBodySizeGenes(this Pawn p)
	{
		return p.FirstBodySizeGene() != null;
	}

	public static bool HasChildOrBabyBody(this Pawn p)
	{
		return p.HasStoryTracker() && (p.story.bodyType == BodyTypeDefOf.Baby || p.story.bodyType == BodyTypeDefOf.Child);
	}

	public static bool HasAdultAge(this Pawn p)
	{
		return p.ageTracker.AgeBiologicalYears - p.GetMinAgeForAdult() >= 0;
	}

	public static int GetMinAgeForAdult(this Pawn p)
	{
		LifeStageAge lifeStageAge = p.RaceProps.lifeStageAges.FirstOrDefault((LifeStageAge l) => l.def.defName != null && !l.def.defName.Contains("Teenager") && l.def.developmentalStage.Adult());
		return (lifeStageAge != null) ? ((int)lifeStageAge.minAge) : 0;
	}

	public static void CheckAllPawnsOnStartupAndReapplyBodySize()
	{
		List<Pawn> list = Find.CurrentMap?.mapPawns?.AllPawns;
		if (list.NullOrEmpty())
		{
			return;
		}
		foreach (Pawn item in list)
		{
			item.TryApplyOrKeepBodySize();
		}
	}

	public static void RemoveOldBodySizeRedundants(Gene gene)
	{
		if (!gene.IsBodySizeGene())
		{
			return;
		}
		Pawn pawn = gene.pawn;
		for (int num = pawn.genes.Xenogenes.Count - 1; num >= 0; num--)
		{
			if (pawn.genes.Xenogenes[num] != gene && pawn.genes.Xenogenes[num].IsBodySizeGene())
			{
				pawn.genes.Xenogenes.Remove(pawn.genes.Xenogenes[num]);
			}
		}
		for (int num2 = pawn.genes.Endogenes.Count - 1; num2 >= 0; num2--)
		{
			if (pawn.genes.Endogenes[num2] != gene && pawn.genes.Endogenes[num2].IsBodySizeGene())
			{
				pawn.genes.Endogenes.Remove(pawn.genes.Endogenes[num2]);
			}
		}
	}

	public static void TryToFallbackLifeAge(Pawn p)
	{
		if (p != null && p.HasAgeTracker() && p.ageTracker.CurLifeStageRace.IsBodySize())
		{
			bRecalcTriggered = true;
			p.ageTracker.DebugSetGrowth(0.9f);
			p.ageTracker.CallMethod("RecalculateLifeStageIndex", null);
		}
	}

	public static void RememberBackstory(this Pawn p)
	{
		if (p != null && p.HasStoryTracker())
		{
			flippingBackstory = new KeyValuePair<string, BackstoryDef>(p.ThingID, p.story.Adulthood);
		}
	}

	public static void TryApplyOrKeepBodySize(this Pawn p, Gene gene = null)
	{
		if (p == null)
		{
			return;
		}
		if (bRecalcTriggered)
		{
			bRecalcTriggered = !bRecalcTriggered;
			return;
		}
		if (!p.CanHaveChangedBodySize())
		{
			TryToFallbackLifeAge(p);
			return;
		}
		Gene gene2 = ((gene == null) ? p.FirstBodySizeGene() : gene);
		if (gene2 == null)
		{
			TryToFallbackLifeAge(p);
		}
		else
		{
			gene2.pawn.KeepOrSet_LifeStage(gene2.GetLifeStageDef());
		}
	}

	private static void KeepOrSet_LifeStage(this Pawn p, LifeStageDef ld)
	{
		if (p == null || ld == null || p.DevelopmentalStage != DevelopmentalStage.Adult || p.HasChildOrBabyBody())
		{
			return;
		}
		LifeStageAge lifeStageAge = p.RaceProps.GetLifeStageAgeByDef(ld);
		if (lifeStageAge == null)
		{
			lifeStageAge = p.TryCreateLifeStageAge(ld);
		}
		if (lifeStageAge == null)
		{
			return;
		}
		int recentLifeStageAgeIndex_ForLSDef = p.RaceProps.GetRecentLifeStageAgeIndex_ForLSDef(ld);
		if (recentLifeStageAgeIndex_ForLSDef >= 0)
		{
			if (p.ThingID == flippingBackstory.Key)
			{
				p.story.Adulthood = flippingBackstory.Value;
			}
			p.ageTracker.SetMemberValue("growth", 1f);
			p.ageTracker.SetMemberValue("cachedLifeStageIndex", recentLifeStageAgeIndex_ForLSDef);
			CEditor.API.UpdateGraphics();
		}
	}

	public static int GetRecentLifeStageAgeIndex_ForLSDef(this RaceProperties p, LifeStageDef ld)
	{
		if (p.lifeStageAges.NullOrEmpty())
		{
			return -1;
		}
		for (int num = p.lifeStageAges.Count - 1; num >= 0; num--)
		{
			if (p.lifeStageAges[num].def == ld)
			{
				return num;
			}
		}
		return -1;
	}

	private static LifeStageAge TryCreateLifeStageAge(this Pawn p, LifeStageDef ld)
	{
		if (ld == null || p == null || p.RaceProps == null)
		{
			return null;
		}
		LifeStageAge lastDefaultLifeStageAge = p.RaceProps.GetLastDefaultLifeStageAge();
		if (lastDefaultLifeStageAge == null)
		{
			return null;
		}
		LifeStageAge lifeStageAge = new LifeStageAge();
		lifeStageAge.def = ld;
		lifeStageAge.minAge = float.MaxValue;
		lifeStageAge.soundCall = lastDefaultLifeStageAge.soundCall;
		lifeStageAge.soundAmbience = lastDefaultLifeStageAge.soundAmbience;
		lifeStageAge.soundAngry = lastDefaultLifeStageAge.soundAngry;
		lifeStageAge.soundDeath = lastDefaultLifeStageAge.soundDeath;
		lifeStageAge.soundWounded = lastDefaultLifeStageAge.soundWounded;
		p.RaceProps.lifeStageAges.Add(lifeStageAge);
		p.RaceProps.ResolveReferencesSpecial();
		lifeStageAge.def.ResolveReferences();
		lifeStageAge.def.PostLoad();
		p.def.ResolveReferences();
		return lifeStageAge;
	}

	public static LifeStageAge GetLifeStageAgeByDef(this RaceProperties p, LifeStageDef def)
	{
		if (p.lifeStageAges.NullOrEmpty())
		{
			return null;
		}
		for (int num = p.lifeStageAges.Count - 1; num >= 0; num--)
		{
			if (p.lifeStageAges[num].def == def)
			{
				return p.lifeStageAges[num];
			}
		}
		return null;
	}

	public static LifeStageAge GetLastDefaultLifeStageAge(this RaceProperties p)
	{
		if (p.lifeStageAges.NullOrEmpty())
		{
			return null;
		}
		for (int num = p.lifeStageAges.Count - 1; num >= 0; num--)
		{
			if (!p.lifeStageAges[num].IsBodySize())
			{
				return p.lifeStageAges[num];
			}
		}
		return null;
	}

	public static void ChangeRace(Pawn pawn, PawnKindDef pkd, bool keepRaceSpecificClothes)
	{
		PresetPawn presetPawn = new PresetPawn();
		presetPawn.SavePawn(pawn, -1);
		presetPawn.dicParams[PresetPawn.Param.P01_pawnkinddef] = pkd.defName;
		MessageTool.Show("ppn kind=" + pkd.defName);
		presetPawn.GeneratePawn(_setBodyParts: true, !keepRaceSpecificClothes);
		Pawn pawn2 = presetPawn.pawn;
		MessageTool.Show("pawnNeu kind=" + pawn2.kindDef.defName);
		pawn2.ChangeKind(pkd);
		IntVec3 position = pawn.Position;
		bool isPrisoner = pawn.IsPrisoner;
		pawn.Delete(force: true);
		if (!CEditor.InStartingScreen)
		{
			pawn2.TeleportPawn(position);
		}
		if (pawn2.story != null && pawn2.kindDef != null && pawn2.story.headType != null)
		{
			HashSet<HeadTypeDef> headDefList = pawn2.GetHeadDefList(genderized: true);
			pawn2.SetHeadTypeDef(headDefList.RandomElement());
		}
		if (pawn2.story != null && pawn2.kindDef != null && pawn2.story.bodyType != null)
		{
			List<BodyTypeDef> bodyDefList = pawn2.GetBodyDefList(restricted: true);
			pawn2.SetBody(bodyDefList.RandomElement());
		}
		if (isPrisoner)
		{
			if (pawn2.Faction == Faction.OfPlayer)
			{
				pawn2.SetFaction(null);
			}
			if (pawn2.guest.Released)
			{
				pawn2.guest.Released = false;
			}
			if (!pawn2.IsPrisonerOfColony)
			{
				pawn2.guest.CapturedBy(Faction.OfPlayer);
			}
		}
		PawnxTool.PostProcess(pawn2, presetPawn);
	}
}
