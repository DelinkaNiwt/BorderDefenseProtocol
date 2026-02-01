using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded;

public class Ability_ConsumeBodies : Ability_TargetCorpse
{
	public override void WarmupToil(Toil toil)
	{
		((Ability)this).WarmupToil(toil);
		toil.AddPreInitAction(delegate
		{
			GlobalTargetInfo[] currentlyCastingTargets = ((Ability)this).Comp.currentlyCastingTargets;
			for (int i = 0; i < currentlyCastingTargets.Length; i++)
			{
				GlobalTargetInfo globalTargetInfo = currentlyCastingTargets[i];
				if (globalTargetInfo.HasThing && globalTargetInfo.Thing.TryGetComp<CompRottable>() != null)
				{
					((Ability)this).AddEffecterToMaintain(VPE_DefOf.VPE_Liferot.Spawn(globalTargetInfo.Thing.Position, ((Ability)this).pawn.Map), (TargetInfo)globalTargetInfo.Thing, toil.defaultDuration);
				}
			}
		});
		toil.AddPreTickAction(delegate
		{
			GlobalTargetInfo[] currentlyCastingTargets = ((Ability)this).Comp.currentlyCastingTargets;
			for (int i = 0; i < currentlyCastingTargets.Length; i++)
			{
				GlobalTargetInfo globalTargetInfo = currentlyCastingTargets[i];
				if (globalTargetInfo.HasThing && globalTargetInfo.Thing.TryGetComp<CompRottable>() != null && globalTargetInfo.Thing.IsHashIntervalTick(60))
				{
					FilthMaker.TryMakeFilth(globalTargetInfo.Thing.Position, globalTargetInfo.Thing.Map, ThingDefOf.Filth_CorpseBile);
					globalTargetInfo.Thing.TryGetComp<CompRottable>().RotProgress += 60000f;
				}
			}
		});
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		if (!((Ability)this).pawn.health.hediffSet.HasHediff(VPE_DefOf.VPE_BodiesConsumed))
		{
			((Ability)this).pawn.health.AddHediff(VPE_DefOf.VPE_BodiesConsumed);
		}
		Hediff_BodiesConsumed hediff_BodiesConsumed = ((Ability)this).pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_BodiesConsumed) as Hediff_BodiesConsumed;
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			MoteBetween obj = (MoteBetween)ThingMaker.MakeThing(VPE_DefOf.VPE_SoulOrbTransfer);
			obj.Attach(globalTargetInfo.Thing, ((Ability)this).pawn);
			obj.exactPosition = globalTargetInfo.Thing.DrawPos;
			GenSpawn.Spawn(obj, globalTargetInfo.Thing.Position, ((Ability)this).pawn.Map);
			hediff_BodiesConsumed.consumedBodies++;
			globalTargetInfo.Thing.Destroy();
		}
	}
}
