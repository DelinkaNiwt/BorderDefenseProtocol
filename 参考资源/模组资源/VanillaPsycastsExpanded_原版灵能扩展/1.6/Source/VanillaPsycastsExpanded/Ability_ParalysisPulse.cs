using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;

namespace VanillaPsycastsExpanded;

public class Ability_ParalysisPulse : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		MoteMaker.MakeAttachedOverlay(base.pawn, VPE_DefOf.VPE_Mote_ParalysisPulse, Vector3.zero, ((Ability)this).GetRadiusForPawn());
	}
}
