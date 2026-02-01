using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_AggressiveHeatDump : Ability
{
	public override float GetRadiusForPawn()
	{
		return Mathf.Min(base.pawn.psychicEntropy.EntropyValue / 20f, 9f * ((Ability)this).GetRadiusForPawn(), GenRadial.MaxRadialPatternRadius);
	}

	public override float GetPowerForPawn()
	{
		return base.pawn.psychicEntropy.EntropyValue * ((Ability)this).GetPowerForPawn();
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		float radiusForPawn = ((Ability)this).GetRadiusForPawn();
		float powerForPawn = ((Ability)this).GetPowerForPawn();
		base.pawn.psychicEntropy.RemoveAllEntropy();
		Ability.MakeStaticFleck(targets[0].Cell, targets[0].Thing.Map, FleckDefOf.PsycastAreaEffect, radiusForPawn, 0f);
		Ability.MakeStaticFleck(targets[0].Cell, targets[0].Thing.Map, VPE_DefOf.VPE_AggresiveHeatDump, radiusForPawn, 0f);
		IntVec3 cell = targets[0].Cell;
		Map map = base.pawn.Map;
		DamageDef flame = DamageDefOf.Flame;
		Pawn pawn = base.pawn;
		int damAmount = (int)powerForPawn;
		List<Thing> ignoredThings = new List<Thing> { base.pawn };
		GenExplosion.DoExplosion(cell, map, radiusForPawn, flame, pawn, damAmount, -1f, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, ignoredThings);
	}
}
