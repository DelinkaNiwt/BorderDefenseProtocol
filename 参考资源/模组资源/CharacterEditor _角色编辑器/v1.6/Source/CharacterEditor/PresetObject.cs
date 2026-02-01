using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal class PresetObject
{
	internal enum Param
	{
		P00_defName,
		P01_label,
		P02_techLevel,
		P03_tradeability,
		P04_stealable,
		P05_costStuffCount,
		P06_stuffCategories,
		P07_statBases,
		P08_statOffsets,
		P09_costList,
		P10_costListDifficulty,
		P11_tradeTags,
		P12_weaponTags,
		P13_bulletDefName,
		P14_bulletDamageDef,
		P15_bulletDamageAmountBase,
		P16_bulletSpeed,
		P17_bulletStoppingPower,
		P18_bulletArmorPenetrationBase,
		P19_bulletExplosionRadius,
		P20_bulletExplosionDelay,
		P21_bulletNumExtraHitCells,
		P22_bulletPreExplosionSpawnThingDef,
		P23_bulletPreExplosionSpawnThingCount,
		P24_bulletPreExplosionSpawnChance,
		P25_bulletPostExplosionSpawnThingDef,
		P26_bulletPostExplosionSpawnThingCount,
		P27_bulletPostExplosionSpawnChance,
		P28_bulletPostExplosionGasType,
		P29_bulletExplosionEffect,
		P30_bulletLandedEffect,
		P35_bulletFlyOverhead,
		P36_bulletSoundExplode,
		P37_bulletSoundImpact,
		P38_bulletDamageToCellsNeighbors,
		P39_beamTargetsGround,
		P40_ticksBetweenBurstShots,
		P41_burstShotCount,
		P42_weaponRange,
		P43_weaponMinRange,
		P44_forcedMissRadius,
		P45_defaultCooldownTime,
		P46_warmupTime,
		P47_baseAccuracyTouch,
		P48_baseAccuracyShort,
		P49_baseAccuracyMedium,
		P50_baseAccuracyLong,
		P51_forcedMissRadiusMortar,
		P52_requireLineOfSight,
		P53_targetGround,
		P54_beamWidth,
		P55_beamFullWidthRange,
		P56_beamDamageDef,
		P57_consumeFuelPerShot,
		P58_consumeFuelPerBurst,
		P61_soundCast,
		P62_soundAiming,
		P63_soundCastBeam,
		P64_soundCastTail,
		P65_soundLanding,
		P66_turretWarmupTimeMin,
		P67_turretWarmupTimeMax,
		P68_turretBurstCooldownTime,
		P69_recipePrerequisite,
		P70_buildingPrerequisites,
		P71_CEfuelCapacity,
		P72_CEreloadTime,
		P73_apparelLayer,
		P74_bodyPartGroup,
		P75_apparelTags,
		P76_outfitTags,
		P77_stackLimit
	}

	internal const string CO_DAMAGEAMOUNTBASE = "damageAmountBase";

	internal const string CO_ARMORPENETRATIONBASE = "armorPenetrationBase";

	internal const string CO_FORCEDMISSRADIUS = "forcedMissRadius";

	internal const string CO_FORCEDMISSRADIUSCLASSICMORTARS = "forcedMissRadiusClassicMortars";

	internal const string CO_ISMORTAR = "isMortar";

	internal SortedDictionary<Param, string> dicParams;

	internal ThingDef def = null;

	internal static OptionS optionS => OptionS.CUSTOMOBJECT;

	public string AsString => Preset.AsString(dicParams);

	public static Dictionary<string, PresetObject> AllDefaultObjects => CEditor.API.Get<Dictionary<string, PresetObject>>(EType.ObjectPreset);

	public static Dictionary<string, PresetObject> AllDefaultTurrets => CEditor.API.Get<Dictionary<string, PresetObject>>(EType.TurretPreset);

	public static HashSet<ThingDef> ListAllObjects => ThingTool.ListOfItems(null, null, ThingCategory.None);

	public static HashSet<ThingDef> ListAllTurrets => DefTool.ListBy((ThingDef x) => !x.defName.NullOrEmpty() && x.IsTurret() && !x.label.NullOrEmpty() && x.building != null && x.building.turretGunDef != null);

	internal static Dictionary<string, ThingDef> DicGunAndTurret
	{
		get
		{
			Dictionary<string, ThingDef> dictionary = new Dictionary<string, ThingDef>();
			HashSet<ThingDef> listAllTurrets = ListAllTurrets;
			if (listAllTurrets.NullOrEmpty())
			{
				return dictionary;
			}
			foreach (ThingDef item in listAllTurrets)
			{
				dictionary.Add(item.defName, item.building.turretGunDef);
			}
			return dictionary;
		}
	}

	private VerbProperties sverb => def.Verbs[0];

	private ProjectileProperties sbullet => def.Verbs[0].defaultProjectile.projectile;

	private bool HasDefaultProjectile => def.Verbs.Count > 0 && def.Verbs[0].defaultProjectile != null;

	public void SaveCustom()
	{
		if (def != null)
		{
			CEditor.API.SetCustom(optionS, AsString, def.defName);
		}
		MessageTool.Show(def.defName + " " + Label.SETTINGSSAVED);
	}

	public static Dictionary<string, PresetObject> CreateDefaultObjects()
	{
		return Preset.CreateDefaults(ListAllObjects, (ThingDef s) => s.defName, (ThingDef x) => new PresetObject(x), "objects");
	}

	public static Dictionary<string, PresetObject> CreateDefaultTurrets()
	{
		return Preset.CreateDefaults(ListAllTurrets, (ThingDef s) => s.defName, (ThingDef x) => new PresetObject(x), "turrets");
	}

	public static void LoadAllModifications(string custom)
	{
		Preset.LoadAllModifications(custom, delegate(string s)
		{
			new PresetObject(s);
		}, "objects");
	}

	public static void ResetAllToDefaults()
	{
		Preset.ResetAllToDefault(AllDefaultObjects, delegate(PresetObject p)
		{
			p.FromDictionary();
		}, optionS, "objects");
		Preset.ResetAllToDefault(AllDefaultTurrets, delegate(PresetObject p)
		{
			p.FromDictionary();
		}, optionS, "turrets");
	}

	public static void ResetToDefault(string defName)
	{
		if (AllDefaultTurrets.ContainsKey(defName))
		{
			Preset.ResetToDefault(AllDefaultTurrets, delegate(PresetObject p)
			{
				p.FromDictionary();
			}, optionS, defName);
		}
		else if (AllDefaultObjects.ContainsKey(defName))
		{
			Preset.ResetToDefault(AllDefaultObjects, delegate(PresetObject p)
			{
				p.FromDictionary();
			}, optionS, defName);
		}
		else
		{
			MessageTool.Show("default preset not found!", MessageTypeDefOf.RejectInput);
		}
	}

	public static void SaveModification(ThingDef s)
	{
		new PresetObject(s).SaveCustom();
	}

	internal PresetObject(ThingDef gun)
	{
		if (gun == null)
		{
			return;
		}
		ThingDef turretDef = gun.GetTurretDef();
		def = ((turretDef == null || DicGunAndTurret.Values.Contains(gun)) ? gun : turretDef);
		if (def == null)
		{
			return;
		}
		dicParams = new SortedDictionary<Param, string>();
		try
		{
			dicParams.Add(Param.P00_defName, def.defName);
			dicParams.Add(Param.P01_label, def.label);
			SortedDictionary<Param, string> sortedDictionary = dicParams;
			int techLevel = (int)def.techLevel;
			sortedDictionary.Add(Param.P02_techLevel, techLevel.ToString());
			SortedDictionary<Param, string> sortedDictionary2 = dicParams;
			techLevel = (int)def.tradeability;
			sortedDictionary2.Add(Param.P03_tradeability, techLevel.ToString());
			dicParams.Add(Param.P04_stealable, def.stealable.ToString());
			dicParams.Add(Param.P05_costStuffCount, def.costStuffCount.ToString());
			dicParams.Add(Param.P06_stuffCategories, def.stuffCategories.ListToString());
			dicParams.Add(Param.P07_statBases, def.statBases.ListToString());
			dicParams.Add(Param.P08_statOffsets, def.equippedStatOffsets.ListToString());
			dicParams.Add(Param.P09_costList, def.costList.ListToString());
			dicParams.Add(Param.P10_costListDifficulty, (def.costListForDifficulty != null) ? def.costListForDifficulty.costList.ListToString() : "");
			dicParams.Add(Param.P11_tradeTags, def.tradeTags.ListToString());
			dicParams.Add(Param.P12_weaponTags, def.weaponTags.ListToString());
			bool flag = def.HasProjectile();
			dicParams.Add(Param.P13_bulletDefName, HasDefaultProjectile ? sverb.defaultProjectile.defName : "");
			dicParams.Add(Param.P14_bulletDamageDef, (!flag) ? "" : ((sbullet.damageDef != null) ? sbullet.damageDef.defName : ""));
			dicParams.Add(Param.P15_bulletDamageAmountBase, flag ? sbullet.GetMemberValueAsString<int>("damageAmountBase", "") : "");
			dicParams.Add(Param.P16_bulletSpeed, flag ? sbullet.speed.ToString() : "");
			dicParams.Add(Param.P17_bulletStoppingPower, flag ? sbullet.stoppingPower.ToString() : "");
			dicParams.Add(Param.P18_bulletArmorPenetrationBase, flag ? sbullet.GetMemberValueAsString<float>("armorPenetrationBase", "") : "");
			dicParams.Add(Param.P19_bulletExplosionRadius, flag ? sbullet.explosionRadius.ToString() : "");
			dicParams.Add(Param.P20_bulletExplosionDelay, flag ? sbullet.explosionDelay.ToString() : "");
			dicParams.Add(Param.P21_bulletNumExtraHitCells, flag ? sbullet.numExtraHitCells.ToString() : "");
			dicParams.Add(Param.P22_bulletPreExplosionSpawnThingDef, (!flag) ? "" : ((sbullet.preExplosionSpawnThingDef != null) ? sbullet.preExplosionSpawnThingDef.defName : ""));
			dicParams.Add(Param.P23_bulletPreExplosionSpawnThingCount, flag ? sbullet.preExplosionSpawnThingCount.ToString() : "");
			dicParams.Add(Param.P24_bulletPreExplosionSpawnChance, flag ? sbullet.preExplosionSpawnChance.ToString() : "");
			dicParams.Add(Param.P25_bulletPostExplosionSpawnThingDef, (!flag) ? "" : ((sbullet.postExplosionSpawnThingDef != null) ? sbullet.postExplosionSpawnThingDef.defName : ""));
			dicParams.Add(Param.P26_bulletPostExplosionSpawnThingCount, flag ? sbullet.postExplosionSpawnThingCount.ToString() : "");
			dicParams.Add(Param.P27_bulletPostExplosionSpawnChance, flag ? sbullet.postExplosionSpawnChance.ToString() : "");
			dicParams.Add(Param.P28_bulletPostExplosionGasType, (!flag) ? "" : (sbullet.postExplosionGasType.HasValue ? ((int)sbullet.postExplosionGasType.Value).ToString() : ""));
			dicParams.Add(Param.P29_bulletExplosionEffect, (!flag) ? "" : ((sbullet.explosionEffect != null) ? sbullet.explosionEffect.defName : ""));
			dicParams.Add(Param.P30_bulletLandedEffect, (!flag) ? "" : ((sbullet.landedEffecter != null) ? sbullet.landedEffecter.defName : ""));
			dicParams.Add(Param.P35_bulletFlyOverhead, flag ? sbullet.flyOverhead.ToString() : "");
			dicParams.Add(Param.P36_bulletSoundExplode, (!flag) ? "" : ((sbullet.soundExplode != null) ? sbullet.soundExplode.defName : ""));
			dicParams.Add(Param.P37_bulletSoundImpact, (!flag) ? "" : ((sbullet.soundImpact != null) ? sbullet.soundImpact.defName : ""));
			dicParams.Add(Param.P38_bulletDamageToCellsNeighbors, flag ? sbullet.applyDamageToExplosionCellsNeighbors.ToString() : "");
			bool flag2 = def.HasVerb();
			dicParams.Add(Param.P39_beamTargetsGround, flag2 ? sverb.beamTargetsGround.ToString() : "");
			dicParams.Add(Param.P40_ticksBetweenBurstShots, flag2 ? sverb.ticksBetweenBurstShots.ToString() : "");
			dicParams.Add(Param.P41_burstShotCount, flag2 ? sverb.burstShotCount.ToString() : "");
			dicParams.Add(Param.P42_weaponRange, flag2 ? sverb.range.ToString() : "");
			dicParams.Add(Param.P43_weaponMinRange, flag2 ? sverb.minRange.ToString() : "");
			dicParams.Add(Param.P44_forcedMissRadius, flag2 ? sverb.GetMemberValueAsString<float>("forcedMissRadius", "") : "");
			dicParams.Add(Param.P45_defaultCooldownTime, flag2 ? sverb.defaultCooldownTime.ToString() : "");
			dicParams.Add(Param.P46_warmupTime, flag2 ? sverb.warmupTime.ToString() : "");
			dicParams.Add(Param.P47_baseAccuracyTouch, flag2 ? sverb.accuracyTouch.ToString() : "");
			dicParams.Add(Param.P48_baseAccuracyShort, flag2 ? sverb.accuracyShort.ToString() : "");
			dicParams.Add(Param.P49_baseAccuracyMedium, flag2 ? sverb.accuracyMedium.ToString() : "");
			dicParams.Add(Param.P50_baseAccuracyLong, flag2 ? sverb.accuracyLong.ToString() : "");
			bool flag3 = flag2 && sverb.GetMemberValue("isMortar", fallback: false);
			dicParams.Add(Param.P51_forcedMissRadiusMortar, (flag2 && turretDef != null && flag3) ? sverb.GetMemberValueAsString<float>("forcedMissRadiusClassicMortars", "") : "");
			dicParams.Add(Param.P52_requireLineOfSight, flag2 ? sverb.requireLineOfSight.ToString() : "");
			dicParams.Add(Param.P53_targetGround, def.GetWeaponTargetGround().ToString());
			dicParams.Add(Param.P54_beamWidth, flag2 ? sverb.beamWidth.ToString() : "");
			dicParams.Add(Param.P55_beamFullWidthRange, flag2 ? sverb.beamFullWidthRange.ToString() : "");
			dicParams.Add(Param.P56_beamDamageDef, (!flag2) ? "" : ((sverb.beamDamageDef != null) ? sverb.beamDamageDef.defName : ""));
			dicParams.Add(Param.P57_consumeFuelPerShot, flag2 ? sverb.consumeFuelPerShot.ToString() : "");
			dicParams.Add(Param.P58_consumeFuelPerBurst, flag2 ? sverb.consumeFuelPerBurst.ToString() : "");
			dicParams.Add(Param.P61_soundCast, (!flag2) ? "" : ((sverb.soundCast != null) ? sverb.soundCast.defName : ""));
			dicParams.Add(Param.P62_soundAiming, (!flag2) ? "" : ((sverb.soundAiming != null) ? sverb.soundAiming.defName : ""));
			dicParams.Add(Param.P63_soundCastBeam, (!flag2) ? "" : ((sverb.soundCastBeam != null) ? sverb.soundCastBeam.defName : ""));
			dicParams.Add(Param.P64_soundCastTail, (!flag2) ? "" : ((sverb.soundCastTail != null) ? sverb.soundCastTail.defName : ""));
			dicParams.Add(Param.P65_soundLanding, (!flag2) ? "" : ((sverb.soundLanding != null) ? sverb.soundLanding.defName : ""));
			bool flag4 = turretDef != null && turretDef.building != null;
			dicParams.Add(Param.P66_turretWarmupTimeMin, flag4 ? turretDef.building.turretBurstWarmupTime.min.ToString() : "");
			dicParams.Add(Param.P67_turretWarmupTimeMax, flag4 ? turretDef.building.turretBurstWarmupTime.max.ToString() : "");
			dicParams.Add(Param.P68_turretBurstCooldownTime, flag4 ? turretDef.building.turretBurstCooldownTime.ToString() : "");
			dicParams.Add(Param.P69_recipePrerequisite, (def.recipeMaker != null) ? def.recipeMaker.researchPrerequisite.SDefname() : "");
			dicParams.Add(Param.P70_buildingPrerequisites, def.researchPrerequisites.ListToString());
			dicParams.Add(Param.P71_CEfuelCapacity, WeaponTool.GetCompRefuelable(turretDef, gun).ToString());
			dicParams.Add(Param.P72_CEreloadTime, WeaponTool.GetCompReloadTime(turretDef, gun).ToString());
			bool flag5 = def.apparel != null;
			dicParams.Add(Param.P73_apparelLayer, flag5 ? def.apparel.layers.ListToString() : "");
			dicParams.Add(Param.P74_bodyPartGroup, flag5 ? def.apparel.bodyPartGroups.ListToString() : "");
			dicParams.Add(Param.P75_apparelTags, flag5 ? def.apparel.tags.ListToString() : "");
			dicParams.Add(Param.P76_outfitTags, flag5 ? def.apparel.defaultOutfitTags.ListToString() : "");
			dicParams.Add(Param.P77_stackLimit, def.stackLimit.ToString());
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
		}
	}

	internal PresetObject(string custom)
	{
		if (Preset.LoadModification(custom, ref dicParams) && FromDictionary())
		{
			Log.Message(def.defName + " " + Label.MODIFICATIONLOADED);
		}
	}

	private bool FromDictionary()
	{
		ThingDef thingDef = DefTool.GetDef<ThingDef>(dicParams.GetValue(Param.P00_defName));
		if (thingDef == null)
		{
			return false;
		}
		try
		{
			ThingDef turretDef = thingDef.GetTurretDef();
			def = ((turretDef == null || DicGunAndTurret.Values.Contains(thingDef)) ? thingDef : turretDef);
			if (def == null)
			{
				return false;
			}
			def.label = dicParams.GetValue(Param.P01_label);
			if (Enum.TryParse<TechLevel>(dicParams.GetValue(Param.P02_techLevel), out var result))
			{
				def.techLevel = result;
			}
			if (Enum.TryParse<Tradeability>(dicParams.GetValue(Param.P03_tradeability), out var result2))
			{
				def.tradeability = result2;
			}
			def.stealable = dicParams.GetValue(Param.P04_stealable).AsBool();
			def.costStuffCount = dicParams.GetValue(Param.P05_costStuffCount).AsInt32();
			def.stuffCategories = dicParams.GetValue(Param.P06_stuffCategories).StringToList<StuffCategoryDef>();
			def.statBases = dicParams.GetValue(Param.P07_statBases).StringToListNonDef<StatModifier>();
			def.equippedStatOffsets = dicParams.GetValue(Param.P08_statOffsets).StringToListNonDef<StatModifier>();
			def.costList = dicParams.GetValue(Param.P09_costList).StringToListNonDef<ThingDefCountClass>();
			if (!dicParams.GetValue(Param.P10_costListDifficulty).NullOrEmpty())
			{
				if (def.costListForDifficulty == null)
				{
					def.costListForDifficulty = new CostListForDifficulty();
				}
				def.costListForDifficulty.costList = dicParams.GetValue(Param.P10_costListDifficulty).StringToListNonDef<ThingDefCountClass>();
			}
			def.tradeTags = dicParams.GetValue(Param.P11_tradeTags).StringToList();
			def.weaponTags = dicParams.GetValue(Param.P12_weaponTags).StringToList();
			def.stackLimit = dicParams.GetValue(Param.P77_stackLimit).AsInt32();
			def.UpdateStackLimit();
			if (def.HasVerb())
			{
				sverb.defaultProjectile = DefTool.GetDef<ThingDef>(dicParams.GetValue(Param.P13_bulletDefName));
				if (def.HasProjectile())
				{
					sbullet.damageDef = DefTool.GetDef<DamageDef>(dicParams.GetValue(Param.P14_bulletDamageDef));
					sbullet.SetMemberValue("damageAmountBase", dicParams.GetValue(Param.P15_bulletDamageAmountBase).AsInt32());
					sbullet.speed = dicParams.GetValue(Param.P16_bulletSpeed).AsFloat();
					sbullet.stoppingPower = dicParams.GetValue(Param.P17_bulletStoppingPower).AsFloat();
					sbullet.SetMemberValue("armorPenetrationBase", dicParams.GetValue(Param.P18_bulletArmorPenetrationBase).AsFloat());
					sbullet.explosionRadius = dicParams.GetValue(Param.P19_bulletExplosionRadius).AsFloat();
					sbullet.explosionDelay = dicParams.GetValue(Param.P20_bulletExplosionDelay).AsInt32();
					sbullet.numExtraHitCells = dicParams.GetValue(Param.P21_bulletNumExtraHitCells).AsInt32();
					sbullet.preExplosionSpawnThingDef = DefTool.GetDef<ThingDef>(dicParams.GetValue(Param.P22_bulletPreExplosionSpawnThingDef));
					sbullet.preExplosionSpawnThingCount = dicParams.GetValue(Param.P23_bulletPreExplosionSpawnThingCount).AsInt32();
					sbullet.preExplosionSpawnChance = dicParams.GetValue(Param.P24_bulletPreExplosionSpawnChance).AsFloat();
					sbullet.postExplosionSpawnThingDef = DefTool.GetDef<ThingDef>(dicParams.GetValue(Param.P25_bulletPostExplosionSpawnThingDef));
					sbullet.postExplosionSpawnThingCount = dicParams.GetValue(Param.P26_bulletPostExplosionSpawnThingCount).AsInt32();
					sbullet.postExplosionSpawnChance = dicParams.GetValue(Param.P27_bulletPostExplosionSpawnChance).AsFloat();
					if (Enum.TryParse<GasType>(dicParams.GetValue(Param.P28_bulletPostExplosionGasType), out var result3))
					{
						sbullet.postExplosionGasType = result3;
					}
					sbullet.explosionEffect = DefTool.GetDef<EffecterDef>(dicParams.GetValue(Param.P29_bulletExplosionEffect));
					sbullet.landedEffecter = DefTool.GetDef<EffecterDef>(dicParams.GetValue(Param.P30_bulletLandedEffect));
					sbullet.flyOverhead = dicParams.GetValue(Param.P35_bulletFlyOverhead).AsBool();
					sbullet.soundExplode = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P36_bulletSoundExplode));
					sbullet.soundImpact = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P37_bulletSoundImpact));
					sbullet.applyDamageToExplosionCellsNeighbors = dicParams.GetValue(Param.P38_bulletDamageToCellsNeighbors).AsBool();
				}
				sverb.beamTargetsGround = dicParams.GetValue(Param.P39_beamTargetsGround).AsBool();
				sverb.ticksBetweenBurstShots = dicParams.GetValue(Param.P40_ticksBetweenBurstShots).AsInt32();
				sverb.burstShotCount = dicParams.GetValue(Param.P41_burstShotCount).AsInt32();
				sverb.range = dicParams.GetValue(Param.P42_weaponRange).AsFloat();
				sverb.minRange = dicParams.GetValue(Param.P43_weaponMinRange).AsFloat();
				sverb.SetMemberValue("forcedMissRadius", dicParams.GetValue(Param.P44_forcedMissRadius).AsFloat());
				sverb.defaultCooldownTime = dicParams.GetValue(Param.P45_defaultCooldownTime).AsFloat();
				sverb.warmupTime = dicParams.GetValue(Param.P46_warmupTime).AsFloat();
				sverb.accuracyTouch = dicParams.GetValue(Param.P47_baseAccuracyTouch).AsFloat();
				sverb.accuracyShort = dicParams.GetValue(Param.P48_baseAccuracyShort).AsFloat();
				sverb.accuracyMedium = dicParams.GetValue(Param.P49_baseAccuracyMedium).AsFloat();
				sverb.accuracyLong = dicParams.GetValue(Param.P50_baseAccuracyLong).AsFloat();
				sverb.requireLineOfSight = dicParams.GetValue(Param.P52_requireLineOfSight).AsBool();
				def.SetTargetParams(dicParams.GetValue(Param.P53_targetGround).AsBool());
				sverb.beamWidth = dicParams.GetValue(Param.P54_beamWidth).AsFloat();
				sverb.beamFullWidthRange = dicParams.GetValue(Param.P55_beamFullWidthRange).AsFloat();
				sverb.beamDamageDef = DefTool.GetDef<DamageDef>(dicParams.GetValue(Param.P56_beamDamageDef));
				sverb.consumeFuelPerShot = dicParams.GetValue(Param.P57_consumeFuelPerShot).AsFloat();
				sverb.consumeFuelPerBurst = dicParams.GetValue(Param.P58_consumeFuelPerBurst).AsFloat();
				sverb.soundCast = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P61_soundCast));
				sverb.soundAiming = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P62_soundAiming));
				sverb.soundCastBeam = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P63_soundCastBeam));
				sverb.soundCastTail = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P64_soundCastTail));
				sverb.soundLanding = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P65_soundLanding));
				WeaponTool.SetCompRefuelable(null, def, dicParams.GetValue(Param.P71_CEfuelCapacity).AsInt32());
				WeaponTool.SetCompReloadTime(null, def, dicParams.GetValue(Param.P72_CEreloadTime).AsFloat());
				bool memberValue = sverb.GetMemberValue("isMortar", fallback: false);
				if (turretDef != null && memberValue)
				{
					sverb.SetMemberValue("forcedMissRadiusClassicMortars", dicParams.GetValue(Param.P51_forcedMissRadiusMortar).AsFloat());
				}
			}
			def.SetResearchPrerequisite(DefTool.GetDef<ResearchProjectDef>(dicParams.GetValue(Param.P69_recipePrerequisite)));
			def.researchPrerequisites = dicParams.GetValue(Param.P70_buildingPrerequisites).StringToList<ResearchProjectDef>();
			if (turretDef != null)
			{
				if (def.building != null)
				{
					def.building.turretBurstWarmupTime.min = dicParams.GetValue(Param.P66_turretWarmupTimeMin).AsFloat();
					def.building.turretBurstWarmupTime.max = dicParams.GetValue(Param.P67_turretWarmupTimeMax).AsFloat();
					def.building.turretBurstCooldownTime = dicParams.GetValue(Param.P68_turretBurstCooldownTime).AsFloat();
				}
				WeaponTool.SetCompRefuelable(def, thingDef, dicParams.GetValue(Param.P71_CEfuelCapacity).AsInt32());
				WeaponTool.SetCompReloadTime(def, thingDef, dicParams.GetValue(Param.P72_CEreloadTime).AsFloat());
				def.ResolveReferences();
			}
			if (def.apparel != null)
			{
				def.apparel.layers = dicParams.GetValue(Param.P73_apparelLayer).StringToList<ApparelLayerDef>();
				def.apparel.bodyPartGroups = dicParams.GetValue(Param.P74_bodyPartGroup).StringToList<BodyPartGroupDef>();
				def.apparel.tags = dicParams.GetValue(Param.P75_apparelTags).StringToList();
				def.apparel.defaultOutfitTags = dicParams.GetValue(Param.P76_outfitTags).StringToList();
			}
			if (HasDefaultProjectile)
			{
				sverb.defaultProjectile.ResolveReferences();
			}
			def.UpdateRecipes();
			def.ResolveReferences();
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
			return false;
		}
		return true;
	}
}
