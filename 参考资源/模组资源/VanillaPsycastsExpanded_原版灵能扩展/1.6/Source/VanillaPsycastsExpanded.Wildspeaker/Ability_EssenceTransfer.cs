using System.Linq;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class Ability_EssenceTransfer : Ability
{
	private Pawn curTarget;

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		if (!(targets[0].Thing is Pawn essenceOf) || !(targets[1].Thing is Pawn pawn))
		{
			return;
		}
		Pawn pawn2 = curTarget;
		if (pawn2 != null && !pawn2.Dead && !pawn2.Discarded && !pawn2.Destroyed)
		{
			foreach (Hediff_Essence item in curTarget.health.hediffSet.hediffs.OfType<Hediff_Essence>().ToList())
			{
				curTarget.health.RemoveHediff(item);
			}
		}
		else
		{
			curTarget = null;
		}
		Hediff_Essence hediff_Essence = (Hediff_Essence)HediffMaker.MakeHediff(VPE_DefOf.VPE_Essence, pawn);
		hediff_Essence.EssenceOf = essenceOf;
		pawn.health.AddHediff(hediff_Essence);
		curTarget = pawn;
	}

	public override float GetRangeForPawn()
	{
		if (base.currentTargetingIndex == 1)
		{
			return 99999f;
		}
		return ((Ability)this).GetRangeForPawn();
	}

	public override void ExposeData()
	{
		((Ability)this).ExposeData();
		Scribe_References.Look(ref curTarget, "curTarget");
	}
}
