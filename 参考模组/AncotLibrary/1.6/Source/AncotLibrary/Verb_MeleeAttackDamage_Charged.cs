using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Verb_MeleeAttackDamage_Charged : Verb_MeleeAttack
{
	private const float MeleeDamageRandomFactorMin = 0.8f;

	private const float MeleeDamageRandomFactorMax = 1.2f;

	public CompWeaponCharge compCharge => base.EquipmentSource.TryGetComp<CompWeaponCharge>();

	public CompMeleeCombo compMeleeCombo => base.EquipmentSource.TryGetComp<CompMeleeCombo>();

	public override void WarmupComplete()
	{
		base.WarmupComplete();
		compMeleeCombo?.TryComboOnce();
	}

	public virtual float ChargedDamageFactor()
	{
		if (compCharge.CanBeUsed)
		{
			return compCharge.MeleeDamageFactorCharged;
		}
		return 1f;
	}

	public virtual float ChargedArmorPenetrationFactor()
	{
		if (compCharge.CanBeUsed)
		{
			return compCharge.MeleeArmorPenetrationFactorCharged;
		}
		return 1f;
	}

	public virtual void EffecterCharged(LocalTargetInfo target)
	{
		if (compCharge.CanBeUsed)
		{
			compCharge?.UsedOnce();
			Effecter_Extension effecter_Extension = maneuver?.GetModExtension<Effecter_Extension>();
			if (effecter_Extension?.effcterDef != null && CasterPawn != null && target.Thing != null && CasterPawn.Map != null)
			{
				effecter_Extension.effcterDef.Spawn(CasterPawn.Position, target.Thing.Position, CasterPawn.Map)?.Cleanup();
			}
		}
	}

	private IEnumerable<DamageInfo> DamageInfosToApply(LocalTargetInfo target)
	{
		float num = verbProps.AdjustedMeleeDamageAmount(this, CasterPawn);
		float armorPenetration = verbProps.AdjustedArmorPenetration(this, CasterPawn) * ChargedArmorPenetrationFactor();
		DamageDef def = verbProps.meleeDamageDef;
		BodyPartGroupDef bodyPartGroupDef = null;
		HediffDef hediffDef = null;
		QualityCategory qc = QualityCategory.Normal;
		num = Rand.Range(num * 0.8f, num * 1.2f) * ChargedDamageFactor();
		EffecterCharged(target);
		if (CasterIsPawn)
		{
			bodyPartGroupDef = verbProps.AdjustedLinkedBodyPartsGroup(tool);
			if (num >= 1f)
			{
				if (base.HediffCompSource != null)
				{
					hediffDef = base.HediffCompSource.Def;
				}
			}
			else
			{
				num = 1f;
				def = DamageDefOf.Blunt;
			}
		}
		ThingDef source;
		if (base.EquipmentSource != null)
		{
			source = base.EquipmentSource.def;
			base.EquipmentSource.TryGetQuality(out qc);
		}
		else
		{
			source = CasterPawn.def;
		}
		Vector3 direction = (target.Thing.Position - CasterPawn.Position).ToVector3();
		Pawn pawn2;
		Pawn pawn = (pawn2 = caster as Pawn);
		DamageInfo damageInfo = new DamageInfo(instigatorGuilty: pawn2 == null || !pawn.Drafted, def: def, amount: num, armorPenetration: armorPenetration, angle: -1f, instigator: caster, hitPart: null, weapon: source);
		damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
		damageInfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
		damageInfo.SetWeaponHediff(hediffDef);
		damageInfo.SetAngle(direction);
		damageInfo.SetTool(tool);
		damageInfo.SetWeaponQuality(qc);
		yield return damageInfo;
		if (tool != null && tool.extraMeleeDamages != null)
		{
			foreach (ExtraDamage extraMeleeDamage in tool.extraMeleeDamages)
			{
				if (Rand.Chance(extraMeleeDamage.chance))
				{
					num = extraMeleeDamage.amount;
					damageInfo = new DamageInfo(amount: Rand.Range(num * 0.8f, num * 1.2f), def: extraMeleeDamage.def, armorPenetration: extraMeleeDamage.AdjustedArmorPenetration(this, CasterPawn), angle: -1f, instigator: caster, hitPart: null, weapon: source);
					damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
					damageInfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
					damageInfo.SetWeaponHediff(hediffDef);
					damageInfo.SetAngle(direction);
					yield return damageInfo;
				}
			}
		}
		if (!surpriseAttack || ((verbProps.surpriseAttack == null || verbProps.surpriseAttack.extraMeleeDamages.NullOrEmpty()) && (tool == null || tool.surpriseAttack == null || tool.surpriseAttack.extraMeleeDamages.NullOrEmpty())))
		{
			yield break;
		}
		IEnumerable<ExtraDamage> enumerable = Enumerable.Empty<ExtraDamage>();
		if (verbProps.surpriseAttack != null && verbProps.surpriseAttack.extraMeleeDamages != null)
		{
			enumerable = enumerable.Concat(verbProps.surpriseAttack.extraMeleeDamages);
		}
		if (tool != null && tool.surpriseAttack != null && !tool.surpriseAttack.extraMeleeDamages.NullOrEmpty())
		{
			enumerable = enumerable.Concat(tool.surpriseAttack.extraMeleeDamages);
		}
		foreach (ExtraDamage item in enumerable)
		{
			int num2 = GenMath.RoundRandom(item.AdjustedDamageAmount(this, CasterPawn));
			DamageInfo damageInfo2 = new DamageInfo(armorPenetration: item.AdjustedArmorPenetration(this, CasterPawn), def: item.def, amount: num2, angle: -1f, instigator: caster, hitPart: null, weapon: source);
			damageInfo2.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
			damageInfo2.SetWeaponBodyPartGroup(bodyPartGroupDef);
			damageInfo2.SetWeaponHediff(hediffDef);
			damageInfo2.SetAngle(direction);
			yield return damageInfo2;
		}
	}

	protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
	{
		DamageWorker.DamageResult result = new DamageWorker.DamageResult();
		foreach (DamageInfo item in DamageInfosToApply(target))
		{
			if (!target.ThingDestroyed)
			{
				result = target.Thing.TakeDamage(item);
				continue;
			}
			return result;
		}
		return result;
	}
}
