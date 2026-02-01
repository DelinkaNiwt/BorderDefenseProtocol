using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Ability_ConjureHeatPearls : Ability
{
	private static readonly AccessTools.FieldRef<Pawn_PsychicEntropyTracker, float> currentEntropy = AccessTools.FieldRefAccess<Pawn_PsychicEntropyTracker, float>("currentEntropy");

	public override bool IsEnabledForPawn(out string reason)
	{
		if (!((Ability)this).IsEnabledForPawn(ref reason))
		{
			return false;
		}
		if (base.pawn.psychicEntropy.EntropyValue - base.pawn.GetStatValue(VPE_DefOf.VPE_PsychicEntropyMinimum) >= 20f)
		{
			return true;
		}
		reason = "VPE.NotEnoughHeat".Translate();
		return false;
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		currentEntropy(base.pawn.psychicEntropy) -= 20f;
		Thing thing = ThingMaker.MakeThing(VPE_DefOf.VPE_HeatPearls);
		IntVec3 intVec = base.pawn.Position + GenRadial.RadialPattern[Rand.RangeInclusive(2, GenRadial.NumCellsInRadius(4.9f))];
		Map map = base.pawn.Map;
		DamageDef bomb = DamageDefOf.Bomb;
		Pawn pawn = base.pawn;
		List<Thing> ignoredThings = new List<Thing> { base.pawn, thing };
		GenExplosion.DoExplosion(intVec, map, 1.9f, bomb, pawn, -1, -1f, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, ignoredThings);
		GenSpawn.Spawn(thing, intVec, base.pawn.Map);
	}

	public override string GetDescriptionForPawn()
	{
		return ((Ability)this).GetDescriptionForPawn() + "\n" + "VPE.MustHaveHeatAmount".Translate(20).Colorize(Color.red);
	}
}
