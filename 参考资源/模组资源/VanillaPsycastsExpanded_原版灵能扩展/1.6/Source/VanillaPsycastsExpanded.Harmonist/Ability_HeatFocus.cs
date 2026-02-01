using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;

namespace VanillaPsycastsExpanded.Harmonist;

public class Ability_HeatFocus : Ability
{
	private static readonly AccessTools.FieldRef<Pawn_PsychicEntropyTracker, float> currentEntropy = AccessTools.FieldRefAccess<Pawn_PsychicEntropyTracker, float>("currentEntropy");

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		float num = Mathf.Min(1f - base.pawn.psychicEntropy.CurrentPsyfocus, (base.pawn.psychicEntropy.EntropyValue - base.pawn.GetStatValue(VPE_DefOf.VPE_PsychicEntropyMinimum)) * 0.002f);
		base.pawn.psychicEntropy.OffsetPsyfocusDirectly(num);
		currentEntropy(base.pawn.psychicEntropy) -= num * 500f;
	}
}
