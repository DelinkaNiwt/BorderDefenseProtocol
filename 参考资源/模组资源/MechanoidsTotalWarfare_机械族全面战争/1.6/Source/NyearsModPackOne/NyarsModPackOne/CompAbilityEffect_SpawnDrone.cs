using RimWorld;
using Verse;

namespace NyarsModPackOne;

public class CompAbilityEffect_SpawnDrone : CompAbilityEffect
{
	public new CompProperties_AbilitySpawnDrone Props => (CompProperties_AbilitySpawnDrone)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		Pawn pawn = parent.pawn;
		PawnKindDef droneKind = Props.droneKind ?? PawnKindDef.Named("NCL_Dinergate_Drone");
		for (int i = 0; i < Props.droneCount; i++)
		{
			Drone drone = Drone.MakeNewDrone(pawn, droneKind);
			if (drone != null)
			{
				GenSpawn.Spawn(drone, pawn.PositionHeld, pawn.Map, WipeMode.VanishOrMoveAside);
			}
		}
	}
}
