using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Information;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 把武装构型声明适配为 Core 可消费的中性信息块。
    /// UI 只消费统一信息模型，不直接读取武装构型原始字段。
    /// </summary>
    internal static class ChipArmamentFormInformationAdapter
    {
        /// <summary>武装构型的 VerbProperties 覆盖适用的功能来源。</summary>
        private static readonly ChipInformationSourceKind[] VerbSourceKinds =
        {
            ChipInformationSourceKind.RangedVerb,
            ChipInformationSourceKind.Ability
        };

        /// <summary>统一攻击次数覆盖适用的功能来源。</summary>
        private static readonly ChipInformationSourceKind[] AttackSourceKinds =
        {
            ChipInformationSourceKind.RangedVerb,
            ChipInformationSourceKind.Ability,
            ChipInformationSourceKind.MeleeTool
        };

        /// <summary>近战执行间隔覆盖适用的功能来源。</summary>
        private static readonly ChipInformationSourceKind[] MeleeSourceKinds =
        {
            ChipInformationSourceKind.MeleeTool
        };

        /// <summary>
        /// 读取武装构型实际声明的玩家可见绝对值、倍率和执行覆盖。
        /// 最终动作块仍读取合成后的有效值；本块只保留构型声明本身，避免丢失倍率来源。
        /// </summary>
        internal static List<ChipInformationFactBlock> CreateBlocks(
            ChipArmamentFormDef armamentForm)
        {
            List<ChipInformationFactBlock> result =
                new List<ChipInformationFactBlock>();
            if (armamentForm == null)
            {
                return result;
            }

            ChipArmamentFormOverrides overrides = armamentForm.overrides;
            ProjectileOverrides projectileOverrides = armamentForm.projectileOverrides;
            if (overrides == null && projectileOverrides == null)
            {
                return result;
            }

            string sourceKey = "armament." + armamentForm.defName;
            List<ChipInformationFactValue> fields =
                new List<ChipInformationFactValue>();
            AddNumber(fields, "range", "BDP_Info_Field_Range", overrides?.range, sourceKey);
            AddNumber(fields, "minRange", "BDP_Info_Field_MinRange", overrides?.minRange, sourceKey);
            AddPercentage(fields, "accuracyTouch", "BDP_Info_Field_AccuracyTouch", overrides?.accuracyTouch, sourceKey);
            AddPercentage(fields, "accuracyShort", "BDP_Info_Field_AccuracyShort", overrides?.accuracyShort, sourceKey);
            AddPercentage(fields, "accuracyMedium", "BDP_Info_Field_AccuracyMedium", overrides?.accuracyMedium, sourceKey);
            AddPercentage(fields, "accuracyLong", "BDP_Info_Field_AccuracyLong", overrides?.accuracyLong, sourceKey);
            AddDuration(fields, "warmupTime", "BDP_Info_Field_Warmup", overrides?.warmupTime, sourceKey);
            AddDuration(fields, "defaultCooldownTime", "BDP_Info_Field_Cooldown", overrides?.defaultCooldownTime, sourceKey);
            AddInteger(fields, "burstShotCount", "BDP_Info_Field_BurstShotCount", overrides?.burstShotCount, sourceKey);
            AddInteger(fields, "ticksBetweenBurstShots", "BDP_Info_Field_TicksBetweenBurstShots", overrides?.ticksBetweenBurstShots, sourceKey, "BDP_Unit_GameTick");
            AddBoolean(fields, "forcedMissEvenDispersal", "BDP_Info_Field_ForcedMissEvenDispersal", overrides?.forcedMissEvenDispersal, sourceKey);
            AddBoolean(fields, "canGoWild", "BDP_Info_Field_CanGoWild", overrides?.canGoWild, sourceKey);
            AddNumber(fields, "muzzleFlashScale", "BDP_Info_Field_MuzzleFlashScale", overrides?.muzzleFlashScale, sourceKey);
            AddNumber(fields, "noiseRadius", "BDP_Info_Field_NoiseRadius", overrides?.noiseRadius, sourceKey);
            AddInteger(fields, "hitCount", "BDP_Info_Field_HitCount", overrides?.hitCount, sourceKey);
            AddInteger(fields, "hitIntervalTicks", "BDP_Info_Field_HitIntervalTicks", overrides?.hitIntervalTicks, sourceKey, "BDP_Unit_GameTick");
            string rhythmKey = overrides?.rhythm.HasValue == true
                ? "BDP_RangedShotRhythm_" + overrides.rhythm.Value
                : null;
            AddEnum(fields, "rhythm", "BDP_Info_Field_Rhythm",
                overrides?.rhythm.HasValue == true
                    ? ChipInformationValue.FromLocalizedEnum(overrides.rhythm.Value.ToString(), rhythmKey)
                    : null,
                sourceKey);
            AddEnum(fields, "defaultProjectile", "BDP_Info_Field_ProjectileDef",
                ChipInformationValue.FromDefReference(typeof(ThingDef), overrides?.defaultProjectile),
                sourceKey);

            AddNumber(fields, "rangeMultiplier", "BDP_Info_Field_RangeMultiplier", overrides?.rangeMultiplier, sourceKey);
            AddNumber(fields, "accuracyMultiplier", "BDP_Info_Field_AccuracyMultiplier", overrides?.accuracyMultiplier, sourceKey);
            AddNumber(fields, "warmupMultiplier", "BDP_Info_Field_WarmupMultiplier", overrides?.warmupMultiplier, sourceKey);
            AddNumber(fields, "cooldownMultiplier", "BDP_Info_Field_CooldownMultiplier", overrides?.cooldownMultiplier, sourceKey);
            AddNumber(fields, "burstShotCountMultiplier", "BDP_Info_Field_BurstShotCountMultiplier", overrides?.burstShotCountMultiplier, sourceKey);
            AddNumber(fields, "damageMultiplier", "BDP_Info_Field_DamageMultiplier", projectileOverrides?.damageMultiplier, sourceKey);
            AddNumber(fields, "speedMultiplier", "BDP_Info_Field_SpeedMultiplier", projectileOverrides?.speedMultiplier, sourceKey);
            AddNumber(fields, "stoppingPowerMultiplier", "BDP_Info_Field_StoppingPowerMultiplier", projectileOverrides?.stoppingPowerMultiplier, sourceKey);
            AddEnum(fields, "damageDefOverride", "BDP_Info_Field_DamageDefOverride",
                ChipInformationValue.FromDefReference(typeof(DamageDef), projectileOverrides?.damageDef),
                sourceKey);

            List<ChipInformationFactBlock> children =
                new List<ChipInformationFactBlock>();
            if (overrides?.originSpread != null)
            {
                List<ChipInformationFactValue> spreadFields =
                    new List<ChipInformationFactValue>();
                AddNumber(spreadFields, "LateralMin", "BDP_Info_Field_LateralMin", overrides.originSpread.LateralMin, sourceKey);
                AddNumber(spreadFields, "LateralMax", "BDP_Info_Field_LateralMax", overrides.originSpread.LateralMax, sourceKey);
                AddNumber(spreadFields, "ForwardMin", "BDP_Info_Field_ForwardMin", overrides.originSpread.ForwardMin, sourceKey);
                AddNumber(spreadFields, "ForwardMax", "BDP_Info_Field_ForwardMax", overrides.originSpread.ForwardMax, sourceKey);
                children.Add(new ChipInformationFactBlock(
                    "OriginSpread",
                    "originSpread",
                    "BDP_Info_Block_OriginSpread",
                    10,
                    spreadFields,
                    null));
            }

            if (fields.Count == 0 && children.Count == 0)
            {
                return result;
            }

            result.Add(new ChipInformationFactBlock(
                "ArmamentForm",
                "armamentForm",
                "BDP_Info_Block_ArmamentForm",
                15,
                fields,
                children));
            return result;
        }

        /// <summary>把构型原始覆盖值关联到同义功能属性。</summary>
        internal static List<ChipInformationPropertySource> CreateFeaturePropertySources(
            ChipArmamentFormDef armamentForm)
        {
            List<ChipInformationPropertySource> result =
                new List<ChipInformationPropertySource>();
            if (armamentForm == null)
            {
                return result;
            }

            ChipArmamentFormOverrides overrides = armamentForm.overrides;
            ProjectileOverrides projectileOverrides = armamentForm.projectileOverrides;
            string sourceKey = "armament." + armamentForm.defName;
            AddNumberSource(result, overrides?.range, "range", "BDP_Property_CastRange", sourceKey, "replace", VerbSourceKinds);
            AddNumberSource(result, overrides?.minRange, "minRange", "BDP_Property_MinimumCastRange", sourceKey, "replace", VerbSourceKinds);
            AddPercentageSource(result, overrides?.accuracyTouch, "accuracyTouch", "BDP_Property_AccuracyTouch", sourceKey, "replace", VerbSourceKinds);
            AddPercentageSource(result, overrides?.accuracyShort, "accuracyShort", "BDP_Property_AccuracyShort", sourceKey, "replace", VerbSourceKinds);
            AddPercentageSource(result, overrides?.accuracyMedium, "accuracyMedium", "BDP_Property_AccuracyMedium", sourceKey, "replace", VerbSourceKinds);
            AddPercentageSource(result, overrides?.accuracyLong, "accuracyLong", "BDP_Property_AccuracyLong", sourceKey, "replace", VerbSourceKinds);
            AddDurationSource(result, overrides?.warmupTime, "warmupTime", "BDP_Property_WarmupDuration", sourceKey, "replace", VerbSourceKinds);
            AddDurationSource(result, overrides?.defaultCooldownTime, "defaultCooldownTime", "BDP_Property_ActionCooldownDuration", sourceKey, "replace", VerbSourceKinds);
            bool executionHitCountOverridesBurst = overrides != null
                && overrides.hitCount.HasValue
                && overrides.hitCount.Value > 0;
            AddIntegerSource(result, overrides?.burstShotCount, "burstShotCount", "BDP_Property_AttackBurstCount", sourceKey, "replace", null, VerbSourceKinds, !executionHitCountOverridesBurst);
            AddIntegerSource(result, overrides?.hitCount, "hitCount", "BDP_Property_AttackBurstCount", sourceKey, "replace", null, AttackSourceKinds, executionHitCountOverridesBurst);
            AddIntegerSource(result, overrides?.ticksBetweenBurstShots, "ticksBetweenBurstShots", "BDP_Property_AttackBurstInterval", sourceKey, "replace", "BDP_Unit_GameTick", VerbSourceKinds);
            AddIntegerSource(result, overrides?.hitIntervalTicks, "hitIntervalTicks", "BDP_Property_AttackBurstInterval", sourceKey, "replace", "BDP_Unit_GameTick", MeleeSourceKinds);
            AddNumberSource(result, overrides?.rangeMultiplier, "rangeMultiplier", "BDP_Property_CastRange", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, overrides?.accuracyMultiplier, "accuracyMultiplier", "BDP_Property_AccuracyTouch", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, overrides?.accuracyMultiplier, "accuracyMultiplier", "BDP_Property_AccuracyShort", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, overrides?.accuracyMultiplier, "accuracyMultiplier", "BDP_Property_AccuracyMedium", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, overrides?.accuracyMultiplier, "accuracyMultiplier", "BDP_Property_AccuracyLong", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, overrides?.warmupMultiplier, "warmupMultiplier", "BDP_Property_WarmupDuration", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, overrides?.cooldownMultiplier, "cooldownMultiplier", "BDP_Property_ActionCooldownDuration", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, overrides?.burstShotCountMultiplier, "burstShotCountMultiplier", "BDP_Property_AttackBurstCount", sourceKey, "multiply", VerbSourceKinds, !executionHitCountOverridesBurst);
            AddProjectileReferenceSources(result, overrides?.defaultProjectile, sourceKey);
            AddNumberSource(result, projectileOverrides?.damageMultiplier, "damageMultiplier", "BDP_Property_DamageAmount", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, projectileOverrides?.speedMultiplier, "speedMultiplier", "BDP_Property_ProjectileSpeed", sourceKey, "multiply", VerbSourceKinds);
            AddNumberSource(result, projectileOverrides?.stoppingPowerMultiplier, "stoppingPowerMultiplier", "BDP_Property_StoppingPower", sourceKey, "multiply", VerbSourceKinds);
            AddDefReferenceSource(result, projectileOverrides?.damageDef, "damageDefOverride", "BDP_Property_DamageType", sourceKey, typeof(DamageDef), VerbSourceKinds);
            AddDefReferenceSource(result, projectileOverrides?.damageDef, "damageDefOverride", "BDP_Property_ArmorPenetration", sourceKey, typeof(DamageDef), VerbSourceKinds);
            if (overrides?.rhythm.HasValue == true)
            {
                string rhythmKey = "BDP_RangedShotRhythm_" + overrides.rhythm.Value;
                AddSource(result, "BDP_Property_RangedShotRhythm", "rhythm",
                    ChipInformationValue.FromLocalizedEnum(overrides.rhythm.Value.ToString(), rhythmKey),
                    sourceKey, "replace", null, VerbSourceKinds);
            }

            return result;
        }

        /// <summary>添加武装构型普通可空数值。</summary>
        private static void AddNumber(
            ICollection<ChipInformationFactValue> fields,
            string fieldKey,
            string labelKey,
            float? value,
            string sourceKey)
        {
            if (value.HasValue)
            {
                AddValue(fields, fieldKey, labelKey, ChipInformationValueKind.Number,
                    ChipInformationValue.FromNumber(value.Value), sourceKey);
            }
        }

        /// <summary>添加武装构型可空精度百分比。</summary>
        private static void AddPercentage(
            ICollection<ChipInformationFactValue> fields,
            string fieldKey,
            string labelKey,
            float? value,
            string sourceKey)
        {
            if (value.HasValue)
            {
                AddValue(fields, fieldKey, labelKey, ChipInformationValueKind.Percentage,
                    ChipInformationValue.FromPercentage(value.Value), sourceKey);
            }
        }

        /// <summary>添加武装构型可空持续时间。</summary>
        private static void AddDuration(
            ICollection<ChipInformationFactValue> fields,
            string fieldKey,
            string labelKey,
            float? value,
            string sourceKey)
        {
            if (value.HasValue)
            {
                AddValue(fields, fieldKey, labelKey, ChipInformationValueKind.Duration,
                    ChipInformationValue.FromDuration(value.Value), sourceKey);
            }
        }

        /// <summary>添加武装构型可空整数。</summary>
        private static void AddInteger(
            ICollection<ChipInformationFactValue> fields,
            string fieldKey,
            string labelKey,
            int? value,
            string sourceKey,
            string unitKey = null)
        {
            if (value.HasValue)
            {
                AddValue(fields, fieldKey, labelKey, ChipInformationValueKind.Integer,
                    ChipInformationValue.FromInteger(value.Value), sourceKey, unitKey);
            }
        }

        /// <summary>添加武装构型可空布尔。</summary>
        private static void AddBoolean(
            ICollection<ChipInformationFactValue> fields,
            string fieldKey,
            string labelKey,
            bool? value,
            string sourceKey)
        {
            if (value.HasValue)
            {
                AddValue(fields, fieldKey, labelKey, ChipInformationValueKind.Boolean,
                    ChipInformationValue.FromBoolean(value.Value), sourceKey);
            }
        }

        /// <summary>添加已经明确标记显示来源的武装构型枚举值。</summary>
        private static void AddEnum(
            ICollection<ChipInformationFactValue> fields,
            string fieldKey,
            string labelKey,
            ChipInformationValue value,
            string sourceKey)
        {
            if (value != null)
            {
                AddValue(fields, fieldKey, labelKey, ChipInformationValueKind.Enum,
                    value, sourceKey);
            }
        }

        /// <summary>创建武装构型字段并标记为变体来源。</summary>
        private static void AddValue(
            ICollection<ChipInformationFactValue> fields,
            string fieldKey,
            string labelKey,
            ChipInformationValueKind valueKind,
            ChipInformationValue value,
            string sourceKey,
            string unitKey = null)
        {
            fields.Add(new ChipInformationFactValue(
                fieldKey,
                labelKey,
                valueKind,
                value,
                unitKey,
                new[]
                {
                    new ChipInformationProvenance(
                        sourceKey + "." + fieldKey,
                        ChipInformationSourceKind.Variant,
                        ChipInformationContributionKind.Variant,
                        value,
                        null,
                        true,
                        unitKey)
                }));
        }

        /// <summary>添加有值时才存在的可空普通数值来源。</summary>
        private static void AddNumberSource(
            ICollection<ChipInformationPropertySource> target,
            float? value,
            string fieldKey,
            string propertyKey,
            string sourceKey,
            string operation,
            IEnumerable<ChipInformationSourceKind> applicableSourceKinds,
            bool applied = true)
        {
            if (value.HasValue)
            {
                AddSource(target, propertyKey, fieldKey,
                    ChipInformationValue.FromNumber(value.Value), sourceKey, operation, null, applicableSourceKinds, applied);
            }
        }

        /// <summary>添加有值时才存在的可空百分比来源。</summary>
        private static void AddPercentageSource(
            ICollection<ChipInformationPropertySource> target,
            float? value,
            string fieldKey,
            string propertyKey,
            string sourceKey,
            string operation,
            IEnumerable<ChipInformationSourceKind> applicableSourceKinds,
            bool applied = true)
        {
            if (value.HasValue)
            {
                AddSource(target, propertyKey, fieldKey,
                    ChipInformationValue.FromPercentage(value.Value), sourceKey, operation, null, applicableSourceKinds, applied);
            }
        }

        /// <summary>添加有值时才存在的可空持续时间来源。</summary>
        private static void AddDurationSource(
            ICollection<ChipInformationPropertySource> target,
            float? value,
            string fieldKey,
            string propertyKey,
            string sourceKey,
            string operation,
            IEnumerable<ChipInformationSourceKind> applicableSourceKinds,
            bool applied = true)
        {
            if (value.HasValue)
            {
                AddSource(target, propertyKey, fieldKey,
                    ChipInformationValue.FromDuration(value.Value), sourceKey, operation, null, applicableSourceKinds, applied);
            }
        }

        /// <summary>添加有值时才存在的可空整数来源。</summary>
        private static void AddIntegerSource(
            ICollection<ChipInformationPropertySource> target,
            int? value,
            string fieldKey,
            string propertyKey,
            string sourceKey,
            string operation,
            string unitKey = null,
            IEnumerable<ChipInformationSourceKind> applicableSourceKinds = null,
            bool applied = true)
        {
            if (value.HasValue)
            {
                AddSource(target, propertyKey, fieldKey,
                    ChipInformationValue.FromInteger(value.Value), sourceKey, operation, unitKey, applicableSourceKinds, applied);
            }
        }

        /// <summary>添加构型中的定义引用来源。</summary>
        private static void AddDefReferenceSource(
            ICollection<ChipInformationPropertySource> target,
            string defName,
            string fieldKey,
            string propertyKey,
            string sourceKey,
            System.Type definitionType,
            IEnumerable<ChipInformationSourceKind> applicableSourceKinds)
        {
            if (!string.IsNullOrWhiteSpace(defName))
            {
                bool applied = definitionType == typeof(DamageDef)
                    ? DefDatabase<DamageDef>.GetNamedSilentFail(defName) != null
                    : definitionType == typeof(ThingDef)
                        && DefDatabase<ThingDef>.GetNamedSilentFail(defName)?.projectile != null;
                AddSource(target, propertyKey, fieldKey,
                    ChipInformationValue.FromDefReference(definitionType, defName),
                    sourceKey, "replace", null, applicableSourceKinds, applied);
            }
        }

        /// <summary>把投射物定义引用关联到由其数值产生的功能属性。</summary>
        private static void AddProjectileReferenceSources(
            ICollection<ChipInformationPropertySource> target,
            string defName,
            string sourceKey)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return;
            }

            string[] propertyKeys =
            {
                "BDP_Property_DamageAmount",
                "BDP_Property_DamageType",
                "BDP_Property_ArmorPenetration",
                "BDP_Property_ProjectileSpeed",
                "BDP_Property_StoppingPower",
                "BDP_Property_EffectRadius",
                "BDP_Property_ProjectileFlightFeatures"
            };
            for (int index = 0; index < propertyKeys.Length; index++)
            {
                AddDefReferenceSource(
                    target,
                    defName,
                    "defaultProjectile",
                    propertyKeys[index],
                    sourceKey,
                    typeof(ThingDef),
                    VerbSourceKinds);
            }
        }

        /// <summary>创建构型属性的原始来源记录。</summary>
        private static void AddSource(
            ICollection<ChipInformationPropertySource> target,
            string propertyKey,
            string fieldKey,
            ChipInformationValue value,
            string sourceKey,
            string operation,
            string unitKey,
            IEnumerable<ChipInformationSourceKind> applicableSourceKinds = null,
            bool applied = true)
        {
            if (target == null || value == null)
            {
                return;
            }

            target.Add(new ChipInformationPropertySource(
                propertyKey,
                new ChipInformationProvenance(
                    sourceKey + "." + fieldKey,
                    ChipInformationSourceKind.Variant,
                    ChipInformationContributionKind.Variant,
                    value,
                    operation,
                    applied,
                    unitKey),
                applicableSourceKinds));
        }
    }
}
