using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_SpawnSkeleton : Ability_TargetCorpse
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Corpse obj = globalTargetInfo.Thing as Corpse;
			IntVec3 position = obj.Position;
			obj.Destroy();
			FilthMaker.TryMakeFilth(position, ((Ability)this).pawn.Map, ThingDefOf.Filth_CorpseBile, 3);
			GenSpawn.Spawn(PawnGenerator.GeneratePawn(VPE_DefOf.VPE_SummonedSkeleton, ((Ability)this).pawn.Faction), position, ((Ability)this).pawn.Map);
		}
	}
}
