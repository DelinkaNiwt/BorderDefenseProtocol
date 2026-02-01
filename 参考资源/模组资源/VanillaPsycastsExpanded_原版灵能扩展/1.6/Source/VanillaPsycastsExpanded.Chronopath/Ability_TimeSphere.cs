using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class Ability_TimeSphere : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			TimeSphere obj = (TimeSphere)ThingMaker.MakeThing(VPE_DefOf.VPE_TimeSphere);
			obj.Duration = ((Ability)this).GetDurationForPawn();
			obj.Radius = ((Ability)this).GetRadiusForPawn();
			GenSpawn.Spawn(obj, globalTargetInfo.Cell, globalTargetInfo.Map);
		}
	}
}
