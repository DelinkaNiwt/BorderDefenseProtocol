using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using Verse;

using BDP.Content.Assembly.ChipManufacturing.Defs;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 枪壳表达条目复制与覆盖服务。
    /// 将 ChipGunShellDef 的覆盖层应用到远程表达条目上。
    ///
    /// 规则：
    /// 1. 仅对 Kind=PrimaryVerb|SecondaryVerb 且 WeaponMode=Ranged 的条目生效。
    /// 2. 标量字段：枪壳声明了就使用枪壳值，未声明则保留动作原值。
    /// 3. 列表字段：按显式的 MergeStrategy 决定 Append 或 Replace。
    /// 4. 始终返回新构造的对象，不修改原始数据。
    /// </summary>
    public static class ChipGunShellExpressionService
    {
        /// <summary>
        /// 对条目列表直接执行合并。
        /// 当枪壳为 null 时，只返回条目的深复制。
        /// </summary>
        /// <param name="entries">条目列表，不可为 null。</param>
        /// <param name="gunShell">可选枪壳，null 表示不覆盖。</param>
        /// <param name="idPrefix">为每条生成的条目 Id 添加的前缀。</param>
        /// <returns>合并后的条目列表。</returns>
        public static List<ChipExpressionEntryConfig> MergeEntries(
            List<ChipExpressionEntryConfig> entries,
            ChipGunShellDef gunShell,
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
                if (gunShell != null && ShouldApplyGunShellOverride(source))
                {
                    ApplyGunShellOverrides(merged, gunShell);
                }

                result.Add(merged);
            }

            return result;
        }

        /// <summary>
        /// 判断当前条目是否应接受枪械类别覆盖。
        /// 仅 Verb 通道 + 远程模式才有此资格。
        /// </summary>
        private static bool ShouldApplyGunShellOverride(ChipExpressionEntryConfig entry)
        {
            if (entry.Kind != ChipExpressionEntryKindConfig.PrimaryVerb
                && entry.Kind != ChipExpressionEntryKindConfig.SecondaryVerb)
            {
                return false;
            }

            return entry.WeaponMode == VerbExpressionModeConfig.Ranged;
        }

        /// <summary>
        /// 对单条远程条目执行枪壳覆盖。
        /// 标量字段逐字段 non-null 判断；列表字段按 MergeStrategy 处理。
        /// </summary>
        public static void ApplyGunShellOverrides(
            ChipExpressionEntryConfig entry,
            ChipGunShellDef gunShell)
        {
            ChipGunShellOverrides ov = gunShell.overrides;
            if (ov == null)
            {
                return;
            }

            // --- VerbProps 标量 ---
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

            // --- ProjectileOverrides（投射物属性覆盖） ---
            ApplyProjectileOverrides(entry, gunShell);

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
        /// 对单条条目的投射物属性施加枪壳覆盖。
        /// 对标 VerbProps 的逐字段 nullable 覆盖：声明了的才覆盖，未声明保留原值。
        /// </summary>
        private static void ApplyProjectileOverrides(
            ChipExpressionEntryConfig entry,
            ChipGunShellDef gunShell)
        {
            ProjectileOverrides src = gunShell.projectileOverrides;
            if (src == null) return;

            if (entry.ProjectileOverrides == null)
                entry.ProjectileOverrides = new ProjectileOverrides();

            ProjectileOverrides target = entry.ProjectileOverrides;

            if (src.damageMultiplier.HasValue) target.damageMultiplier = src.damageMultiplier;
            if (src.speedMultiplier.HasValue) target.speedMultiplier = src.speedMultiplier;
            if (src.stoppingPowerMultiplier.HasValue) target.stoppingPowerMultiplier = src.stoppingPowerMultiplier;
            if (src.beamTrailPreset != null) target.beamTrailPreset = src.beamTrailPreset;
            if (src.damageDef != null) target.damageDef = src.damageDef;
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
                clone.ProjectileOverrides = new ProjectileOverrides
                {
                    damageMultiplier = source.ProjectileOverrides.damageMultiplier,
                    speedMultiplier = source.ProjectileOverrides.speedMultiplier,
                    stoppingPowerMultiplier = source.ProjectileOverrides.stoppingPowerMultiplier,
                    beamTrailPreset = source.ProjectileOverrides.beamTrailPreset,
                    damageDef = source.ProjectileOverrides.damageDef
                };
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
