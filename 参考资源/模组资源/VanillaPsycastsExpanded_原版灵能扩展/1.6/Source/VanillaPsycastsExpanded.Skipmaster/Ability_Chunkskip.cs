using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Skipmaster;

public class Ability_Chunkskip : Ability
{
	private readonly Dictionary<LocalTargetInfo, HashSet<Thing>> foundChunksCache = new Dictionary<LocalTargetInfo, HashSet<Thing>>();

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			AbilityExtension_Clamor modExtension = ((Def)(object)base.def).GetModExtension<AbilityExtension_Clamor>();
			foreach (Thing item in FindClosestChunks(globalTargetInfo.HasThing ? new LocalTargetInfo(globalTargetInfo.Thing) : new LocalTargetInfo(globalTargetInfo.Cell)))
			{
				if (FindFreeCell(globalTargetInfo.Cell, base.pawn.Map, out var result))
				{
					AbilityUtility.DoClamor(item.Position, modExtension.clamorRadius, base.pawn, modExtension.clamorType);
					AbilityUtility.DoClamor(result, modExtension.clamorRadius, base.pawn, modExtension.clamorType);
					((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(item.Position, globalTargetInfo.Map, 0.72f), item.Position, 60, (Map)null);
					((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_ExitNoDelay.Spawn(result, globalTargetInfo.Map, 0.72f), result, 60, (Map)null);
					FleckMaker.ThrowDustPuffThick(result.ToVector3(), globalTargetInfo.Map, Rand.Range(1.5f, 3f), CompAbilityEffect_Chunkskip.DustColor);
					item.Position = result;
				}
			}
			SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(globalTargetInfo.Cell, base.pawn.Map));
		}
	}

	public override void WarmupToil(Toil toil)
	{
		((Ability)this).WarmupToil(toil);
		toil.AddPreTickAction(delegate
		{
			if (base.pawn.jobs.curDriver.ticksLeftThisToil == 5)
			{
				foreach (Thing item in FindClosestChunks(base.pawn.jobs.curJob.targetA))
				{
					FleckMaker.Static(item.TrueCenter(), base.pawn.Map, FleckDefOf.PsycastSkipFlashEntry, 0.72f);
				}
			}
		});
	}

	private IEnumerable<Thing> FindClosestChunks(LocalTargetInfo target)
	{
		if (foundChunksCache.TryGetValue(target, out var foundChunks))
		{
			return foundChunks;
		}
		foundChunks = new HashSet<Thing>();
		RegionTraverser.BreadthFirstTraverse(target.Cell, base.pawn.Map, (Region from, Region to) => true, delegate(Region x)
		{
			List<Thing> list = x.ListerThings.ThingsInGroup(ThingRequestGroup.Chunk);
			for (int i = 0; i < list.Count; i++)
			{
				if (!((float)foundChunks.Count < ((Ability)this).GetPowerForPawn()))
				{
					break;
				}
				Thing thing = list[i];
				if (!thing.Fogged() && !foundChunks.Contains(thing))
				{
					foundChunks.Add(thing);
				}
			}
			return (float)foundChunks.Count >= ((Ability)this).GetPowerForPawn();
		}, 999999, RegionType.Set_All);
		foundChunksCache.Add(target, foundChunks);
		return foundChunks;
	}

	private bool FindFreeCell(IntVec3 target, Map map, out IntVec3 result)
	{
		return CellFinder.TryFindRandomCellNear(target, map, Mathf.RoundToInt(((Ability)this).GetRadiusForPawn()) - 1, (IntVec3 cell) => CompAbilityEffect_WithDest.CanTeleportThingTo(cell, map) && GenSight.LineOfSight(cell, target, map, skipFirstCell: true), out result);
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		((Ability)this).DrawHighlight(target);
		foreach (Thing item in FindClosestChunks(target))
		{
			GenDraw.DrawLineBetween(item.TrueCenter(), target.CenterVector3);
			GenDraw.DrawTargetHighlight(item);
		}
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!target.Cell.Standable(base.pawn.Map))
		{
			return false;
		}
		if (target.Cell.Filled(base.pawn.Map))
		{
			return false;
		}
		if (!FindClosestChunks(target).Any())
		{
			if (showMessages)
			{
				Messages.Message("VPE.NoChunks".Translate(), base.pawn, MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if (!FindFreeCell(target.Cell, base.pawn.Map, out var _))
		{
			if (showMessages)
			{
				Messages.Message("AbilityNotEnoughFreeSpace".Translate(), base.pawn, MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return ((Ability)this).ValidateTarget(target, showMessages);
	}
}
