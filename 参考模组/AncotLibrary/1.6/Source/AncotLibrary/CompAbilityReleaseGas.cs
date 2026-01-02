using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompAbilityReleaseGas : CompAbilityEffect
{
	private int remainingGas;

	private bool started;

	[Unsaved(false)]
	private Effecter effecter;

	public new CompProperties_AbilityReleaseGas Props => (CompProperties_AbilityReleaseGas)props;

	private Pawn Pawn => parent.pawn;

	private int TotalGas => Mathf.CeilToInt(Props.cellsToFill * 255f);

	private float GasReleasedPerTick => (float)TotalGas / Props.durationSeconds / 60f;

	private Thing EffecterSourceThing => Pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		StartRelease();
	}

	public void StartRelease()
	{
		remainingGas = TotalGas;
		started = true;
	}

	public override void CompTick()
	{
		if (!started || Pawn.MapHeld == null)
		{
			return;
		}
		if (Props.effecterReleasing != null)
		{
			if (effecter == null)
			{
				effecter = Props.effecterReleasing.Spawn(EffecterSourceThing, TargetInfo.Invalid);
			}
			effecter.EffectTick(EffecterSourceThing, TargetInfo.Invalid);
		}
		if (remainingGas > 0 && Pawn.IsHashIntervalTick(Props.intervalTick))
		{
			int num = Mathf.Min(remainingGas, Mathf.RoundToInt(GasReleasedPerTick * (float)Props.intervalTick));
			GasUtility.AddGas(Pawn.PositionHeld, Pawn.MapHeld, Props.gasType, num);
			remainingGas -= num;
			if (remainingGas <= 0)
			{
				started = false;
				remainingGas = TotalGas;
			}
		}
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref remainingGas, "remainingGas", 0);
		Scribe_Values.Look(ref started, "started", defaultValue: false);
	}
}
