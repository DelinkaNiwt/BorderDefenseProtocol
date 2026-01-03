using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffCompApplyDamage_ExplodeChance : HediffComp
{
	private int cooldownTicksLeft = 0;

	public HediffCompProperties_ApplyDamage_ExplodeChance Props => (HediffCompProperties_ApplyDamage_ExplodeChance)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		if (cooldownTicksLeft > 0)
		{
			cooldownTicksLeft -= delta;
		}
	}

	public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		if (base.Pawn.health.summaryHealth.SummaryHealthPercent < Props.startTriggerHealthPct && Rand.Chance(Props.chance) && cooldownTicksLeft == 0 && TriggerAngle(dinfo))
		{
			GenExplosion.DoExplosion(base.Pawn.PositionHeld, base.Pawn.MapHeld, Props.explosionRadius, Props.damageDef, base.Pawn, Props.damageAmountBase, Props.armorPenetrationBase);
			if (Props.cooldownAbility != null)
			{
				Ability ability = parent.pawn.abilities.GetAbility(Props.cooldownAbility, includeTemporary: true);
				ability.StartCooldown(Props.cooldownTicks);
			}
			if (Props.cooldownTicks > 0)
			{
				cooldownTicksLeft = Props.cooldownTicks;
			}
			if (Props.dieInExplosion)
			{
				base.Pawn.Kill(dinfo);
			}
		}
	}

	public bool TriggerAngle(DamageInfo dinfo)
	{
		_ = Props.triggerAngle;
		if (false)
		{
			return true;
		}
		float angle = dinfo.Angle;
		float asAngle = base.Pawn.Rotation.AsAngle;
		float num = (angle - asAngle + 360f + 180f) % 360f;
		Log.Message(num);
		float min = Props.triggerAngle.min;
		float max = Props.triggerAngle.max;
		if (min <= max)
		{
			return num >= min && num <= max;
		}
		return num >= min || num <= max;
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Values.Look(ref cooldownTicksLeft, "cooldownTicksLeft", 0);
	}
}
