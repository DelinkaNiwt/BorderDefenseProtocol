using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Conflagrator;

[StaticConstructorOnStartup]
public class Ability_FireTornado : Ability
{
	private static readonly ThingDef FireTornadoDef = ThingDef.Named("VPE_FireTornado");

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			((FireTornado)GenSpawn.Spawn(FireTornadoDef, globalTargetInfo.Cell, globalTargetInfo.Map)).ticksLeftToDisappear = ((Ability)this).GetDurationForPawn();
		}
	}
}
