using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NiceInventoryTab;

public static class DamageUtility
{
	public static float MaxRange = 45f;

	public static float MaxMeleePawnDPS = 20f;

	public static float MaxRangedPawnDPS = 20f;

	public static float stepOptimalRange = 0.5f;

	public static float MaxDPSForWeapons = 20f;

	public static Thing GetPawnWeapon(Pawn pawn)
	{
		return pawn?.equipment?.Primary;
	}

	public static float GetRange(Pawn pawn, StatDrawer statBar)
	{
		Thing pawnWeapon = GetPawnWeapon(pawn);
		if (pawnWeapon == null)
		{
			return 0f;
		}
		if (pawnWeapon.def.IsRangedWeapon)
		{
			if (pawnWeapon.def.Verbs.NullOrEmpty())
			{
				return 0f;
			}
			return GetRange_Internal(pawnWeapon, null, pawn);
		}
		return 0f;
	}

	public static float GetRange_Internal(Thing weapon, VerbProperties shootVerb, Pawn pawn)
	{
		if (shootVerb == null)
		{
			shootVerb = GetShootVerb(weapon.def);
		}
		if (shootVerb == null)
		{
			return 0f;
		}
		return shootVerb.range * weapon.GetStatValue(StatDefOf.RangedWeapon_RangeMultiplier) * GetVerbRangeMultiplier(pawn);
	}

	public static float GetVerbRangeMultiplier(Pawn pawn)
	{
		if (pawn == null || VanillaExpandedFrameworkIntegration.VEF_VerbRangeFactor == null)
		{
			return 1f;
		}
		try
		{
			return pawn.GetStatValueForPawn(VanillaExpandedFrameworkIntegration.VEF_VerbRangeFactor, pawn);
		}
		catch
		{
			return 1f;
		}
	}

	public static float GetMaxDPS(Pawn p)
	{
		return MaxDPSForWeapons;
	}

