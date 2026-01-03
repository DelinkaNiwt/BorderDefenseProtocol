using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

public class Comp_LaserData_Instant : ThingComp
{
	public int DMGmp = 1;

	private float qualityNum;

	public CompProperties_LaserData_Instant Props => (CompProperties_LaserData_Instant)props;

	public float QualityNum
	{
		get
		{
			try
			{
				CompQuality compQuality = parent.TryGetComp<CompQuality>();
				switch (compQuality.Quality)
				{
				case QualityCategory.Awful:
					qualityNum = 0.9f;
					break;
				case QualityCategory.Poor:
				case QualityCategory.Normal:
				case QualityCategory.Good:
				case QualityCategory.Excellent:
					qualityNum = 1f;
					break;
				case QualityCategory.Masterwork:
					qualityNum = 1.25f;
					break;
				case QualityCategory.Legendary:
					qualityNum = 1.5f;
					break;
				}
				return qualityNum;
			}
			catch
			{
				return 1f;
			}
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponDMG_Label".Translate(), "StatEnergyWeaponDMG_Desc".Translate((float)Props.DamageNum * QualityNum), "StatEnergyWeaponDMG_Text".Translate((float)Props.DamageNum * QualityNum), 114515);
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponAP_Label".Translate(), "StatEnergyWeaponAP_Desc".Translate(Props.DamageArmorPenetration.ToStringPercent()), "StatEnergyWeaponAP_Text".Translate(Props.DamageArmorPenetration.ToStringPercent()), 114514);
		if (Props.IfSecondDamage)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponDMG2_Label".Translate(), "StatEnergyWeaponDMG2_Desc".Translate(Props.DamageNum_B), "StatEnergyWeaponDMG2_Text".Translate(Props.DamageNum_B), 114513);
			yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponAP2_Label".Translate(), "StatEnergyWeaponAP2_Desc".Translate(Props.DamageArmorPenetration_B.ToStringPercent()), "StatEnergyWeaponAP2_Text".Translate(Props.DamageArmorPenetration_B.ToStringPercent()), 114512);
		}
		if (Props.IfCanScatter)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponDMG3Radius_Label".Translate(), "StatEnergyWeaponDMG3Radius_Desc".Translate(Props.ScatterRadius), "StatEnergyWeaponDMG3Radius_Text".Translate(Props.ScatterRadius), 114511);
		}
	}

	public void TakeDamageToTarget(LocalTargetInfo TargetPlace, Thing Caster, Verb Verb)
	{
		Thing thing;
		Vector3 vector;
		if (TargetPlace.HasThing)
		{
			thing = TargetPlace.Thing;
			vector = thing.DrawPos;
		}
		else
		{
			thing = null;
			vector = TargetPlace.CenterVector3;
		}
		Map map = Caster.Map;
		Vector3 drawPos = Caster.DrawPos;
		IntVec3 center = vector.ToIntVec3();
		DamageDef damageDef = Props.DamageDef;
		int num = Props.DamageNum * DMGmp * (int)QualityNum;
		float damageArmorPenetration = Props.DamageArmorPenetration;
		if (thing != null && damageDef != null)
		{
			float angleFlat = (TargetPlace.CenterVector3.ToIntVec3() - Caster.Position).AngleFlat;
			BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(Caster, thing, thing, Verb.EquipmentSource.def, null, null);
			DamageInfo dinfo = new DamageInfo(damageDef, num, damageArmorPenetration, angleFlat, Caster, null, Verb.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, thing);
			thing.TakeDamage(dinfo).AssociateWithLog(log);
			if (Props.IfSecondDamage)
			{
				DamageDef damageDef_B = Props.DamageDef_B;
				int damageNum_B = Props.DamageNum_B;
				float damageArmorPenetration_B = Props.DamageArmorPenetration_B;
				DamageInfo dinfo2 = new DamageInfo(damageDef_B, damageNum_B, damageArmorPenetration_B, angleFlat, Caster, null, Verb.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, thing);
				thing.TakeDamage(dinfo2).AssociateWithLog(log);
			}
			if (Props.IfCanScatter)
			{
				for (int i = 0; i < Props.ScatterNum; i++)
				{
					CellRect cellRect = CellRect.CenteredOn(center, (int)Props.ScatterRadius);
					cellRect.ClipInsideMap(map);
					IntVec3 randomCell = cellRect.RandomCell;
					GenExplosion.DoExplosion(randomCell, map, Props.ScatterExplosionRadius, Props.ScatterExplosionDef, Caster, Props.ScatterExplosionDamage, Props.ScatterExplosionArmorPenetration, null, Verb.EquipmentSource.def, null, null, null, 0f, 0, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 0, 0f, damageFalloff: false, null, null, null, doVisualEffects: true, 1f, 0f, doSoundEffects: true, null, 0f);
					FleckMaker.ConnectingLine(vector, randomCell.ToVector3Shifted(), Props.LaserFleck_ScatterLaser, map);
				}
			}
			bool flag = Props.LaserLine_FleckDef != null;
			bool flag2 = Props.LaserLine_FleckDef2 != null;
			if (flag && flag2)
			{
				float num2;
				float num3;
				float num4;
				if (!Props.RandomRGB)
				{
					num2 = (float)Props.Color_Red / 255f;
					num3 = (float)Props.Color_Green / 255f;
					num4 = (float)Props.Color_Blue / 255f;
				}
				else
				{
					num2 = (float)Rand.RangeInclusive(1, 255) / 255f;
					num3 = (float)Rand.RangeInclusive(1, 255) / 255f;
					num4 = (float)Rand.RangeInclusive(1, 255) / 255f;
				}
				float color_Alpha = Props.Color_Alpha;
				float turretyoffset = Props.turretyoffset;
				Vector3 vector2;
				if (Props.useyoffset)
				{
					vector2 = Caster.DrawPos;
					vector2.z += turretyoffset;
				}
				else
				{
					float angle = (Caster.DrawPos - thing.DrawPos).AngleFlat();
					vector2 = MYDE_ModFront.GetVector3_By_AngleFlat(Caster.DrawPos, Props.StartPositionOffset_Range, angle);
				}
				Vector3 vector3 = thing.DrawPos - vector2;
				float x = vector3.MagnitudeHorizontal();
				FleckCreationData dataStatic;
				FleckCreationData dataStatic2;
				if (Rand.RangeInclusive(0, 100) <= 85)
				{
					dataStatic = FleckMaker.GetDataStatic(vector2 + vector3 * 0.5f, map, Props.LaserLine_FleckDef);
					dataStatic2 = FleckMaker.GetDataStatic(vector2 + vector3 * 0.5f, map, Props.LaserLine_FleckDef);
				}
				else
				{
					dataStatic = FleckMaker.GetDataStatic(vector2 + vector3 * 0.5f, map, Props.LaserLine_FleckDef2);
					dataStatic2 = FleckMaker.GetDataStatic(vector2 + vector3 * 0.5f, map, Props.LaserLine_FleckDef2);
				}
				FleckCreationData dataStatic3 = FleckMaker.GetDataStatic(thing.DrawPos, map, Props.MuzzleGlow);
				dataStatic3.exactScale = new Vector3(1.53f, 1.53f, 1.53f);
				dataStatic3.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic3.instanceColor = new Color(Mathf.Max(num2 * 1.5f, 1f), Mathf.Max(num3 * 1.5f, 1f), Mathf.Max(num4 * 1.5f, 1f), 1f);
				map.flecks.CreateFleck(dataStatic3);
				FleckCreationData dataStatic4 = FleckMaker.GetDataStatic(thing.DrawPos, map, Props.MuzzleGlow);
				dataStatic4.exactScale = new Vector3(3f, 3f, 3f);
				dataStatic4.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic4.instanceColor = new Color(num2, num3, num4, 1f);
				map.flecks.CreateFleck(dataStatic4);
				FleckCreationData dataStatic5 = FleckMaker.GetDataStatic(vector2, map, Props.MuzzleGlow);
				dataStatic5.exactScale = new Vector3(1.33f, 1.33f, 1.33f);
				dataStatic5.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic5.instanceColor = new Color(Mathf.Max(num2 * 1.5f, 1f), Mathf.Max(num3 * 1.5f, 1f), Mathf.Max(num4 * 1.5f, 1f), 1f);
				map.flecks.CreateFleck(dataStatic5);
				FleckCreationData dataStatic6 = FleckMaker.GetDataStatic(vector2, map, Props.MuzzleGlow);
				dataStatic6.exactScale = new Vector3(2f, 2f, 2f);
				dataStatic6.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic6.instanceColor = new Color(num2, num3, num4, 1f);
				map.flecks.CreateFleck(dataStatic6);
				float randomInRange = new FloatRange(0.4f, 0.8f).RandomInRange;
				dataStatic.exactScale = new Vector3(x, 1f, randomInRange * 1.5f);
				dataStatic.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic.instanceColor = new Color(num2, num3, num4, color_Alpha * 0.3f);
				map.flecks.CreateFleck(dataStatic);
				dataStatic2.exactScale = new Vector3(x, 1f, randomInRange * 0.2f);
				dataStatic2.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic2.instanceColor = new Color(Mathf.Max(num2 * 1.1f, 1f), Mathf.Max(num3 * 1.1f, 1f), Mathf.Max(num4 * 1.1f, 1f), 0.5f);
				map.flecks.CreateFleck(dataStatic2);
			}
			if (Props.LaserFleck_Spark != null && Rand.Chance(Props.LaserFleck_Spark_Spawn_Chance))
			{
				float num5 = (vector - drawPos).AngleFlat();
				for (int j = 0; j < Props.LaserFleck_Spark_Num; j++)
				{
					float scale = Props.LaserFleck_Spark_Scale_Base + Rand.Range(0f - Props.LaserFleck_Spark_Scale_Deviation, Props.LaserFleck_Spark_Scale_Deviation);
					FleckCreationData dataStatic7 = FleckMaker.GetDataStatic(vector, map, Props.LaserFleck_Spark, scale);
					float num6 = num5 + Rand.Range(-30f, 30f);
					if (num6 > 180f)
					{
						num6 = num6 - 180f + -180f;
					}
					if (num6 < -180f)
					{
						num6 = num6 + 180f + 180f;
					}
					dataStatic7.velocityAngle = num6;
					dataStatic7.velocitySpeed = Rand.Range(5f, 10f);
					map.flecks.CreateFleck(dataStatic7);
				}
			}
			if (Props.SoundDef != null)
			{
				Props.SoundDef.PlayOneShot(new TargetInfo(Caster.Position, Caster.MapHeld));
			}
			return;
		}
		if (Props.IfCanScatter)
		{
			for (int k = 0; k < Props.ScatterNum; k++)
			{
				CellRect cellRect2 = CellRect.CenteredOn(center, (int)Props.ScatterRadius);
				cellRect2.ClipInsideMap(map);
				IntVec3 randomCell2 = cellRect2.RandomCell;
				GenExplosion.DoExplosion(randomCell2, map, Props.ScatterExplosionRadius, Props.ScatterExplosionDef, Caster, Props.ScatterExplosionDamage, Props.ScatterExplosionArmorPenetration, null, Verb.EquipmentSource.def, null, null, null, 0f, 0, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 0, 0f, damageFalloff: false, null, null, null, doVisualEffects: true, 1f, 0f, doSoundEffects: true, null, 0f);
				FleckMaker.ConnectingLine(vector, randomCell2.ToVector3Shifted(), Props.LaserFleck_ScatterLaser, map);
			}
		}
		bool flag3 = Props.LaserLine_FleckDef != null;
		bool flag4 = Props.LaserLine_FleckDef2 != null;
		if (flag3 && flag4)
		{
			float num7;
			float num8;
			float num9;
			if (!Props.RandomRGB)
			{
				num7 = (float)Props.Color_Red / 255f;
				num8 = (float)Props.Color_Green / 255f;
				num9 = (float)Props.Color_Blue / 255f;
			}
			else
			{
				num7 = (float)Rand.RangeInclusive(1, 255) / 255f;
				num8 = (float)Rand.RangeInclusive(1, 255) / 255f;
				num9 = (float)Rand.RangeInclusive(1, 255) / 255f;
			}
			float color_Alpha2 = Props.Color_Alpha;
			float turretyoffset2 = Props.turretyoffset;
			Vector3 vector4;
			if (Props.useyoffset)
			{
				vector4 = Caster.DrawPos;
				vector4.z += turretyoffset2;
			}
			else
			{
				float angle2 = (Caster.DrawPos - TargetPlace.CenterVector3).AngleFlat();
				vector4 = MYDE_ModFront.GetVector3_By_AngleFlat(Caster.DrawPos, Props.StartPositionOffset_Range, angle2);
			}
			Vector3 vector5 = TargetPlace.CenterVector3 - vector4;
			float x2 = vector5.MagnitudeHorizontal();
			FleckCreationData dataStatic8;
			FleckCreationData dataStatic9;
			if (Rand.RangeInclusive(0, 100) <= 85)
			{
				dataStatic8 = FleckMaker.GetDataStatic(vector4 + vector5 * 0.5f, map, Props.LaserLine_FleckDef);
				dataStatic9 = FleckMaker.GetDataStatic(vector4 + vector5 * 0.5f, map, Props.LaserLine_FleckDef);
			}
			else
			{
				dataStatic8 = FleckMaker.GetDataStatic(vector4 + vector5 * 0.5f, map, Props.LaserLine_FleckDef2);
				dataStatic9 = FleckMaker.GetDataStatic(vector4 + vector5 * 0.5f, map, Props.LaserLine_FleckDef2);
			}
			FleckCreationData dataStatic10 = FleckMaker.GetDataStatic(TargetPlace.CenterVector3, map, Props.MuzzleGlow);
			dataStatic10.exactScale = new Vector3(1.53f, 1.53f, 1.53f);
			dataStatic10.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic10.instanceColor = new Color(Mathf.Max(num7 * 1.5f, 1f), Mathf.Max(num8 * 1.5f, 1f), Mathf.Max(num9 * 1.5f, 1f), 1f);
			map.flecks.CreateFleck(dataStatic10);
			FleckCreationData dataStatic11 = FleckMaker.GetDataStatic(TargetPlace.CenterVector3, map, Props.MuzzleGlow);
			dataStatic11.exactScale = new Vector3(3f, 3f, 3f);
			dataStatic11.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic11.instanceColor = new Color(num7, num8, num9, 1f);
			map.flecks.CreateFleck(dataStatic11);
			FleckCreationData dataStatic12 = FleckMaker.GetDataStatic(vector4, map, Props.MuzzleGlow);
			dataStatic12.exactScale = new Vector3(1.33f, 1.33f, 1.33f);
			dataStatic12.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic12.instanceColor = new Color(Mathf.Max(num7 * 1.5f, 1f), Mathf.Max(num8 * 1.5f, 1f), Mathf.Max(num9 * 1.5f, 1f), 1f);
			map.flecks.CreateFleck(dataStatic12);
			FleckCreationData dataStatic13 = FleckMaker.GetDataStatic(vector4, map, Props.MuzzleGlow);
			dataStatic13.exactScale = new Vector3(2f, 2f, 2f);
			dataStatic13.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic13.instanceColor = new Color(num7, num8, num9, 1f);
			map.flecks.CreateFleck(dataStatic13);
			float randomInRange2 = new FloatRange(0.4f, 0.8f).RandomInRange;
			dataStatic8.exactScale = new Vector3(x2, 1f, randomInRange2 * 1.5f);
			dataStatic8.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic8.instanceColor = new Color(num7, num8, num9, color_Alpha2 * 0.3f);
			map.flecks.CreateFleck(dataStatic8);
			dataStatic9.exactScale = new Vector3(x2, 1f, randomInRange2 * 0.2f);
			dataStatic9.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic9.instanceColor = new Color(Mathf.Max(num7 * 1.1f, 1f), Mathf.Max(num8 * 1.1f, 1f), Mathf.Max(num9 * 1.1f, 1f), 0.5f);
			map.flecks.CreateFleck(dataStatic9);
		}
		if (Props.LaserFleck_Spark != null && Rand.Chance(Props.LaserFleck_Spark_Spawn_Chance))
		{
			float num10 = (vector - drawPos).AngleFlat();
			for (int l = 0; l < Props.LaserFleck_Spark_Num; l++)
			{
				float scale2 = Props.LaserFleck_Spark_Scale_Base + Rand.Range(0f - Props.LaserFleck_Spark_Scale_Deviation, Props.LaserFleck_Spark_Scale_Deviation);
				FleckCreationData dataStatic14 = FleckMaker.GetDataStatic(vector, map, Props.LaserFleck_Spark, scale2);
				float num11 = num10 + Rand.Range(-30f, 30f);
				if (num11 > 180f)
				{
					num11 = num11 - 180f + -180f;
				}
				if (num11 < -180f)
				{
					num11 = num11 + 180f + 180f;
				}
				dataStatic14.velocityAngle = num11;
				dataStatic14.velocitySpeed = Rand.Range(5f, 10f);
				map.flecks.CreateFleck(dataStatic14);
				map.flecks.CreateFleck(dataStatic14);
				map.flecks.CreateFleck(dataStatic14);
			}
		}
		if (Props.SoundDef != null)
		{
			Props.SoundDef.PlayOneShot(new TargetInfo(Caster.Position, Caster.MapHeld));
		}
	}

	public void TakeDamageToTarget(LocalTargetInfo TargetPlace, Vector3 SecondPos, Thing Caster, Verb Verb)
	{
		Thing thing;
		Vector3 vector;
		if (TargetPlace.HasThing)
		{
			thing = TargetPlace.Thing;
			vector = thing.DrawPos;
		}
		else
		{
			thing = null;
			vector = TargetPlace.CenterVector3;
		}
		Map map = Caster.Map;
		IntVec3 center = vector.ToIntVec3();
		DamageDef damageDef = Props.DamageDef;
		int num = Props.DamageNum * DMGmp * (int)QualityNum;
		float damageArmorPenetration = Props.DamageArmorPenetration;
		if (thing != null && damageDef != null)
		{
			float angleFlat = (TargetPlace.CenterVector3.ToIntVec3() - SecondPos.ToIntVec3()).AngleFlat;
			BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(Caster, thing, thing, Verb.EquipmentSource.def, null, null);
			DamageInfo dinfo = new DamageInfo(damageDef, num, damageArmorPenetration, angleFlat, Caster, null, Verb.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, thing);
			thing.TakeDamage(dinfo).AssociateWithLog(log);
			if (Props.IfSecondDamage)
			{
				DamageDef damageDef_B = Props.DamageDef_B;
				int damageNum_B = Props.DamageNum_B;
				float damageArmorPenetration_B = Props.DamageArmorPenetration_B;
				DamageInfo dinfo2 = new DamageInfo(damageDef_B, damageNum_B, damageArmorPenetration_B, angleFlat, Caster, null, Verb.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, thing);
				thing.TakeDamage(dinfo2).AssociateWithLog(log);
			}
			if (Props.IfCanScatter)
			{
				for (int i = 0; i < Props.ScatterNum; i++)
				{
					CellRect cellRect = CellRect.CenteredOn(center, (int)Props.ScatterRadius);
					cellRect.ClipInsideMap(map);
					IntVec3 randomCell = cellRect.RandomCell;
					GenExplosion.DoExplosion(randomCell, map, Props.ScatterExplosionRadius, Props.ScatterExplosionDef, Caster, Props.ScatterExplosionDamage, Props.ScatterExplosionArmorPenetration, null, Verb.EquipmentSource.def, null, null, null, 0f, 0, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 0, 0f, damageFalloff: false, null, null, null, doVisualEffects: true, 1f, 0f, doSoundEffects: true, null, 0f);
					FleckMaker.ConnectingLine(vector, randomCell.ToVector3Shifted(), Props.LaserFleck_ScatterLaser, map);
				}
			}
			bool flag = Props.LaserLine_FleckDef != null;
			bool flag2 = Props.LaserLine_FleckDef2 != null;
			if (flag && flag2)
			{
				float num2;
				float num3;
				float num4;
				if (!Props.RandomRGB)
				{
					num2 = (float)Props.Color_Red / 255f;
					num3 = (float)Props.Color_Green / 255f;
					num4 = (float)Props.Color_Blue / 255f;
				}
				else
				{
					num2 = (float)Rand.RangeInclusive(1, 255) / 255f;
					num3 = (float)Rand.RangeInclusive(1, 255) / 255f;
					num4 = (float)Rand.RangeInclusive(1, 255) / 255f;
				}
				float color_Alpha = Props.Color_Alpha;
				float turretyoffset = Props.turretyoffset;
				Vector3 vector2;
				if (Props.useyoffset)
				{
					vector2 = SecondPos;
					vector2.z += turretyoffset;
				}
				else
				{
					float angle = (SecondPos - thing.DrawPos).AngleFlat();
					vector2 = MYDE_ModFront.GetVector3_By_AngleFlat(SecondPos, Props.StartPositionOffset_Range, angle);
				}
				Vector3 vector3 = thing.DrawPos - vector2;
				float x = vector3.MagnitudeHorizontal();
				FleckCreationData dataStatic;
				FleckCreationData dataStatic2;
				if (Rand.RangeInclusive(0, 100) <= 85)
				{
					dataStatic = FleckMaker.GetDataStatic(vector2 + vector3 * 0.5f, map, Props.LaserLine_FleckDef);
					dataStatic2 = FleckMaker.GetDataStatic(vector2 + vector3 * 0.5f, map, Props.LaserLine_FleckDef);
				}
				else
				{
					dataStatic = FleckMaker.GetDataStatic(vector2 + vector3 * 0.5f, map, Props.LaserLine_FleckDef2);
					dataStatic2 = FleckMaker.GetDataStatic(vector2 + vector3 * 0.5f, map, Props.LaserLine_FleckDef2);
				}
				FleckCreationData dataStatic3 = FleckMaker.GetDataStatic(thing.DrawPos, map, Props.MuzzleGlow);
				dataStatic3.exactScale = new Vector3(1.53f, 1.53f, 1.53f);
				dataStatic3.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic3.instanceColor = new Color(Mathf.Max(num2 * 1.5f, 1f), Mathf.Max(num3 * 1.5f, 1f), Mathf.Max(num4 * 1.5f, 1f), 1f);
				map.flecks.CreateFleck(dataStatic3);
				FleckCreationData dataStatic4 = FleckMaker.GetDataStatic(thing.DrawPos, map, Props.MuzzleGlow);
				dataStatic4.exactScale = new Vector3(3f, 3f, 3f);
				dataStatic4.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic4.instanceColor = new Color(num2, num3, num4, 1f);
				map.flecks.CreateFleck(dataStatic4);
				FleckCreationData dataStatic5 = FleckMaker.GetDataStatic(vector2, map, Props.MuzzleGlow);
				dataStatic5.exactScale = new Vector3(1.33f, 1.33f, 1.33f);
				dataStatic5.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic5.instanceColor = new Color(Mathf.Max(num2 * 1.5f, 1f), Mathf.Max(num3 * 1.5f, 1f), Mathf.Max(num4 * 1.5f, 1f), 1f);
				map.flecks.CreateFleck(dataStatic5);
				FleckCreationData dataStatic6 = FleckMaker.GetDataStatic(vector2, map, Props.MuzzleGlow);
				dataStatic6.exactScale = new Vector3(2f, 2f, 2f);
				dataStatic6.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic6.instanceColor = new Color(num2, num3, num4, 1f);
				map.flecks.CreateFleck(dataStatic6);
				float randomInRange = new FloatRange(0.4f, 0.8f).RandomInRange;
				dataStatic.exactScale = new Vector3(x, 1f, randomInRange * 1.5f);
				dataStatic.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic.instanceColor = new Color(num2, num3, num4, color_Alpha * 0.3f);
				map.flecks.CreateFleck(dataStatic);
				dataStatic2.exactScale = new Vector3(x, 1f, randomInRange * 0.2f);
				dataStatic2.rotation = Mathf.Atan2(0f - vector3.z, vector3.x) * 57.29578f;
				dataStatic2.instanceColor = new Color(Mathf.Max(num2 * 1.1f, 1f), Mathf.Max(num3 * 1.1f, 1f), Mathf.Max(num4 * 1.1f, 1f), 0.5f);
				map.flecks.CreateFleck(dataStatic2);
			}
			if (Props.LaserFleck_Spark != null && Rand.Chance(Props.LaserFleck_Spark_Spawn_Chance))
			{
				float num5 = (vector - SecondPos).AngleFlat();
				for (int j = 0; j < Props.LaserFleck_Spark_Num; j++)
				{
					float scale = Props.LaserFleck_Spark_Scale_Base + Rand.Range(0f - Props.LaserFleck_Spark_Scale_Deviation, Props.LaserFleck_Spark_Scale_Deviation);
					FleckCreationData dataStatic7 = FleckMaker.GetDataStatic(vector, map, Props.LaserFleck_Spark, scale);
					float num6 = num5 + Rand.Range(-30f, 30f);
					if (num6 > 180f)
					{
						num6 = num6 - 180f + -180f;
					}
					if (num6 < -180f)
					{
						num6 = num6 + 180f + 180f;
					}
					dataStatic7.velocityAngle = num6;
					dataStatic7.velocitySpeed = Rand.Range(5f, 10f);
					map.flecks.CreateFleck(dataStatic7);
				}
			}
			if (Props.SoundDef != null)
			{
				Props.SoundDef.PlayOneShot(new TargetInfo(Caster.Position, Caster.MapHeld));
			}
			return;
		}
		if (Props.IfCanScatter)
		{
			for (int k = 0; k < Props.ScatterNum; k++)
			{
				CellRect cellRect2 = CellRect.CenteredOn(center, (int)Props.ScatterRadius);
				cellRect2.ClipInsideMap(map);
				IntVec3 randomCell2 = cellRect2.RandomCell;
				GenExplosion.DoExplosion(randomCell2, map, Props.ScatterExplosionRadius, Props.ScatterExplosionDef, Caster, Props.ScatterExplosionDamage, Props.ScatterExplosionArmorPenetration, null, Verb.EquipmentSource.def, null, null, null, 0f, 0, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 0, 0f, damageFalloff: false, null, null, null, doVisualEffects: true, 1f, 0f, doSoundEffects: true, null, 0f);
				FleckMaker.ConnectingLine(vector, randomCell2.ToVector3Shifted(), Props.LaserFleck_ScatterLaser, map);
			}
		}
		bool flag3 = Props.LaserLine_FleckDef != null;
		bool flag4 = Props.LaserLine_FleckDef2 != null;
		if (flag3 && flag4)
		{
			float num7;
			float num8;
			float num9;
			if (!Props.RandomRGB)
			{
				num7 = (float)Props.Color_Red / 255f;
				num8 = (float)Props.Color_Green / 255f;
				num9 = (float)Props.Color_Blue / 255f;
			}
			else
			{
				num7 = (float)Rand.RangeInclusive(1, 255) / 255f;
				num8 = (float)Rand.RangeInclusive(1, 255) / 255f;
				num9 = (float)Rand.RangeInclusive(1, 255) / 255f;
			}
			float color_Alpha2 = Props.Color_Alpha;
			float turretyoffset2 = Props.turretyoffset;
			Vector3 vector4;
			if (Props.useyoffset)
			{
				vector4 = SecondPos;
				vector4.z += turretyoffset2;
			}
			else
			{
				float angle2 = (SecondPos - TargetPlace.CenterVector3).AngleFlat();
				vector4 = MYDE_ModFront.GetVector3_By_AngleFlat(SecondPos, Props.StartPositionOffset_Range, angle2);
			}
			Vector3 vector5 = TargetPlace.CenterVector3 - vector4;
			float x2 = vector5.MagnitudeHorizontal();
			FleckCreationData dataStatic8;
			FleckCreationData dataStatic9;
			if (Rand.RangeInclusive(0, 100) <= 85)
			{
				dataStatic8 = FleckMaker.GetDataStatic(vector4 + vector5 * 0.5f, map, Props.LaserLine_FleckDef);
				dataStatic9 = FleckMaker.GetDataStatic(vector4 + vector5 * 0.5f, map, Props.LaserLine_FleckDef);
			}
			else
			{
				dataStatic8 = FleckMaker.GetDataStatic(vector4 + vector5 * 0.5f, map, Props.LaserLine_FleckDef2);
				dataStatic9 = FleckMaker.GetDataStatic(vector4 + vector5 * 0.5f, map, Props.LaserLine_FleckDef2);
			}
			FleckCreationData dataStatic10 = FleckMaker.GetDataStatic(TargetPlace.CenterVector3, map, Props.MuzzleGlow);
			dataStatic10.exactScale = new Vector3(1.53f, 1.53f, 1.53f);
			dataStatic10.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic10.instanceColor = new Color(Mathf.Max(num7 * 1.5f, 1f), Mathf.Max(num8 * 1.5f, 1f), Mathf.Max(num9 * 1.5f, 1f), 1f);
			map.flecks.CreateFleck(dataStatic10);
			FleckCreationData dataStatic11 = FleckMaker.GetDataStatic(TargetPlace.CenterVector3, map, Props.MuzzleGlow);
			dataStatic11.exactScale = new Vector3(3f, 3f, 3f);
			dataStatic11.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic11.instanceColor = new Color(num7, num8, num9, 1f);
			map.flecks.CreateFleck(dataStatic11);
			FleckCreationData dataStatic12 = FleckMaker.GetDataStatic(vector4, map, Props.MuzzleGlow);
			dataStatic12.exactScale = new Vector3(1.33f, 1.33f, 1.33f);
			dataStatic12.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic12.instanceColor = new Color(Mathf.Max(num7 * 1.5f, 1f), Mathf.Max(num8 * 1.5f, 1f), Mathf.Max(num9 * 1.5f, 1f), 1f);
			map.flecks.CreateFleck(dataStatic12);
			FleckCreationData dataStatic13 = FleckMaker.GetDataStatic(vector4, map, Props.MuzzleGlow);
			dataStatic13.exactScale = new Vector3(2f, 2f, 2f);
			dataStatic13.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic13.instanceColor = new Color(num7, num8, num9, 1f);
			map.flecks.CreateFleck(dataStatic13);
			float randomInRange2 = new FloatRange(0.4f, 0.8f).RandomInRange;
			dataStatic8.exactScale = new Vector3(x2, 1f, randomInRange2 * 1.5f);
			dataStatic8.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic8.instanceColor = new Color(num7, num8, num9, color_Alpha2 * 0.3f);
			map.flecks.CreateFleck(dataStatic8);
			dataStatic9.exactScale = new Vector3(x2, 1f, randomInRange2 * 0.2f);
			dataStatic9.rotation = Mathf.Atan2(0f - vector5.z, vector5.x) * 57.29578f;
			dataStatic9.instanceColor = new Color(Mathf.Max(num7 * 1.1f, 1f), Mathf.Max(num8 * 1.1f, 1f), Mathf.Max(num9 * 1.1f, 1f), 0.5f);
			map.flecks.CreateFleck(dataStatic9);
		}
		if (Props.LaserFleck_Spark != null && Rand.Chance(Props.LaserFleck_Spark_Spawn_Chance))
		{
			float num10 = (vector - SecondPos).AngleFlat();
			for (int l = 0; l < Props.LaserFleck_Spark_Num; l++)
			{
				float scale2 = Props.LaserFleck_Spark_Scale_Base + Rand.Range(0f - Props.LaserFleck_Spark_Scale_Deviation, Props.LaserFleck_Spark_Scale_Deviation);
				FleckCreationData dataStatic14 = FleckMaker.GetDataStatic(vector, map, Props.LaserFleck_Spark, scale2);
				float num11 = num10 + Rand.Range(-30f, 30f);
				if (num11 > 180f)
				{
					num11 = num11 - 180f + -180f;
				}
				if (num11 < -180f)
				{
					num11 = num11 + 180f + 180f;
				}
				dataStatic14.velocityAngle = num11;
				dataStatic14.velocitySpeed = Rand.Range(5f, 10f);
				map.flecks.CreateFleck(dataStatic14);
				map.flecks.CreateFleck(dataStatic14);
				map.flecks.CreateFleck(dataStatic14);
			}
		}
		if (Props.SoundDef != null)
		{
			Props.SoundDef.PlayOneShot(new TargetInfo(Caster.Position, Caster.MapHeld));
		}
	}
}
