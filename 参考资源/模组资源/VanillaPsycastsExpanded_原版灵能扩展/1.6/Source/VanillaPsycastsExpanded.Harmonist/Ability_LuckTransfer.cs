using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Harmonist;

public class Ability_LuckTransfer : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		if (targets[0].Thing is Pawn pawn && targets[1].Thing is Pawn pawn2)
		{
			MoteBetween obj = (MoteBetween)ThingMaker.MakeThing(VPE_DefOf.VPE_PsycastPsychicEffectTransfer);
			obj.Attach(pawn, pawn2);
			obj.Scale = 1f;
			obj.exactPosition = pawn.DrawPos;
			GenSpawn.Spawn(obj, pawn.Position, pawn.MapHeld);
			MoteBetween obj2 = (MoteBetween)ThingMaker.MakeThing(VPE_DefOf.VPE_PsycastPsychicEffectTransfer);
			obj2.Attach(pawn2, pawn);
			obj2.Scale = 1f;
			obj2.exactPosition = pawn2.DrawPos;
			GenSpawn.Spawn(obj2, pawn2.Position, pawn2.MapHeld);
			int ticksToDisappear = Mathf.RoundToInt((float)((Ability)this).GetDurationForPawn() * pawn.GetStatValue(StatDefOf.PsychicSensitivity));
			pawn.health.AddHediff(VPE_DefOf.VPE_Lucky).TryGetComp<HediffComp_Disappears>().ticksToDisappear = ticksToDisappear;
			pawn2.health.AddHediff(VPE_DefOf.VPE_UnLucky).TryGetComp<HediffComp_Disappears>().ticksToDisappear = ticksToDisappear;
		}
	}
}