	public static float MeleePawnDPS(Pawn pawn, StatDrawer statBar)
	{
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return 0f;
		}
		if (statBar != null)
		{
			(float original, float filtered) statValueFilterHediffs = HediffUtility.GetStatValueFilterHediffs(pawn, StatDefOf.MeleeDPS, HediffUtility.IsPermanent_or_IsImplant);
			float item = statValueFilterHediffs.original;
			float item2 = statValueFilterHediffs.filtered;
			StatRequest req = StatRequest.For(pawn);
			statBar.Descr = StatDefOf.MeleeDPS.Worker.GetExplanationFull(req, StatDefOf.MeleeDPS.toStringNumberSense, item);
			Thing pawnWeapon = GetPawnWeapon(pawn);
			if (pawnWeapon != null)
			{
				if (Assets.MeleeWeapon_AverageArmorPenetration != null)
				{
					float statValueForPawn = pawnWeapon.GetStatValueForPawn(Assets.MeleeWeapon_AverageArmorPenetration, pawn);
					statBar.Descr += string.Format("\n{0}: {1}", "NIT_ArmorPenetration".Translate(), statValueForPawn.ToStringPercent());
				}
			}
			else if (Assets.MeleeWeapon_AverageArmorPenetration != null)
			{
				float statValue = pawn.GetStatValue(Assets.MeleeWeapon_AverageArmorPenetration);
				statBar.Descr += string.Format("\n{0}: {1}", "NIT_ArmorPenetration".Translate(), statValue.ToStringPercent());
			}
			float num = item - item2;
			if (Settings.DrugImpactVisible)
			{
				(statBar as StatBar).AddAutoBuffDebuff(num, (statBar as StatBar).ColorBar);
			}
			else if (num < 0f)
			{
				(statBar as StatBar).AddAutoBuffDebuff(num, (statBar as StatBar).ColorBar);
			}
			return item;
		}
		return pawn.GetStatValue(StatDefOf.MeleeDPS);
	}

	public static float MeleeWeaponDPS(Thing weapon, Pawn pawn = null)
	{
		if (pawn == null)
		{
			return weapon.def.GetStatValueAbstract(StatDefOf.MeleeWeapon_AverageDPS, weapon.Stuff);
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return 0f;
		}
		return weapon.GetStatValueForPawn(StatDefOf.MeleeWeapon_AverageDPS, pawn);
	}

	public static float MaxMeleeDPS(Pawn p)
	{
		return MaxMeleePawnDPS;
	}

	public static float GetAdjustedRangeDPS(Thing weapon, float range, Pawn shooter = null)
	{
		return RangedWeaponDPS(weapon, shooter) * Math.Min(GetAdjustedHitChanceFactor(weapon, range, shooter), 1f);
	}

	public static float GetOneShotDamage(Thing weapon)
	{
		if (weapon.def.defName == "Gun_MiniFlameblaster")
		{
			return 10f;
		}
		VerbProperties verbProperties = weapon.def.Verbs?.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault();
		if (verbProperties == null)
		{
			return 0f;
		}
		if (verbProperties.defaultProjectile?.projectile == null)
		{
			return ((float?)verbProperties.beamDamageDef?.defaultDamage) ?? 0f;
		}
		return verbProperties.defaultProjectile.projectile.GetDamageAmount(weapon);
	}

	public static float? GetArmorPenetration(Thing weapon)
	{
		if (weapon.def.IsRangedWeapon)
		{
			VerbProperties verbProperties = weapon.def.Verbs.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault();
			if (verbProperties.defaultProjectile?.projectile.damageDef != null && verbProperties.defaultProjectile.projectile.damageDef.harmsHealth)
			{
				return verbProperties.defaultProjectile.projectile.GetArmorPenetration(weapon);
			}
		}
		else if (weapon.def.IsMeleeWeapon && Assets.MeleeWeapon_AverageArmorPenetration != null)
		{
			return weapon.GetStatValue(Assets.MeleeWeapon_AverageArmorPenetration);
		}
		return null;
	}

	public static float? GetWarmup(Thing weapon)
	{
		return weapon.def.Verbs.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault()?.warmupTime;
	}

	public static float? GetStoppingPower(Thing weapon)
	{
		VerbProperties verbProperties = weapon.def.Verbs.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault();
		if (verbProperties == null)
		{
			return null;
		}
		float valueOrDefault = (verbProperties?.defaultProjectile?.projectile?.stoppingPower).GetValueOrDefault();
		if (valueOrDefault > 0f)
		{
			return valueOrDefault;
		}
		return null;
	}

	public static float? GetFireRate(Thing weapon)
	{
		VerbProperties verbProperties = weapon.def.Verbs.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault();
		if (verbProperties == null)
		{
			return null;
		}
		if (verbProperties.burstShotCount > 1)
		{
			return 60f / verbProperties.ticksBetweenBurstShots.TicksToSeconds();
		}
		return null;
	}

	public static int GetBurstCount(Thing weapon)
	{
		return weapon.def.Verbs.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault()?.burstShotCount ?? 1;
	}

	public static float GetMeleeHitChance(Pawn pawn, Thing weapon, bool applyPostProcess = true)
	{
		if (pawn == null)
		{
			return 1f;
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return 1f;
		}
		float statValue = pawn.GetStatValue(StatDefOf.MeleeHitChance, applyPostProcess);
		if (weapon.def == null)
		{
			return statValue;
		}
		float statValueAbstract = weapon.def.GetStatValueAbstract(StatDefOf.MeleeHitChance, weapon.Stuff);
		return statValue * statValueAbstract;
	}

	public static float GetMeleeCooldown(Pawn pawn, ThingDef weaponDef)
	{
		if (pawn == null || weaponDef == null)
		{
			return 1f;
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return 1f;
		}
		List<VerbEntry> updatedAvailableVerbsList = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(terrainTools: false);
		if (updatedAvailableVerbsList.NullOrEmpty())
		{
			return 1f;
		}
		float num = 0f;
		float num2 = 0f;
		foreach (VerbEntry item in updatedAvailableVerbsList)
		{
			if (item.IsMeleeAttack && item.verb.EquipmentSource?.def == weaponDef)
			{
				float selectionWeight = item.GetSelectionWeight(null);
				if (!(selectionWeight <= 0f))
				{
					num += selectionWeight;
					float num3 = item.verb.verbProps.AdjustedCooldownTicks(item.verb, pawn);
					num2 += selectionWeight * num3;
				}
			}
		}
		if (num <= 0f)
		{
			return 1f;
		}
		return num2 / num / 60f;
	}

	public static DamageDef RangedDamageType(Thing weapon)
	{
		if (weapon.def.defName == "Gun_MiniFlameblaster")
		{
			return DamageDefOf.Flame;
		}
		VerbProperties verbProperties = weapon.def.Verbs?.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault();
		if (verbProperties == null)
		{
			return DamageDefOf.Bullet;
		}
		if (verbProperties.defaultProjectile?.projectile == null)
		{
			return verbProperties.beamDamageDef ?? DamageDefOf.Bullet;
		}
		return verbProperties.defaultProjectile.projectile.damageDef ?? DamageDefOf.Bullet;
	}

	public static float RangedWeaponDPS(Thing weapon, Pawn shooter = null)
	{
		if (shooter != null && shooter.WorkTagIsDisabled(WorkTags.Violent))
		{
			return 0f;
		}
		if (weapon == null)
		{
			return 0f;
		}
		if (InterruptDPS(weapon, shooter, out var interuptedDPS))
		{
			return interuptedDPS;
		}
		if (weapon.def.Verbs.NullOrEmpty())
		{
			return 0f;
		}
		float statValueAbstract = weapon.def.GetStatValueAbstract(StatDefOf.RangedWeapon_DamageMultiplier);
		VerbProperties verbProperties = weapon.def.Verbs.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault();
		if (verbProperties == null)
		{
			return 0f;
		}
		int num = verbProperties.burstShotCount;
		float num2 = 0f;
		if (verbProperties.defaultProjectile?.projectile != null)
		{
			num2 = verbProperties.defaultProjectile.projectile.GetDamageAmount(weapon);
		}
		else if (verbProperties.beamDamageDef != null)
		{
			num2 = verbProperties.beamDamageDef.defaultDamage;
			num = 4;
		}
		float warmupTime = verbProperties.warmupTime;
		float statValueAbstract2 = weapon.def.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
		int ticksBetweenBurstShots = verbProperties.ticksBetweenBurstShots;
		float num3 = shooter?.GetStatValue(StatDefOf.AimingDelayFactor) ?? 1f;
		float num4 = warmupTime * num3 + statValueAbstract2 + ((num - 1) * ticksBetweenBurstShots).TicksToSeconds();
		return num2 * statValueAbstract * (float)num / num4;
	}

	public static bool InterruptDPS(Thing weapon, Pawn shooter, out float interuptedDPS)
	{
		interuptedDPS = 0f;
		if (weapon.def.defName == "Gun_MiniFlameblaster")
		{
			interuptedDPS = 2.75f;
			return true;
		}
		if (weapon.def.defName == "VWE_Gun_FireExtinguisher")
		{
			interuptedDPS = 0f;
			return true;
		}
		return false;
	}

	public static float RangedPawnDPS(Pawn p, StatDrawer statBar)
	{
		if (p.WorkTagIsDisabled(WorkTags.Violent))
		{
			return 0f;
		}
		Thing weapon = GetPawnWeapon(p);
		if (statBar != null)
		{
			statBar.Descr = "NIT_RangedDPSTip".Translate();
		}
		if (weapon == null)
		{
			return 0f;
		}
		if (!weapon.def.IsRangedWeapon)
		{
			return 0f;
		}
		(float, float) tuple = HediffUtility.EvaluateWithFilteredHediffs(p, HediffUtility.IsPermanent_or_IsImplant, (Pawn pwn, bool _) => FindOptimalRange(weapon, p).distance);
		float optimal = tuple.Item1;
		float optimalIdeal = tuple.Item2;
		(float original, float filtered) tuple2 = HediffUtility.EvaluateWithFilteredHediffs(p, HediffUtility.IsPermanent_or_IsImplant, (Pawn pwn, bool filtered) => GetAdjustedRangeDPS(weapon, filtered ? optimalIdeal : optimal, pwn));
		float num = tuple2.original;
		float num2 = tuple2.filtered;
		bool flag = false;
		if (InterruptDPS(weapon, p, out var interuptedDPS))
		{
			flag = true;
			num = interuptedDPS;
			num2 = interuptedDPS;
		}
		if (statBar != null)
		{
			float num3 = RangedWeaponDPS(weapon, p);
			float num4 = RangedWeaponDPS(weapon);
			float statValue = p.GetStatValue(StatDefOf.AimingDelayFactor);
			statBar.Descr = "NIT_RangedDPSTip".Translate();
			statBar.Descr += string.Format("\n\n{0} ({1}): {2:F2}", "NIT_DPSatOptimalRange".Translate(), optimal.ToString(Assets.Format_Meters), num);
			statBar.Descr += string.Format("\n{0}: {1:F2}", "NIT_DPS100Accuracy".Translate(), num3);
			if (statValue != 1f || num4 != num3)
			{
				statBar.Descr += string.Format("\n{0}:", "NIT_PawnModifiers".Translate());
				if (statValue != 1f)
				{
					statBar.Descr += "\n    " + "RangedWarmupTime".Translate() + (" " + (statValue - 1f).ToStringPercent());
				}
				if (num4 != num3)
				{
					statBar.Descr += "\n    " + "Accuracy".Translate() + (" " + (num3 / num4 - 1f).ToStringPercent());
				}
			}
			if (!flag)
			{
				statBar.Descr += string.Format("\n\n{0}: {1}", "NIT_OneShotDamage".Translate(), GetOneShotDamage(weapon));
			}
			float? armorPenetration = GetArmorPenetration(weapon);
			if (armorPenetration.HasValue)
			{
				statBar.Descr += string.Format("\n{0}: {1}", "NIT_ArmorPenetration".Translate(), armorPenetration.Value.ToStringPercent());
			}
			float num5 = num - num2;
			if (Settings.DrugImpactVisible)
			{
				(statBar as StatBar).AddAutoBuffDebuff(num5, (statBar as StatBar).ColorBar);
			}
			else if (num5 < 0f)
			{
				(statBar as StatBar).AddAutoBuffDebuff(num5, (statBar as StatBar).ColorBar);
			}
		}
		return num;
	}

	public static float MaxRangedDPS(Pawn p)
	{
		return MaxRangedPawnDPS;
	}

	public static float GetMaxRange(Pawn pawn)
	{
		return MaxRange;
	}

	private static VerbProperties GetShootVerb(ThingDef thingDef)
	{
		VerbProperties verbProperties = thingDef.Verbs.Where((VerbProperties v) => !v.IsMeleeAttack).FirstOrDefault();
		if (verbProperties == null)
		{
			Log.Error("Could not find a valid shoot verb for ThingDef " + thingDef.defName);
		}
		return verbProperties;
	}

	public static float GetAdjustedHitChanceFactor(Thing weapon, float range, Thing shooter = null)
	{
		float num = GetShootVerb(weapon.def).GetHitChanceFactor(weapon, range);
		if (shooter != null)
		{
			num *= ShotReport.HitFactorFromShooter(shooter, range);
		}
		return num;
	}

	public static (float distance, float percent) FindOptimalRange(Thing weapon, Pawn shooter = null)
	{
		VerbProperties shootVerb = GetShootVerb(weapon.def);
		float minRange = shootVerb.minRange;
		float range_Internal = GetRange_Internal(weapon, shootVerb, shooter);
		float minInt = (float)Math.Max(1.0, Math.Ceiling(minRange));
		float maxInt = (float)Math.Floor(range_Internal);
		var (num, item) = (from d in (from i in Enumerable.Range(0, (int)Math.Ceiling((maxInt - minInt) / stepOptimalRange) + 1)
				select minInt + (float)i * stepOptimalRange into d
				where d <= maxInt
				select d).ToList()
			select (distance: d, hitChance: GetAdjustedHitChanceFactor(weapon, d, shooter)) into x
			orderby x.hitChance descending
			select x).FirstOrDefault();
		if (!(num > 0f))
		{
			return (distance: minInt, percent: GetAdjustedHitChanceFactor(weapon, minInt, shooter));
		}
		return (distance: num, percent: item);
	}

	public static IEnumerable<(float distance, float percent)> FindRangesAboveThreshold(Thing weapon, float threshold, Thing shooter = null)
	{
		VerbProperties shootVerb = GetShootVerb(weapon.def);
		float minRange = shootVerb.minRange;
		float range = shootVerb.range;
		float minInt = (float)Math.Max(1.0, Math.Ceiling(minRange));
		float maxInt = (float)Math.Floor(range);
		return (from i in Enumerable.Range(0, (int)Math.Ceiling((maxInt - minInt) / stepOptimalRange) + 1)
			select minInt + (float)i * stepOptimalRange into d
			where d <= maxInt
			select (distance: d, hitChance: GetAdjustedHitChanceFactor(weapon, d, shooter)) into x
			where x.hitChance >= threshold
			orderby x.distance
			select x).ToList();
	}

	private static HediffDef FindTechHediffHediff(ThingDef techHediff)
	{
		List<RecipeDef> allDefsListForReading = DefDatabase<RecipeDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			if (allDefsListForReading[i].addsHediff != null && allDefsListForReading[i].IsIngredient(techHediff))
			{
				return allDefsListForReading[i].addsHediff;
			}
		}
		return null;
	}

	public static void GetVerbsAndTools(ThingDef def, out List<VerbProperties> verbs, out List<Tool> tools)
	{
		verbs = def.Verbs;
		tools = def.tools;
		if (!def.isTechHediff)
		{
			return;
		}
		HediffDef hediffDef = FindTechHediffHediff(def);
		if (hediffDef != null)
		{
			HediffCompProperties_VerbGiver hediffCompProperties_VerbGiver = hediffDef.CompProps<HediffCompProperties_VerbGiver>();
			if (hediffCompProperties_VerbGiver != null)
			{
				verbs = hediffCompProperties_VerbGiver.verbs;
				tools = hediffCompProperties_VerbGiver.tools;
			}
		}
	}

	public static float GetMaxHitDamage(Thing weapon)
	{
		GetVerbsAndTools(weapon.def, out var verbs, out var tools);
		IEnumerable<VerbUtility.VerbPropertiesWithSource> enumerable = from x in VerbUtility.GetAllVerbProperties(verbs, tools)
			where x.verbProps.IsMeleeAttack
			select x;
		if (!enumerable.Any())
		{
			return 0f;
		}
		float num = 0f;
		foreach (VerbUtility.VerbPropertiesWithSource item in enumerable)
		{
			float num2 = item.tool.AdjustedBaseMeleeDamageAmount(weapon, item.verbProps.meleeDamageDef);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static bool MeleeWeaponHasExtraDamage(Thing weapon, DamageDef dam)
	{
		GetVerbsAndTools(weapon.def, out var _, out var tools);
		foreach (Tool item in tools)
		{
			List<ExtraDamage> extraMeleeDamages = item.extraMeleeDamages;
			if (extraMeleeDamages != null && extraMeleeDamages.Any((ExtraDamage x) => x.def == dam))
			{
				return true;
			}
		}
		return false;
	}

	public static bool CanUse(Pawn pawn, Thing item)
	{
		if (item.def.IsRangedWeapon)
		{
			return !pawn.TryGetAttackVerb(item, !pawn.IsColonist).ApparelPreventsShooting();
		}
		return true;
	}

	public static bool CanUseWithShields(Pawn pawn, Thing item)
	{
		if (item.def.IsRangedWeapon)
		{
			return !pawn.TryGetAttackVerb(item, !pawn.IsColonist).ApparelPreventsShooting();
		}
		return true;
	}
}
