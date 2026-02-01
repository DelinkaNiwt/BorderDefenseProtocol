using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Skipmaster;

public class Ability_Teleport : Ability
{
	public virtual FleckDef[] EffectSet => new FleckDef[3]
	{
		FleckDefOf.PsycastSkipFlashEntry,
		FleckDefOf.PsycastSkipInnerExit,
		FleckDefOf.PsycastSkipOuterRingExit
	};

	public override void WarmupToil(Toil toil)
	{
		((Ability)this).WarmupToil(toil);
		toil.AddPreTickAction(delegate
		{
			if (base.pawn.jobs.curDriver.ticksLeftThisToil == 5)
			{
				FleckDef[] effectSet = EffectSet;
				for (int i = 0; i < ((Ability)this).Comp.currentlyCastingTargets.Length; i += 2)
				{
					Thing thing = ((Ability)this).Comp.currentlyCastingTargets[i].Thing;
					if (thing != null)
					{
						if (thing is Pawn pawn)
						{
							FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(pawn, effectSet[0], Vector3.zero);
							dataAttachedOverlay.link.detachAfterTicks = 5;
							pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
						}
						else
						{
							FleckMaker.Static(thing.TrueCenter(), thing.Map, FleckDefOf.PsycastSkipFlashEntry);
						}
						GlobalTargetInfo globalTargetInfo = ((Ability)this).Comp.currentlyCastingTargets[i + 1];
						FleckMaker.Static(globalTargetInfo.Cell, globalTargetInfo.Map, effectSet[1]);
						FleckMaker.Static(globalTargetInfo.Cell, globalTargetInfo.Map, effectSet[2]);
						SoundDefOf.Psycast_Skip_Entry.PlayOneShot(thing);
						SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(globalTargetInfo.Cell, globalTargetInfo.Map));
						((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(thing, thing.Map), thing.Position, 60, (Map)null);
						((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_Exit.Spawn(globalTargetInfo.Cell, globalTargetInfo.Map), globalTargetInfo.Cell, 60, (Map)null);
					}
				}
			}
		});
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		AbilityExtension_Clamor modExtension = ((Def)(object)base.def).GetModExtension<AbilityExtension_Clamor>();
		for (int i = 0; i < targets.Length; i += 2)
		{
			Thing thing = targets[i].Thing;
			if (thing == null)
			{
				continue;
			}
			thing.TryGetComp<CompCanBeDormant>()?.WakeUp();
			GlobalTargetInfo globalTargetInfo = targets[i + 1];
			if (thing.Map != globalTargetInfo.Map)
			{
				if (!(thing is Pawn pawn))
				{
					continue;
				}
				pawn.teleporting = true;
				pawn.ExitMap(allowedToJoinOrCreateCaravan: true, Rot4.Invalid);
				pawn.teleporting = false;
				GenSpawn.Spawn(pawn, globalTargetInfo.Cell, globalTargetInfo.Map);
			}
			thing.Position = globalTargetInfo.Cell;
			AbilityUtility.DoClamor(thing.Position, modExtension.clamorRadius, base.pawn, modExtension.clamorType);
			AbilityUtility.DoClamor(globalTargetInfo.Cell, modExtension.clamorRadius, base.pawn, modExtension.clamorType);
			(thing as Pawn)?.Notify_Teleported();
		}
		((Ability)this).Cast(targets);
	}
}
