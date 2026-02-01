using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class HediffComp_MechAutoRecovery : HediffComp
{
	private int ticksToHeal;

	private float healPoint;

	public override string CompTipStringExtra => $"\n再生速度: {Props.tickMultiflier / 60}% / m".Translate();

	public HediffCompProperties_MechAutoRecovery Props => (HediffCompProperties_MechAutoRecovery)props;

	public override void CompPostMake()
	{
		base.CompPostMake();
		ResetTicksToHeal();
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		ticksToHeal--;
		if (ticksToHeal > 0)
		{
			return;
		}
		TryHealRandomPermanentWound();
		TryHealRandomHediffTendable();
		List<Hediff> hediffs = base.Pawn.health.hediffSet.hediffs;
		for (int i = 0; i < hediffs.Count; i++)
		{
			for (int j = 0; j < Props.healHediffDefs.Count; j++)
			{
				if (hediffs[i].def == Props.healHediffDefs[j] && base.Pawn.health.hediffSet.HasHediff(hediffs[i].def))
				{
					base.Pawn.health.hediffSet.GetFirstHediffOfDef(hediffs[i].def).Heal(10f);
				}
			}
		}
		ResetTicksToHeal();
	}

	private void ResetTicksToHeal()
	{
		healPoint = Props.healPoint;
		ticksToHeal = Props.tickMultiflier;
	}

	private void TryHealRandomPermanentWound()
	{
		IEnumerable<Hediff> hediffs = base.Pawn.health.hediffSet.hediffs;
		bool recold = false;
		foreach (Hediff hediff in hediffs)
		{
			if (hediff.def.isBad && hediff.IsPermanent())
			{
				recold = true;
				hediff.Severity -= healPoint;
			}
		}
		if (!recold)
		{
			Hediff t = base.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
			if (t != null)
			{
				t.Severity -= healPoint * 0.01f;
			}
		}
	}

	private void TryHealRandomHediffTendable()
	{
		IEnumerable<Hediff> hediffs = base.Pawn.health.hediffSet.hediffs;
		foreach (Hediff hediff in hediffs)
		{
			if (hediff.def.defName != "Scaria" && hediff.def.isBad && (hediff.IsPermanent() || hediff.def.chronic || hediff.def.tendable || hediff.def.makesSickThought))
			{
				hediff.Heal(healPoint);
				if ((double)hediff.Severity <= 0.003)
				{
					HealthUtility.Cure(hediff);
					break;
				}
			}
		}
	}
}
