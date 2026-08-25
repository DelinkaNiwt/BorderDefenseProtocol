using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.Combos;
using BDP.Core.Expressions;
using Verse;

using BDP.Content.Assembly.ChipManufacturing.Defs;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 武装型表达条目复制与覆盖服务。
    /// 将 ChipArmamentFormDef 的覆盖层应用到武装表达条目上。
    ///
    /// 规则：
    /// 1. 仅对 Kind=PrimaryVerb|SecondaryVerb 且 WeaponMode=Melee|Ranged 的条目生效。
    /// 2. 标量字段：武装型声明了就使用武装型值，未声明则保留动作原值。
    /// 3. 列表字段：按显式的 MergeStrategy 决定 Append 或 Replace。
    /// 4. 始终返回新构造的对象，不修改原始数据。
    /// </summary>
    public static class ChipArmamentFormExpressionService
    {
        /// <summary>
        /// 对条目列表直接执行合并。
        /// 当武装型为 null 时，只返回条目的深复制。
        /// </summary>
        /// <param name="entries">条目列表，不可为 null。</param>
        /// <param name="armamentForm">可选武装型，null 表示不覆盖。</param>
        /// <param name="idPrefix">为每条生成的条目 Id 添加的前缀。</param>
        /// <returns>合并后的条目列表。</returns>
        public static List<ChipExpressionEntryConfig> MergeEntries(
            List<ChipExpressionEntryConfig> entries,
            ChipArmamentFormDef armamentForm,
            string idPrefix)
        {
            if (entries == null || entries.Count == 0)
            {
                return new List<ChipExpressionEntryConfig>();
            }

            List<ChipExpressionEntryConfig> result = new List<ChipExpressionEntryConfig>();
            for (int i = 0; i < entries.Count; i++)
            {
                ChipExpressionEntryConfig source = entries[i];
                if (source == null) continue;

                ChipExpressionEntryConfig merged = CloneEntry(source, idPrefix);
                if (armamentForm != null && ShouldApplyArmamentFormOverride(source))
                {
                    ApplyArmamentFormOverrides(merged, armamentForm);
                }

                result.Add(merged);
            }

            return result;
        }

        /// <summary>
        /// 判断当前条目是否应接受武装型覆盖。
        /// 仅 Verb 通道 + 近战或远程模式才有此资格。
        /// </summary>
        private static bool ShouldApplyArmamentFormOverride(ChipExpressionEntryConfig entry)
        {
            if (entry.Kind != ChipExpressionEntryKindConfig.PrimaryVerb
                && entry.Kind != ChipExpressionEntryKindConfig.SecondaryVerb)
            {
                return false;
            }

            return entry.WeaponMode == VerbExpressionModeConfig.Melee
                || entry.WeaponMode == VerbExpressionModeConfig.Ranged;
        }

        /// <summary>
        /// 判断组合结果条目是否具备接受武装构型修正的资格。
        /// 组合结果只允许近战/远程主动作条目进入该业务修正入口。
        /// </summary>
        internal static bool ShouldApplyArmamentFormOverride(ComboExpressionEntryConfig entry)
        {
            if (entry == null
                || (entry.Kind != ChipExpressionEntryKindConfig.PrimaryVerb
                    && entry.Kind != ChipExpressionEntryKindConfig.SecondaryVerb))
            {
                return false;
            }

            return entry.WeaponMode == VerbExpressionModeConfig.Melee
                || entry.WeaponMode == VerbExpressionModeConfig.Ranged;
        }

        /// <summary>
        /// 对单条武装条目执行武装型覆盖。
        /// 标量字段逐字段 non-null 判断；列表字段按 MergeStrategy 处理。
        /// </summary>
        public static void ApplyArmamentFormOverrides(
            ChipExpressionEntryConfig entry,
            ChipArmamentFormDef armamentForm)
        {
            if (armamentForm == null)
            {
                return;
            }

            ChipArmamentFormOverrides ov = armamentForm.overrides;
            if (ov == null)
            {
                ApplyProjectileOverrides(entry, armamentForm);
                return;
            }

            // --- VerbProps 标量 ---
            // 只改贴图或近战 Tool 的构型不能凭空创建 VerbProperties；
            // 否则 new VerbProperties() 的原版默认值会冒充射程、精度和射击节奏。
            if (HasVerbPropertiesOverrides(ov))
            {
                if (entry.VerbProps == null)
                {
                    entry.VerbProps = new VerbProperties();
                }

                VerbProperties vp = entry.VerbProps;

                if (ov.range.HasValue) vp.range = ov.range.Value;
                if (ov.minRange.HasValue) vp.minRange = ov.minRange.Value;
                if (ov.accuracyTouch.HasValue) vp.accuracyTouch = ov.accuracyTouch.Value;
                if (ov.accuracyShort.HasValue) vp.accuracyShort = ov.accuracyShort.Value;
                if (ov.accuracyMedium.HasValue) vp.accuracyMedium = ov.accuracyMedium.Value;
                if (ov.accuracyLong.HasValue) vp.accuracyLong = ov.accuracyLong.Value;
                if (ov.warmupTime.HasValue) vp.warmupTime = ov.warmupTime.Value;
                if (ov.defaultCooldownTime.HasValue) vp.defaultCooldownTime = ov.defaultCooldownTime.Value;
                if (ov.burstShotCount.HasValue) vp.burstShotCount = ov.burstShotCount.Value;
                if (ov.ticksBetweenBurstShots.HasValue) vp.ticksBetweenBurstShots = ov.ticksBetweenBurstShots.Value;
                if (ov.forcedMissEvenDispersal.HasValue) vp.forcedMissEvenDispersal = ov.forcedMissEvenDispersal.Value;
                if (ov.canGoWild.HasValue) vp.canGoWild = ov.canGoWild.Value;
                if (ov.soundCast != null) vp.soundCast = SoundDef.Named(ov.soundCast);
                if (ov.soundCastTail != null) vp.soundCastTail = SoundDef.Named(ov.soundCastTail);
                if (ov.soundAiming != null) vp.soundAiming = SoundDef.Named(ov.soundAiming);
                if (ov.muzzleFlashScale.HasValue) vp.muzzleFlashScale = ov.muzzleFlashScale.Value;
                if (ov.noiseRadius.HasValue) vp.noiseRadius = ov.noiseRadius.Value;
                if (ov.rangedFireRulepack != null) vp.rangedFireRulepack = DefDatabase<RulePackDef>.GetNamed(ov.rangedFireRulepack);
                if (ov.defaultProjectile != null) vp.defaultProjectile = ThingDef.Named(ov.defaultProjectile);

                // --- 倍率覆盖（在绝对值覆盖之后乘法叠加） ---
                if (ov.rangeMultiplier.HasValue) vp.range *= ov.rangeMultiplier.Value;
                if (ov.accuracyMultiplier.HasValue)
                {
                    vp.accuracyTouch *= ov.accuracyMultiplier.Value;
                    vp.accuracyShort *= ov.accuracyMultiplier.Value;
                    vp.accuracyMedium *= ov.accuracyMultiplier.Value;
                    vp.accuracyLong *= ov.accuracyMultiplier.Value;
                }
                if (ov.warmupMultiplier.HasValue) vp.warmupTime *= ov.warmupMultiplier.Value;
                if (ov.cooldownMultiplier.HasValue) vp.defaultCooldownTime *= ov.cooldownMultiplier.Value;
                if (ov.burstShotCountMultiplier.HasValue) vp.burstShotCount = (int)(vp.burstShotCount * ov.burstShotCountMultiplier.Value);
            }

            // --- 统一攻击执行字段 ---
            if (ov.hitCount.HasValue || ov.hitIntervalTicks.HasValue)
            {
                if (entry.Execution == null)
                {
                    entry.Execution = new ChipAttackExecutionConfig();
                }

                if (ov.hitCount.HasValue)
                {
                    entry.Execution.HitCount = ov.hitCount.Value;
                }

                if (ov.hitIntervalTicks.HasValue)
                {
                    entry.Execution.HitIntervalTicks = ov.hitIntervalTicks.Value;
                }
            }

            // --- ProjectileOverrides（投射物属性覆盖） ---
            ApplyProjectileOverrides(entry, armamentForm);

            // --- Execution 覆盖 ---
            if (ov.rhythm.HasValue)
            {
                if (entry.Execution == null)
                {
                    entry.Execution = new ChipAttackExecutionConfig();
                }

                entry.Execution.Rhythm = ov.rhythm.Value;
            }

            if (ov.originSpread != null)
            {
                if (entry.Execution == null)
                {
                    entry.Execution = new ChipAttackExecutionConfig();
                }

                entry.Execution.OriginSpread = new ChipAttackOriginSpreadConfig
                {
                    LateralMin = ov.originSpread.LateralMin,
                    LateralMax = ov.originSpread.LateralMax,
                    ForwardMin = ov.originSpread.ForwardMin,
                    ForwardMax = ov.originSpread.ForwardMax
                };
            }

            // --- Presentation 覆盖 ---
            if (entry.Presentation == null)
            {
                entry.Presentation = new ExpressionPresentationConfig();
            }

            if (ov.visualPresetDefName != null)
            {
                entry.Presentation.VisualPresetDefName = ov.visualPresetDefName;
            }

            if (ov.visualGraphicOverrideDefName != null)
            {
                entry.Presentation.VisualGraphicOverrideDefName = ov.visualGraphicOverrideDefName;
            }

            if (ov.compositeVisualPresetDefName != null)
            {
                entry.Presentation.CompositeVisualPresetDefName = ov.compositeVisualPresetDefName;
            }

            if (ov.forceSuppressHostEquipment.HasValue)
            {
                entry.Presentation.ForceSuppressHostEquipment = ov.forceSuppressHostEquipment.Value;
            }

            if (ov.visualPriority.HasValue)
            {
                entry.Presentation.VisualPriority = ov.visualPriority.Value;
            }

            if (ov.manualEntryIconTexPath != null)
            {
                entry.Presentation.ManualEntryIconTexPath = ov.manualEntryIconTexPath;
            }

            // --- 近战工具字段 ---
            if (ov.tool != null)
            {
                entry.Tool = ov.tool;
            }

            if (ov.maneuver != null)
            {
                entry.Maneuver = ov.maneuver;
            }

            // --- RangedModules 列表 ---
            if (ov.rangedModules != null && ov.rangedModules.Count > 0)
            {
                if (entry.RangedModules == null)
                {
                    entry.RangedModules = new List<RangedModuleMountConfig>();
                }

                switch (ov.rangedModulesMerge)
                {
                    case MergeStrategy.Replace:
                        entry.RangedModules = CloneModuleList(ov.rangedModules);
                        break;
                    case MergeStrategy.Append:
                    default:
                        entry.RangedModules.AddRange(CloneModuleList(ov.rangedModules));
                        break;
                }
            }

            // --- Tools 列表 ---
            if (ov.tools != null && ov.tools.Count > 0)
            {
                if (entry.tools == null)
                {
                    entry.tools = new List<Tool>();
                }

                switch (ov.toolsMerge)
                {
                    case MergeStrategy.Replace:
                        entry.tools = CloneToolList(ov.tools);
                        break;
                    case MergeStrategy.Append:
                    default:
                        entry.tools.AddRange(CloneToolList(ov.tools));
                        break;
                }
            }
        }

        /// <summary>
        /// 对组合结果条目自身已显式声明的字段执行一次武装构型修正。
        /// 未声明字段保持 null，继续由组合结果的来源继承规则解析，避免重复影响来源结果。
        /// </summary>
        internal static void ApplyArmamentFormOverrides(
            ComboExpressionEntryConfig entry,
            ChipArmamentFormDef armamentForm)
        {
            if (!ShouldApplyArmamentFormOverride(entry)
                || armamentForm == null)
            {
                return;
            }

            ApplyProjectileOverrides(entry, armamentForm);
            if (armamentForm.overrides == null)
            {
                return;
            }

            ChipArmamentFormOverrides ov = armamentForm.overrides;
            ApplyComboVerbPropsOverrides(entry, ov);
            ApplyComboExecutionOverrides(entry, ov);
            ApplyComboPresentationOverrides(entry, ov);

            if (ov.tool != null)
            {
                entry.Tool = ov.tool;
            }

            if (ov.maneuver != null)
            {
                entry.Maneuver = ov.maneuver;
            }

            if (ov.rangedModules != null && ov.rangedModules.Count > 0)
            {
                if (entry.RangedModules == null)
                {
                    entry.RangedModules = new List<RangedModuleMountConfig>();
                }

                switch (ov.rangedModulesMerge)
                {
                    case MergeStrategy.Replace:
                        entry.RangedModules = CloneModuleList(ov.rangedModules);
                        break;
                    case MergeStrategy.Append:
                    default:
                        entry.RangedModules.AddRange(CloneModuleList(ov.rangedModules));
                        break;
                }
            }

            if (ov.tools != null && ov.tools.Count > 0)
            {
                if (entry.tools == null)
                {
                    entry.tools = new List<Tool>();
                }

                switch (ov.toolsMerge)
                {
                    case MergeStrategy.Replace:
                        entry.tools = CloneToolList(ov.tools);
                        break;
                    case MergeStrategy.Append:
                    default:
                        entry.tools.AddRange(CloneToolList(ov.tools));
                        break;
                }
            }
        }

        /// <summary>把构型的 VerbProperties 覆盖语义应用到组合结果显式覆盖层。</summary>
        private static void ApplyComboVerbPropsOverrides(
            ComboExpressionEntryConfig entry,
            ChipArmamentFormOverrides ov)
        {
            if (entry.VerbProps == null && !HasComboVerbPropertiesAbsoluteOverrides(ov))
            {
                return;
            }

            if (entry.VerbProps == null)
            {
                entry.VerbProps = new VerbPropsOverlay();
            }

            VerbPropsOverlay target = entry.VerbProps;
            if (ov.range.HasValue) target.range = ov.range.Value;
            if (ov.minRange.HasValue) target.minRange = ov.minRange.Value;
            if (ov.accuracyTouch.HasValue) target.accuracyTouch = ov.accuracyTouch.Value;
            if (ov.accuracyShort.HasValue) target.accuracyShort = ov.accuracyShort.Value;
            if (ov.accuracyMedium.HasValue) target.accuracyMedium = ov.accuracyMedium.Value;
            if (ov.accuracyLong.HasValue) target.accuracyLong = ov.accuracyLong.Value;
            if (ov.warmupTime.HasValue) target.warmupTime = ov.warmupTime.Value;
            if (ov.defaultCooldownTime.HasValue) target.defaultCooldownTime = ov.defaultCooldownTime.Value;
            if (ov.burstShotCount.HasValue) target.burstShotCount = ov.burstShotCount.Value;
            if (ov.ticksBetweenBurstShots.HasValue) target.ticksBetweenBurstShots = ov.ticksBetweenBurstShots.Value;
            if (ov.defaultProjectile != null)
            {
                target.defaultProjectile = ThingDef.Named(ov.defaultProjectile);
            }

            // 倍率只作用于组合结果条目已经显式声明的字段；未声明字段不被凭空创建。
            if (ov.rangeMultiplier.HasValue && target.range.HasValue)
            {
                target.range *= ov.rangeMultiplier.Value;
            }

            if (ov.accuracyMultiplier.HasValue)
            {
                if (target.accuracyTouch.HasValue) target.accuracyTouch *= ov.accuracyMultiplier.Value;
                if (target.accuracyShort.HasValue) target.accuracyShort *= ov.accuracyMultiplier.Value;
                if (target.accuracyMedium.HasValue) target.accuracyMedium *= ov.accuracyMultiplier.Value;
                if (target.accuracyLong.HasValue) target.accuracyLong *= ov.accuracyMultiplier.Value;
            }

            if (ov.warmupMultiplier.HasValue && target.warmupTime.HasValue)
            {
                target.warmupTime *= ov.warmupMultiplier.Value;
            }

            if (ov.cooldownMultiplier.HasValue && target.defaultCooldownTime.HasValue)
            {
                target.defaultCooldownTime *= ov.cooldownMultiplier.Value;
            }

            if (ov.burstShotCountMultiplier.HasValue && target.burstShotCount.HasValue)
            {
                target.burstShotCount = (int)(target.burstShotCount.Value * ov.burstShotCountMultiplier.Value);
            }
        }

        /// <summary>把构型的执行节奏覆盖语义应用到组合结果显式执行字段。</summary>
        private static void ApplyComboExecutionOverrides(
            ComboExpressionEntryConfig entry,
            ChipArmamentFormOverrides ov)
        {
            if (!ov.hitCount.HasValue
                && !ov.hitIntervalTicks.HasValue
                && !ov.rhythm.HasValue
                && ov.originSpread == null)
            {
                return;
            }

            if (entry.Execution == null)
            {
                entry.Execution = new ChipAttackExecutionConfig();
            }

            if (ov.hitCount.HasValue) entry.Execution.HitCount = ov.hitCount.Value;
            if (ov.hitIntervalTicks.HasValue) entry.Execution.HitIntervalTicks = ov.hitIntervalTicks.Value;
            if (ov.rhythm.HasValue) entry.Execution.Rhythm = ov.rhythm.Value;
            if (ov.originSpread != null)
            {
                entry.Execution.OriginSpread = new ChipAttackOriginSpreadConfig
                {
                    LateralMin = ov.originSpread.LateralMin,
                    LateralMax = ov.originSpread.LateralMax,
                    ForwardMin = ov.originSpread.ForwardMin,
                    ForwardMax = ov.originSpread.ForwardMax
                };
            }
        }

        /// <summary>把构型的表现覆盖语义应用到组合结果显式表现字段。</summary>
        private static void ApplyComboPresentationOverrides(
            ComboExpressionEntryConfig entry,
            ChipArmamentFormOverrides ov)
        {
            if (ov.visualPresetDefName == null
                && ov.visualGraphicOverrideDefName == null
                && ov.compositeVisualPresetDefName == null
                && !ov.forceSuppressHostEquipment.HasValue
                && !ov.visualPriority.HasValue
                && ov.manualEntryIconTexPath == null)
            {
                return;
            }

            if (entry.Presentation == null)
            {
                entry.Presentation = new ExpressionPresentationConfig();
            }

            if (ov.visualPresetDefName != null) entry.Presentation.VisualPresetDefName = ov.visualPresetDefName;
            if (ov.visualGraphicOverrideDefName != null) entry.Presentation.VisualGraphicOverrideDefName = ov.visualGraphicOverrideDefName;
            if (ov.compositeVisualPresetDefName != null) entry.Presentation.CompositeVisualPresetDefName = ov.compositeVisualPresetDefName;
            if (ov.forceSuppressHostEquipment.HasValue) entry.Presentation.ForceSuppressHostEquipment = ov.forceSuppressHostEquipment.Value;
            if (ov.visualPriority.HasValue) entry.Presentation.VisualPriority = ov.visualPriority.Value;
            if (ov.manualEntryIconTexPath != null) entry.Presentation.ManualEntryIconTexPath = ov.manualEntryIconTexPath;
        }

        /// <summary>判断构型是否声明了可创建组合结果显式 VerbProps 的绝对字段。</summary>
        private static bool HasComboVerbPropertiesAbsoluteOverrides(ChipArmamentFormOverrides ov)
        {
            return ov != null
                && (ov.range.HasValue
                    || ov.minRange.HasValue
                    || ov.accuracyTouch.HasValue
                    || ov.accuracyShort.HasValue
                    || ov.accuracyMedium.HasValue
                    || ov.accuracyLong.HasValue
                    || ov.warmupTime.HasValue
                    || ov.defaultCooldownTime.HasValue
                    || ov.burstShotCount.HasValue
                    || ov.ticksBetweenBurstShots.HasValue
                    || ov.defaultProjectile != null);
        }

        /// <summary>
        /// 对单条条目的投射物属性施加武装型覆盖。
        /// 对标 VerbProps 的逐字段 nullable 覆盖：声明了的才覆盖，未声明保留原值。
        /// </summary>
        private static void ApplyProjectileOverrides(
            ChipExpressionEntryConfig entry,
            ChipArmamentFormDef armamentForm)
        {
            entry.ProjectileOverrides = MergeProjectileOverrides(
                entry.ProjectileOverrides,
                armamentForm != null ? armamentForm.projectileOverrides : null);
        }

        /// <summary>
        /// 把构型投射物覆盖合并到组合条目。
        /// </summary>
        private static void ApplyProjectileOverrides(
            ComboExpressionEntryConfig entry,
            ChipArmamentFormDef armamentForm)
        {
            entry.ProjectileOverrides = MergeProjectileOverrides(
                entry.ProjectileOverrides,
                armamentForm != null ? armamentForm.projectileOverrides : null);
        }

        /// <summary>
        /// 按字段合并投射物覆盖；未声明字段保持目标原值。
        /// </summary>
        private static ProjectileOverrides MergeProjectileOverrides(
            ProjectileOverrides target,
            ProjectileOverrides source)
        {
            if (source == null)
            {
                return target;
            }

            if (target == null)
            {
                target = new ProjectileOverrides();
            }

            if (source.damageMultiplier.HasValue) target.damageMultiplier = source.damageMultiplier;
            if (source.speedMultiplier.HasValue) target.speedMultiplier = source.speedMultiplier;
            if (source.stoppingPowerMultiplier.HasValue) target.stoppingPowerMultiplier = source.stoppingPowerMultiplier;
            if (source.beamTrailPreset != null) target.beamTrailPreset = source.beamTrailPreset;
            if (source.damageDef != null) target.damageDef = source.damageDef;
            return target;
        }

        /// <summary>
        /// 判断构型是否确实声明了 VerbProperties 字段。
        /// 视觉、执行、Tool 和模块覆盖不应创建新的 VerbProperties。
        /// </summary>
        private static bool HasVerbPropertiesOverrides(ChipArmamentFormOverrides ov)
        {
            return ov != null
                && (ov.range.HasValue
                    || ov.minRange.HasValue
                    || ov.accuracyTouch.HasValue
                    || ov.accuracyShort.HasValue
                    || ov.accuracyMedium.HasValue
                    || ov.accuracyLong.HasValue
                    || ov.warmupTime.HasValue
                    || ov.defaultCooldownTime.HasValue
                    || ov.burstShotCount.HasValue
                    || ov.burstShotCountMultiplier.HasValue
                    || ov.ticksBetweenBurstShots.HasValue
                    || ov.forcedMissEvenDispersal.HasValue
                    || ov.canGoWild.HasValue
                    || ov.soundCast != null
                    || ov.soundCastTail != null
                    || ov.soundAiming != null
                    || ov.muzzleFlashScale.HasValue
                    || ov.noiseRadius.HasValue
                    || ov.rangedFireRulepack != null
                    || ov.defaultProjectile != null
                    || ov.rangeMultiplier.HasValue
                    || ov.accuracyMultiplier.HasValue
                    || ov.warmupMultiplier.HasValue
                    || ov.cooldownMultiplier.HasValue);
        }

        /// <summary>
        /// 克隆单条 ChipExpressionEntryConfig，生成新的 Id 以避免碰撞。
        /// 克隆后的对象不会与模板原始 Def 共享引用。
        /// </summary>
        public static ChipExpressionEntryConfig CloneEntry(
            ChipExpressionEntryConfig source,
            string idPrefix)
        {
            ChipExpressionEntryConfig clone = new ChipExpressionEntryConfig
            {
                Id = string.IsNullOrWhiteSpace(idPrefix)
                    ? source.Id
                    : idPrefix + "_" + source.Id,
                DisplayLabel = source.DisplayLabel,
                DisplayLabelKey = source.DisplayLabelKey,
                ToolLabelKeys = source.ToolLabelKeys != null
                    ? new List<string>(source.ToolLabelKeys)
                    : null,
                RoleKey = source.RoleKey,
                Kind = source.Kind,
                RelationKind = source.RelationKind,
                ParentEntryId = source.ParentEntryId,
                WeaponMode = source.WeaponMode,
                DirectTargetLineOfSight = source.DirectTargetLineOfSight,
                AbilityDefName = source.AbilityDefName,
                HediffDefName = source.HediffDefName,
                HediffApplyModeKey = source.HediffApplyModeKey,
                PassiveKey = source.PassiveKey,
                SemanticSourceKind = source.SemanticSourceKind
            };

            // VerbProps — 深拷贝，不共享引用
            if (source.VerbProps != null)
            {
                clone.VerbProps = CloneVerbProps(source.VerbProps);
            }

            // Execution — 深拷贝
            if (source.Execution != null)
            {
                clone.Execution = CloneExecution(source.Execution);
            }

            // Presentation — 深拷贝
            if (source.Presentation != null)
            {
                clone.Presentation = ClonePresentation(source.Presentation);
            }

            // Trion — 保留引用（TrionConfig 是数据对象，制造时不修改）
            clone.Trion = source.Trion;

            // RangedModules — 深拷贝列表
            if (source.RangedModules != null)
            {
                clone.RangedModules = CloneModuleList(source.RangedModules);
            }

            // RangedModuleAugmentations — 深拷贝开放式远程增强声明
            if (source.RangedModuleAugmentations != null)
            {
                clone.RangedModuleAugmentations =
                    CloneRangedModuleAugmentations(source.RangedModuleAugmentations);
            }

            // Tool — 值类型拷贝
            clone.Tool = source.Tool;
            if (source.tools != null)
            {
                clone.tools = CloneToolList(source.tools);
            }

            clone.Maneuver = source.Maneuver;
            clone.ExposedData = source.ExposedData;
            clone.Tags = source.Tags != null
                ? new List<string>(source.Tags)
                : null;
            clone.Conditions = source.Conditions != null
                ? new List<ExpressionSourceConditionConfig>(source.Conditions)
                : null;

            // ProjectileOverrides — 深拷贝
            if (source.ProjectileOverrides != null)
            {
                clone.ProjectileOverrides = source.ProjectileOverrides.Clone();
            }

            return clone;
        }

        /// <summary>
        /// 浅拷贝 VerbProperties，利用原版公开的 MemberwiseClone() 方法。
        /// </summary>
        private static VerbProperties CloneVerbProps(VerbProperties source)
        {
            // RimWorld VerbProperties 公开了 MemberwiseClone()，直接使用。
            return source.MemberwiseClone();
        }

        /// <summary>
        /// 深拷贝 ChipAttackExecutionConfig。
        /// </summary>
        private static ChipAttackExecutionConfig CloneExecution(ChipAttackExecutionConfig source)
        {
            ChipAttackExecutionConfig clone = new ChipAttackExecutionConfig
            {
                Rhythm = source.Rhythm,
                HitCount = source.HitCount,
                HitIntervalTicks = source.HitIntervalTicks
            };

            if (source.OriginSpread != null)
            {
                clone.OriginSpread = new ChipAttackOriginSpreadConfig
                {
                    LateralMin = source.OriginSpread.LateralMin,
                    LateralMax = source.OriginSpread.LateralMax,
                    ForwardMin = source.OriginSpread.ForwardMin,
                    ForwardMax = source.OriginSpread.ForwardMax
                };
            }

            return clone;
        }

        /// <summary>
        /// 深拷贝 ExpressionPresentationConfig。
        /// </summary>
        private static ExpressionPresentationConfig ClonePresentation(
            ExpressionPresentationConfig source)
        {
            return new ExpressionPresentationConfig
            {
                ManualEntryIconTexPath = source.ManualEntryIconTexPath,
                VisualPresetDefName = source.VisualPresetDefName,
                VisualGraphicOverrideDefName = source.VisualGraphicOverrideDefName,
                CompositeVisualPresetDefName = source.CompositeVisualPresetDefName,
                ForceSuppressHostEquipment = source.ForceSuppressHostEquipment,
                VisualPriority = source.VisualPriority
            };
        }

        /// <summary>
        /// 深拷贝 RangedModuleMountConfig 列表。
        /// </summary>
        private static List<RangedModuleMountConfig> CloneModuleList(
            List<RangedModuleMountConfig> source)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    result.Add(source[i].Clone());
                }
            }

            return result;
        }

        /// <summary>
        /// 深拷贝开放式远程增强声明列表。
        /// 制造成品芯片必须保留被动芯片发布的增强，不能只复制自身远程模块。
        /// </summary>
        private static List<RangedModuleAugmentationConfig> CloneRangedModuleAugmentations(
            List<RangedModuleAugmentationConfig> source)
        {
            List<RangedModuleAugmentationConfig> result =
                new List<RangedModuleAugmentationConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                RangedModuleAugmentationConfig augmentation = source[index];
                if (augmentation != null)
                {
                    result.Add(augmentation.Clone());
                }
            }

            return result;
        }

        /// <summary>
        /// 浅拷贝 Tool 列表。
        /// Tool 是 Verse 值类型，在制造时复制引用安全。
        /// </summary>
        private static List<Tool> CloneToolList(List<Tool> source)
        {
            List<Tool> result = new List<Tool>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    result.Add(source[i]);
                }
            }

            return result;
        }
    }
}
