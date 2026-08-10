using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>只从统一解析结果构建中栏预览，不读取 UI 草稿 Def。</summary>
    public static class ChipManufacturingPreviewBuilder
    {
        /// <summary>把当前解析结果转换为稳定分组的玩家预览。</summary>
        public static ChipManufacturingPreviewModel Build(
            ChipCombinationResolution resolution)
        {
            ChipManufacturingPreviewModel model = new ChipManufacturingPreviewModel();
            if (resolution == null
                || resolution.Status != ChipCombinationResolutionStatus.Valid
                || resolution.ResolvedConfig == null)
            {
                model.StatusText = "BDP_ChipManufacturing_PreviewIncomplete".Translate();
                return model;
            }

            ChipDefinitionConfig config = resolution.ResolvedConfig;
            model.ProductLabel = resolution.ResolvedLabel;
            AddSpecifications(model, config);
            AddGunShellAdjustments(model, resolution.GunShell);
            AddGunShellMetrics(model, resolution);
            AddActionForms(model, resolution);
            return model;
        }

        /// <summary>加入槽位、延迟、Trion 成本和使用要求。</summary>
        private static void AddSpecifications(
            ChipManufacturingPreviewModel model,
            ChipDefinitionConfig config)
        {
            ChipLoadoutConfig loadout = config.Loadout;
            model.Specifications.Add(TextMetric(
                "BDP_ChipManufacturing_Spec_SlotRegion",
                TranslateEnum("SlotRegion", loadout?.SlotRegion.ToString())));
            model.Specifications.Add(TextMetric(
                "BDP_ChipManufacturing_Spec_SlotOccupancy",
                TranslateEnum("SlotOccupancy", loadout?.SlotOccupancy.ToString())));
            model.Specifications.Add(TextMetric(
                "BDP_ChipManufacturing_Spec_ActivationDelay",
                FormatTicks(loadout?.ActivationDelayTicks ?? 0)));
            model.Specifications.Add(TextMetric(
                "BDP_ChipManufacturing_Spec_DeactivationDelay",
                FormatTicks(loadout?.DeactivationDelayTicks ?? 0)));
            model.Specifications.Add(TextMetric(
                "BDP_ChipManufacturing_Spec_CapacityCost",
                FormatNumber(config.Trion?.CapacityCost ?? 0f)));
            model.Specifications.Add(TextMetric(
                "BDP_ChipManufacturing_Spec_ActivationCost",
                FormatNumber(config.Trion?.ActivationCost ?? 0f)));
            model.Specifications.Add(TextMetric(
                "BDP_ChipManufacturing_Spec_Requirements",
                FormatRequirements(config.ActivationRequirements)));
        }

        /// <summary>加入一次枪壳绝对覆盖和倍率修正。</summary>
        private static void AddGunShellAdjustments(
            ChipManufacturingPreviewModel model,
            ChipGunShellDef gunShell)
        {
            ChipGunShellOverrides ov = gunShell?.overrides;
            if (ov == null && gunShell?.projectileOverrides == null)
            {
                return;
            }

            AddMultiplier(
                model,
                "ProjectileSpeed",
                gunShell?.projectileOverrides?.speedMultiplier);
        }

        /// <summary>把已选枪壳最终统一的射击属性抽成单独指标组。</summary>
        private static void AddGunShellMetrics(
            ChipManufacturingPreviewModel model,
            ChipCombinationResolution resolution)
        {
            ChipGunShellDef gunShell = resolution?.GunShell;
            if (gunShell == null
                || resolution.Actions == null
                || resolution.Actions.Count == 0)
            {
                return;
            }

            ChipActionPresetDef representativeAction = resolution.Actions[0];
            ChipExpressionEntryConfig entry = FindRepresentativeEntry(
                resolution.ResolvedConfig?.Expression,
                resolution.Actions.Count == 2
                    ? representativeAction?.defName
                    : null);
            if (entry?.VerbProps == null)
            {
                return;
            }

            VerbProperties props = entry.VerbProps;
            model.GunShellMetrics.Add(BarMetric(
                "Range",
                props.range,
                ChipMetricBarScale.RangeMaximum,
                true));
            model.GunShellMetrics.Add(AccuracyMetric(
                "AccuracyTouch",
                props.accuracyTouch,
                true));
            model.GunShellMetrics.Add(AccuracyMetric(
                "AccuracyShort",
                props.accuracyShort,
                true));
            model.GunShellMetrics.Add(AccuracyMetric(
                "AccuracyMedium",
                props.accuracyMedium,
                true));
            model.GunShellMetrics.Add(AccuracyMetric(
                "AccuracyLong",
                props.accuracyLong,
                true));
            model.GunShellMetrics.Add(BarTextMetric(
                "Warmup",
                props.warmupTime,
                ChipMetricBarScale.WarmupMaximum,
                FormatSeconds(props.warmupTime),
                true));
            model.GunShellMetrics.Add(BarTextMetric(
                "Cooldown",
                props.defaultCooldownTime,
                ChipMetricBarScale.CooldownMaximum,
                FormatSeconds(props.defaultCooldownTime),
                true));
            model.GunShellMetrics.Add(BarTextMetric(
                "BurstShotCount",
                props.burstShotCount,
                ChipMetricBarScale.BurstShotCountMaximum,
                FormatNumber(props.burstShotCount),
                true));
        }

        /// <summary>按一个或两个实际动作建立上下形态块。</summary>
        private static void AddActionForms(
            ChipManufacturingPreviewModel model,
            ChipCombinationResolution resolution)
        {
            IReadOnlyList<ChipActionPresetDef> actions = resolution.Actions;
            if (actions == null)
            {
                return;
            }

            for (int index = 0; index < actions.Count; index++)
            {
                ChipActionPresetDef action = actions[index];
                ChipActionFormPreview form = new ChipActionFormPreview
                {
                    Label = action.label
                };
                ChipExpressionEntryConfig entry = FindRepresentativeEntry(
                    resolution.ResolvedConfig.Expression,
                    actions.Count == 2 ? action.defName : null);
                AddActionMetrics(form, entry, resolution.GunShell);
                model.ActionForms.Add(form);
            }
        }

        /// <summary>按形态活动条目查找第一项带 VerbProps 的代表动作。</summary>
        private static ChipExpressionEntryConfig FindRepresentativeEntry(
            ChipExpressionConfig expression,
            string modeKey)
        {
            if (expression?.Entries == null)
            {
                return null;
            }

            HashSet<string> activeIds = null;
            if (!modeKey.NullOrEmpty() && expression.Modes != null)
            {
                ChipExpressionModeConfig mode = expression.Modes.FirstOrDefault(
                    candidate => candidate?.ModeKey == modeKey);
                if (mode?.ActiveEntryIds != null)
                {
                    activeIds = new HashSet<string>(mode.ActiveEntryIds);
                }
            }

            for (int index = 0; index < expression.Entries.Count; index++)
            {
                ChipExpressionEntryConfig entry = expression.Entries[index];
                if (entry?.VerbProps != null
                    && (activeIds == null || activeIds.Contains(entry.Id)))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>加入射程、四档精度、时间和投射物字段。</summary>
        private static void AddActionMetrics(
            ChipActionFormPreview form,
            ChipExpressionEntryConfig entry,
            ChipGunShellDef gunShell)
        {
            if (entry?.VerbProps == null)
            {
                return;
            }

            bool showGunShellCommonMetrics = gunShell == null;
            if (showGunShellCommonMetrics)
            {
                form.Metrics.Add(BarMetric(
                    "Range",
                    entry.VerbProps.range,
                    ChipMetricBarScale.RangeMaximum,
                    false));
                form.Metrics.Add(AccuracyMetric(
                    "AccuracyTouch",
                    entry.VerbProps.accuracyTouch,
                    false));
                form.Metrics.Add(AccuracyMetric(
                    "AccuracyShort",
                    entry.VerbProps.accuracyShort,
                    false));
                form.Metrics.Add(AccuracyMetric(
                    "AccuracyMedium",
                    entry.VerbProps.accuracyMedium,
                    false));
                form.Metrics.Add(AccuracyMetric(
                    "AccuracyLong",
                    entry.VerbProps.accuracyLong,
                    false));
                form.Metrics.Add(TextMetric(
                    Key("Warmup"),
                    FormatSeconds(entry.VerbProps.warmupTime)));
                form.Metrics.Add(TextMetric(
                    Key("Cooldown"),
                    FormatSeconds(entry.VerbProps.defaultCooldownTime)));
            }

            float damage = entry.VerbProps.defaultProjectile?.projectile != null
                ? entry.VerbProps.defaultProjectile.projectile.GetDamageAmount(
                    (Verse.Thing)null,
                    null)
                : 0f;
            float speed = entry.VerbProps.defaultProjectile?.projectile?.speed ?? 0f;
            if (entry.ProjectileOverrides?.damageMultiplier != null)
            {
                damage *= entry.ProjectileOverrides.damageMultiplier.Value;
            }
            if (entry.ProjectileOverrides?.speedMultiplier != null)
            {
                speed *= entry.ProjectileOverrides.speedMultiplier.Value;
            }

            form.Metrics.Add(BarMetric("ProjectileDamage", damage,
                ChipMetricBarScale.DamageMaximum,
                gunShell?.projectileOverrides?.damageMultiplier != null
                    || gunShell?.projectileOverrides?.damageDef != null));
            form.Metrics.Add(BarMetric("ProjectileSpeed", speed,
                ChipMetricBarScale.SpeedMaximum,
                gunShell?.projectileOverrides?.speedMultiplier != null));
        }

        /// <summary>建立普通文本字段。</summary>
        private static ChipMetricPreview TextMetric(
            string labelKey,
            string valueText,
            bool modified = false)
        {
            return new ChipMetricPreview
            {
                LabelKey = labelKey,
                ValueText = valueText,
                ShowBar = false,
                IsModified = modified
            };
        }

        /// <summary>建立普通正向条形图字段。</summary>
        private static ChipMetricPreview BarMetric(
            string name,
            float value,
            float maximum,
            bool modified)
        {
            return new ChipMetricPreview
            {
                LabelKey = Key(name),
                ValueText = FormatNumber(value),
                NormalizedValue = ChipMetricBarScale.Normalize(value, maximum),
                ShowBar = true,
                IsModified = modified
            };
        }

        /// <summary>建立带条形图但使用自定义单位文本的字段。</summary>
        private static ChipMetricPreview BarTextMetric(
            string name,
            float value,
            float maximum,
            string valueText,
            bool modified)
        {
            return new ChipMetricPreview
            {
                LabelKey = Key(name),
                ValueText = valueText,
                NormalizedValue = ChipMetricBarScale.Normalize(value, maximum),
                ShowBar = true,
                IsModified = modified
            };
        }

        /// <summary>建立百分比精度字段。</summary>
        private static ChipMetricPreview AccuracyMetric(
            string name,
            float value,
            bool modified)
        {
            return new ChipMetricPreview
            {
                LabelKey = Key(name),
                ValueText = value.ToStringPercent(),
                NormalizedValue = ChipMetricBarScale.Normalize(
                    value,
                    ChipMetricBarScale.AccuracyMaximum),
                ShowBar = true,
                IsModified = modified
            };
        }

        /// <summary>加入一项绝对覆盖。</summary>
        private static void AddAbsolute(
            ChipManufacturingPreviewModel model,
            string name,
            float? value)
        {
            if (value.HasValue)
            {
                model.GunShellAdjustments.Add(new ChipAdjustmentPreview
                {
                    LabelKey = Key(name),
                    OperationText = "→ " + FormatNumber(value.Value)
                });
            }
        }

        /// <summary>加入一项倍率修正。</summary>
        private static void AddMultiplier(
            ChipManufacturingPreviewModel model,
            string name,
            float? value)
        {
            if (value.HasValue)
            {
                model.GunShellAdjustments.Add(new ChipAdjustmentPreview
                {
                    LabelKey = Key(name),
                    OperationText = "× " + FormatNumber(value.Value)
                });
            }
        }

        /// <summary>拼出动作字段语言键。</summary>
        private static string Key(string name)
        {
            return "BDP_ChipManufacturing_Metric_" + name;
        }

        /// <summary>把枚举值转换为语言包文本。</summary>
        private static string TranslateEnum(string group, string value)
        {
            return ("BDP_ChipManufacturing_" + group + "_" + value).Translate();
        }

        /// <summary>把游戏刻转换为秒。</summary>
        private static string FormatTicks(int ticks)
        {
            return FormatSeconds(ticks / 60f);
        }

        /// <summary>格式化秒数。</summary>
        private static string FormatSeconds(float seconds)
        {
            return "BDP_ChipManufacturing_Seconds".Translate(FormatNumber(seconds));
        }

        /// <summary>用紧凑格式显示精确数值。</summary>
        private static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }

        /// <summary>把全部静态使用要求排成可换行文本。</summary>
        private static string FormatRequirements(IList<PawnRequirement> requirements)
        {
            if (requirements == null || requirements.Count == 0)
            {
                return "BDP_ChipManufacturing_None".Translate();
            }

            List<string> parts = new List<string>();
            for (int index = 0; index < requirements.Count; index++)
            {
                PawnRequirementSnapshot snapshot = requirements[index]?.Describe();
                if (snapshot != null)
                {
                    parts.Add(snapshot.Label + " " + snapshot.RequiredValueText);
                }
            }

            return string.Join("；\n", parts);
        }
    }
}
