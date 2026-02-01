using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CharacterEditor;

internal static class PlacingTool
{
	internal const string CO_JOBSGIVENTHISTICK = "jobsGivenThisTick";

	internal static IntVec3 lastKnownPosition = default(IntVec3);

	internal static Rot4 rotation;

	internal static void DropThingWithPod(Thing t)
	{
		DropPodUtility.DropThingGroupsNear(UI.MouseCell(), Find.CurrentMap, new List<List<Thing>>
		{
			new List<Thing> { t }
		});
	}

	internal static void DropAllSelectedWithPod(List<Selected> l, IntVec3 loc = default(IntVec3))
	{
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.DROP + Label.ALL, DebugMenuOptionMode.Tool, delegate
		{
			foreach (Selected item in l)
			{
				DoDrop(item, loc);
			}
			DebugTools.curTool = null;
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void DropSelectedWithPod(Selected s, IntVec3 loc = default(IntVec3))
	{
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.DROP + s.thingDef.SLabel(), DebugMenuOptionMode.Tool, delegate
		{
			DoDrop(s, loc);
			DebugTools.curTool = null;
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	private static void DoDrop(Selected s, IntVec3 loc)
	{
		List<Thing> item = s.Generate();
		List<List<Thing>> list = new List<List<Thing>>();
		list.Add(item);
		IntVec3 dropCenter = ((loc == default(IntVec3)) ? UI.MouseCell() : loc);
		DropPodUtility.DropThingGroupsNear(dropCenter, Find.CurrentMap, list, 60);
	}

	internal static void DropPawnWithPod(Pawn p, PresetPawn ppn)
	{
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.PLACE_PAWN + " droppod", DebugMenuOptionMode.Tool, delegate
		{
			List<List<Thing>> thingsGroups = new List<List<Thing>>
			{
				new List<Thing> { p }
			};
			if (!p.IsColonist)
			{
				p.CreatePawnLord();
			}
			DropPodUtility.DropThingGroupsNear(UI.MouseCell(), Find.CurrentMap, thingsGroups);
			PawnxTool.PostProcess(p, ppn);
			TryCloneOrAbort(p);
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void DoEffect(Pawn p, IntVec3 pos)
	{
		try
		{
			bool flag = false;
			if (!p.HasPsylink)
			{
				flag = true;
			}
			Ability ability = (CEditor.API.GetO(OptionB.USECHAOSABILITY) ? AbilityUtility.MakeAbility(DefTool.AbilityDef("ChaosSkip"), p) : null);
			if (ability == null)
			{
				return;
			}
			float warmupTime = ability.VerbProperties.First().warmupTime;
			FloatRange randomRange = new FloatRange(0f, 0f);
			foreach (AbilityComp comp in ability.comps)
			{
				if (comp.props.GetType() == typeof(CompProperties_AbilityTeleport))
				{
					randomRange = (comp.props as CompProperties_AbilityTeleport).randomRange;
					(comp.props as CompProperties_AbilityTeleport).randomRange = new FloatRange(0f, 0f);
				}
			}
			ability.VerbProperties.First().warmupTime = 0f;
			ability.VerbTracker.PrimaryVerb.DrawHighlight(UI.MouseCell());
			ability.VerbTracker.PrimaryVerb.TryStartCastOn(p);
			ability.VerbTracker.PrimaryVerb.DrawHighlight(UI.MouseCell());
			DefTool.SoundDef("PsychicSoothePulserCast").PlayOneShot(new TargetInfo(UI.MouseCell(), Find.CurrentMap));
			p.jobs.ClearQueuedJobs();
			p.jobs.SetMemberValue("jobsGivenThisTick", 0);
			ability.VerbProperties.First().warmupTime = warmupTime;
			foreach (AbilityComp comp2 in ability.comps)
			{
				if (comp2.props.GetType() == typeof(CompProperties_AbilityTeleport))
				{
					(comp2.props as CompProperties_AbilityTeleport).randomRange = randomRange;
				}
			}
			if (flag)
			{
				p.psychicEntropy.RemoveAllEntropy();
			}
		}
		catch
		{
		}
	}

	internal static void PlaceInCustomPosition(Pawn p, PresetPawn ppn)
	{
		string label = (Event.current.capsLock ? Label.CLONE_PAWN : Label.PLACE_PAWN);
		DebugMenuOption debugMenuOption = new DebugMenuOption(label, DebugMenuOptionMode.Tool, delegate
		{
			p.SpawnPawn(ppn, UI.MouseCell());
			DoEffect(p, UI.MouseCell());
			TryCloneOrAbort(p);
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void PlaceMultiplePawnsInCustomPosition(Selected s, Faction f)
	{
		CEditor.API.EditorMoveRight();
		string label = (Event.current.capsLock ? Label.CLONE_PAWN : Label.PLACE_PAWN);
		DebugMenuOption debugMenuOption = new DebugMenuOption(label, DebugMenuOptionMode.Tool, delegate
		{
			DoPlaceMultiplePawns(s, f);
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void DoPlaceMultiplePawns(Selected s, Faction f)
	{
		lastKnownPosition = UI.MouseCell();
		for (int i = 0; i < s.stackVal; i++)
		{
			Pawn pawn = PawnxTool.CreateNewPawn(s.pkd, f, s.pkd.race, forceFaction: true);
			if (s.gender != Gender.None)
			{
				pawn.gender = s.gender;
			}
			if (s.age >= 0)
			{
				pawn.SetAge(s.age);
			}
			pawn.SpawnPawn(null, lastKnownPosition);
		}
	}

	internal static void PlaceNearPawn(Selected s, Pawn p)
	{
		JustPlace(s, p.Position);
	}

	internal static void JustPlace(Selected s, IntVec3 pos = default(IntVec3), Rot4 rot = default(Rot4))
	{
		List<Thing> list = s.Generate();
		foreach (Thing item in list)
		{
			item.Spawn(rot, pos);
		}
	}

	internal static void PlaceInCustomPosition(Selected s, Action<Selected> onPlacedAction)
	{
		DebugMenuOption debugMenuOption = new DebugMenuOption("place object", DebugMenuOptionMode.Tool, delegate
		{
			JustPlace(s, default(IntVec3), rotation);
			if (onPlacedAction != null)
			{
				onPlacedAction(s);
			}
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void PlaceInPosition(Pawn p, PresetPawn ppn, IntVec3 pos)
	{
		p.SpawnPawn(ppn, pos);
	}

	internal static void TryCloneOrAbort(Pawn p)
	{
		if (Event.current.capsLock)
		{
			PresetPawn ppn = p.ClonePawn();
			PawnxTool.AddOrCreateExistingPawn(ppn);
		}
		else
		{
			DebugTools.curTool = null;
		}
	}

	internal static void DeletePawnFromCustomPosition()
	{
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.DELETE_PAWN, DebugMenuOptionMode.Tool, delegate
		{
			Find.CurrentMap.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Find.CurrentMap, UI.MouseCell()));
			IntVec3 cell = UI.MouseCell();
			cell.DeletePawnsInCell();
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void BeginTeleportCustomPawn()
	{
		Find.Selector?.ClearSelection();
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.SELECT_PAWN, DebugMenuOptionMode.Tool, delegate
		{
			Pawn pawn = UI.MouseCell().FirstPawnInCellArea();
			if (pawn != null)
			{
				TeleportPawnAndReselect(pawn);
			}
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void BeginTeleportPawn(Pawn p)
	{
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.TELEPORT, DebugMenuOptionMode.Tool, delegate
		{
			p.TeleportPawn();
			DoEffect(p, UI.MouseCell());
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	private static void TeleportPawnAndReselect(Pawn p)
	{
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.TELEPORT, DebugMenuOptionMode.Tool, delegate
		{
			p.TeleportPawn();
			DoEffect(p, UI.MouseCell());
			BeginTeleportCustomPawn();
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void Destroy()
	{
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.DESTROY, DebugMenuOptionMode.Tool, delegate
		{
			IntVec3 intVec = UI.MouseCell();
			Find.CurrentMap.roofGrid.SetRoof(intVec, null);
			ClearCell(intVec, Find.CurrentMap);
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void ClearCell(IntVec3 pos, Map map)
	{
		try
		{
			Dictionary<int, Thing> dictionary = new Dictionary<int, Thing>();
			foreach (Thing thing2 in pos.GetThingList(map))
			{
				dictionary.Add(dictionary.Count, thing2);
			}
			foreach (int key in dictionary.Keys)
			{
				Thing thing = dictionary[key];
				if (thing != null && thing.def != null)
				{
					if (thing.def.destroyable)
					{
						thing.Destroy();
						break;
					}
					if (thing.def.category == Verse.ThingCategory.Building)
					{
						thing.DeSpawn();
						break;
					}
				}
			}
		}
		catch
		{
		}
	}
}
