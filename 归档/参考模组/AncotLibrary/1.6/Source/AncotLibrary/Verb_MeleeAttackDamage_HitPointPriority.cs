using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Verb_MeleeAttackDamage_HitPointPriority : Verb_MeleeAttack
{
	private const float MeleeDamageRandomFactorMin = 0.8f;

	private const float MeleeDamageRandomFactorMax = 1.2f;

	private IEnumerable<DamageInfo> DamageInfosToApply(LocalTargetInfo target)
	{
		float num = verbProps.AdjustedMeleeDamageAmount(this, CasterPawn);
		float armorPenetration = verbProps.AdjustedArmorPenetration(this, CasterPawn);
		DamageDef def = verbProps.meleeDamageDef;
		BodyPartGroupDef bodyPartGroupDef = null;
		HediffDef hediffDef = null;
		QualityCategory qc = QualityCategory.Normal;
		num = Rand.Range(num * 0.8f, num * 1.2f);
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
		ThingDef source = base.EquipmentSource?.def ?? CasterPawn.def;
		Pawn pawn2;
		Pawn pawn = (pawn2 = caster as Pawn);
		bool instigatorGuilty = pawn2 == null || !pawn.Drafted;
		Vector3 direction = (target.Thing.Position - CasterPawn.Position).ToVector3();
		DamageInfo damageInfo = new DamageInfo(hitPart: SelectBodyPartBasedOnHitPoints(target), def: def, amount: num, armorPenetration: armorPenetration, angle: -1f, instigator: caster, weapon: source, category: DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget: null, instigatorGuilty: instigatorGuilty);
		damageInfo.SetAngle(direction);
		damageInfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
		damageInfo.SetWeaponHediff(hediffDef);
		damageInfo.SetTool(tool);
		damageInfo.SetWeaponQuality(qc);
		yield return damageInfo;
	}

	private BodyPartRecord SelectBodyPartBasedOnHitPoints(LocalTargetInfo target)
	{
		if (!(target.Thing is Pawn pawn))
		{
			return null;
		}
		List<BodyPartRecord> source = pawn.health.hediffSet.GetNotMissingParts().ToList();
		var list = source.Select((BodyPartRecord part) => new
		{
			Part = part,
			Weight = part.coverageAbs * (1f + (float)part.def.hitPoints)
		}).ToList();
		float num = list.Sum(wp => wp.Weight);
		float num2 = Rand.Value * num;
		float num3 = 0f;
		foreach (var item in list)
		{
			num3 += item.Weight;
			if (num3 >= num2)
			{
				return item.Part;
			}
		}
		return list.LastOrDefault()?.Part;
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
