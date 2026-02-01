using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace NiceInventoryTab;

public static class FieldTooltipHelper
{
	public static string PawnWordPercent(Pawn pawn, float mod)
	{
		TaggedString taggedString = "\n   " + "NIT_PawnModifierPercent".Translate(mod.ToStringPercent(), pawn.LabelShortCap);
		if (mod < 1f)
		{
			return taggedString.Colorize(Assets.PenaltyColor);
		}
		if (mod > 1f)
		{
			return taggedString.Colorize(Assets.BuffColor);
		}
		return taggedString;
	}

	public static string RangedWeaponDamage(Thing thing, Pawn pawn)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("NIT_Damage".Translate() + $": {DamageUtility.GetOneShotDamage(thing):F1}");
		return stringBuilder.ToTaggedString();
	}

	public static string WeaponCanUseWithShield(Thing thing, Pawn pawn)
	{
		return "VanillaFactionsExpanded.UsableWithShield".Translate() + ": " + VanillaExpandedFrameworkIntegration.UsableWithShields(thing.def).Value.ToStringYesNo();
	}

	public static string WeaponFireRate(Thing thing, Pawn pawn)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("BurstShotFireRate".Translate() + (": " + DamageUtility.GetFireRate(thing).Value.ToString(Assets.Format_FireRate)));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("BurstShotCount".Translate() + $": {DamageUtility.GetBurstCount(thing)}");
		return stringBuilder.ToTaggedString();
	}

	public static string WeaponMeleeAccuracy(Thing thing, Pawn pawn)
	{
		float mod = (pawn.WorkTagIsDisabled(WorkTags.Violent) ? 1f : pawn.GetStatValue(StatDefOf.MeleeHitChance));
		float meleeHitChance = DamageUtility.GetMeleeHitChance(pawn, thing);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(StatDefOf.MeleeHitChance.LabelCap + (" " + meleeHitChance.ToStringPercent()) + PawnWordPercent(pawn, mod));
		return stringBuilder.ToTaggedString();
	}

	public static string WeaponMeleeAttackSpeed(Thing thing, Pawn pawn)
	{
		return DamageUtility.GetMeleeCooldown(pawn, thing.def).ToString("F2") + " " + "SecondsPerAttackLower".Translate() + string.Format(" ({0}) ", "Average".Translate()) + PawnWordPercent(pawn, pawn.GetStatValue(StatDefOf.MeleeCooldownFactor));
	}

	public static string WeaponMeleeDamage(Thing thing, Pawn pawn)
	{
		float maxHitDamage = DamageUtility.GetMaxHitDamage(thing);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("NIT_Damage".Translate() + $": {maxHitDamage:0.##}");
		stringBuilder.AppendLine();
		stringBuilder.Append(WhereGoesMyExtraDamageEffects(thing, pawn));
		return stringBuilder.ToTaggedString();
	}

	public static string WhereGoesMyExtraDamageEffects(Thing weapon, Pawn currentWeaponUser)
	{
		ThingDef def = weapon.def;
		DamageUtility.GetVerbsAndTools(def, out var verbs, out var tools);
		IEnumerable<VerbUtility.VerbPropertiesWithSource> enumerable = from x in VerbUtility.GetAllVerbProperties(verbs, tools)
			where x.verbProps.IsMeleeAttack
			select x;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (VerbUtility.VerbPropertiesWithSource item in enumerable)
		{
			float num = 0f;
			float num2 = 0f;
			if (currentWeaponUser != null)
			{
				num = item.verbProps.AdjustedMeleeDamageAmount(item.tool, currentWeaponUser, weapon, null);
				num2 = item.verbProps.AdjustedCooldown(item.tool, currentWeaponUser, weapon);
			}
			else
			{
				num = item.verbProps.AdjustedMeleeDamageAmount(item.tool, null, def, weapon.Stuff, null);
				num2 = item.verbProps.AdjustedCooldown(item.tool, null, def, weapon.Stuff);
			}
			if (item.tool != null)
			{
				stringBuilder.AppendLine("  " + item.tool.LabelCap);
			}
			else
			{
				stringBuilder.AppendLine(string.Format("  {0}:", "StatsReport_NonToolAttack".Translate()));
			}
			stringBuilder.AppendLine(string.Format("    {0} {1} ({2})", num.ToString("F1"), "DamageLower".Translate(), item.ToolCapacity.label));
			if (!item.tool.extraMeleeDamages.NullOrEmpty())
			{
				foreach (ExtraDamage extraMeleeDamage in item.tool.extraMeleeDamages)
				{
					stringBuilder.AppendLine(string.Format("   +{0} {1} {2}", extraMeleeDamage.chance.ToStringPercent(), extraMeleeDamage.amount.ToString("F1"), extraMeleeDamage.def.label));
				}
			}
			stringBuilder.AppendLine(string.Format("    {0} {1}", num2.ToString("F2"), "SecondsPerAttackLower".Translate()));
		}
		return stringBuilder.ToString();
	}

	public static string WeaponReloadTimeShort(Thing weapon, Pawn pawn)
	{
		float num = (pawn.WorkTagIsDisabled(WorkTags.Violent) ? 1f : pawn.GetStatValue(StatDefOf.AimingDelayFactor));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("WarmupTime".Translate() + (": " + ((DamageUtility.GetWarmup(weapon) ?? 0f) * num).ToString(Assets.Format_Seconds) + " " + PawnWordPercent(pawn, num)));
		stringBuilder.AppendLine(StatDefOf.RangedWeapon_Cooldown.LabelCap + (": " + weapon.def.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown).ToString(Assets.Format_Seconds)));
		return stringBuilder.ToTaggedString();
	}

	public static string WeaponStoppingPower(Thing thing, Pawn pawn)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("StoppingPower".Translate() + $": {DamageUtility.GetStoppingPower(thing).Value}");
		stringBuilder.AppendLine("");
		stringBuilder.AppendLine("StoppingPowerExplanation".Translate());
		return stringBuilder.ToTaggedString();
	}
}
