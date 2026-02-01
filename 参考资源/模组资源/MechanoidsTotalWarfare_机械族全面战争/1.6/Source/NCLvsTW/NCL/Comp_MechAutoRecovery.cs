using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class Comp_MechAutoRecovery : ThingComp
{
	private int ticksToHeal;

	private Pawn mechPawn;

	private CompProperties_MechAutoRecovery Props => (CompProperties_MechAutoRecovery)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		mechPawn = parent as Pawn;
		ResetTicksToHeal();
	}

	public override void CompTick()
	{
		base.CompTick();
		if (mechPawn != null && !mechPawn.Dead && mechPawn.Spawned)
		{
			ticksToHeal--;
			if (ticksToHeal <= 0)
			{
				ApplyAutoRecovery();
				ResetTicksToHeal();
			}
		}
	}

	private void ApplyAutoRecovery()
	{
		HealSpecificHediffs();
		if (!TryHealPermanentWounds())
		{
			HealBloodLoss();
		}
		TryHealTendableHediffs();
	}

	private void HealSpecificHediffs()
	{
		if (Props.healHediffDefs == null || Props.healHediffDefs.Count == 0)
		{
			return;
		}
		foreach (HediffDef hediffDef in Props.healHediffDefs)
		{
			mechPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef)?.Heal(10f);
		}
	}

	private bool TryHealPermanentWounds()
	{
		bool healedPermanent = false;
		List<Hediff> hediffs = mechPawn.health.hediffSet.hediffs;
		for (int i = 0; i < hediffs.Count; i++)
		{
			Hediff hediff = hediffs[i];
			if (hediff.def.isBad && hediff.IsPermanent())
			{
				hediff.Severity -= Props.healAmount;
				healedPermanent = true;
			}
		}
		return healedPermanent;
	}

	private void HealBloodLoss()
	{
		Hediff bloodLoss = mechPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
		if (bloodLoss != null)
		{
			bloodLoss.Severity -= Props.healAmount * 0.01f;
		}
	}

	private void TryHealTendableHediffs()
	{
		List<Hediff> hediffs = mechPawn.health.hediffSet.hediffs;
		for (int i = 0; i < hediffs.Count; i++)
		{
			Hediff hediff = hediffs[i];
			if (IsHealableHediff(hediff))
			{
				hediff.Heal(Props.healAmount);
				if (hediff.Severity <= 0.003f)
				{
					mechPawn.health.RemoveHediff(hediff);
					break;
				}
			}
		}
	}

	private bool IsHealableHediff(Hediff hediff)
	{
		return hediff.def.isBad && hediff.def != HediffDefOf.Scaria && (hediff.IsPermanent() || hediff.def.chronic || hediff.def.tendable || hediff.def.makesSickThought);
	}

	private void ResetTicksToHeal()
	{
		ticksToHeal = Props.tickInterval;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticksToHeal, "ticksToHeal", Props.tickInterval);
	}
}
