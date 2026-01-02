using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityComboCharge : CompAbilityEffect
{
	private int ticks = 0;

	private Pawn pawn => parent.pawn;

	public new CompProperties_ComboCharge Props => (CompProperties_ComboCharge)props;

	public override bool CanCast => !parent.OnCooldown;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return !parent.OnCooldown;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		ticks = 0;
	}

	public override void CompTick()
	{
		ticks++;
		if (!parent.Casting && !parent.OnCooldown && parent.RemainingCharges != parent.maxCharges && ticks > Props.comboTick)
		{
			parent.StartCooldown(parent.def.cooldownTicksRange.RandomInRange);
			parent.RemainingCharges = parent.maxCharges;
		}
		if ((parent.RemainingCharges < parent.maxCharges) && parent.Casting && parent.verb.WarmupProgress > (Props.comboWarmupProgressPct ?? 0.2f))
		{
			parent.verb.WarmupComplete();
		}
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref ticks, "ticks", 0);
	}
}
