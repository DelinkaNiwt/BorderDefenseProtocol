using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Expressions;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>把一个或两个动作的表达配置复制为成品自己的表达配置。</summary>
    public static class ChipExpressionMergeService
    {
        /// <summary>完整复制单动作表达，包括动作自身已有的多形态。</summary>
        public static ChipExpressionConfig CloneSingle(ChipExpressionConfig source)
        {
            if (source == null)
            {
                return new ChipExpressionConfig
                {
                    Entries = new List<ChipExpressionEntryConfig>()
                };
            }

            return new ChipExpressionConfig
            {
                Entries = CloneEntries(source.Entries, null),
                Modes = CloneModes(source.Modes),
                DefaultModeKey = source.DefaultModeKey
            };
        }

        /// <summary>按动作顺序生成两个独立形态，第一动作固定为默认形态。</summary>
        public static ChipExpressionConfig MergeDual(
            ChipActionPresetDef firstAction,
            ChipActionPresetDef secondAction)
        {
            List<ChipExpressionEntryConfig> firstEntries =
                CloneEntries(firstAction.config?.Expression?.Entries, "mfg_0");
            List<ChipExpressionEntryConfig> secondEntries =
                CloneEntries(secondAction.config?.Expression?.Entries, "mfg_1");
            List<ChipExpressionEntryConfig> allEntries =
                new List<ChipExpressionEntryConfig>(firstEntries);
            allEntries.AddRange(secondEntries);

            return new ChipExpressionConfig
            {
                Entries = allEntries,
                Modes = new List<ChipExpressionModeConfig>
                {
                    BuildMode(firstAction, firstEntries, "mfg_0"),
                    BuildMode(secondAction, secondEntries, "mfg_1")
                },
                DefaultModeKey = firstAction.defName
            };
        }

        /// <summary>复制条目并给双动作条目增加稳定前缀。</summary>
        private static List<ChipExpressionEntryConfig> CloneEntries(
            IList<ChipExpressionEntryConfig> source,
            string prefix)
        {
            List<ChipExpressionEntryConfig> result = new List<ChipExpressionEntryConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ChipExpressionEntryConfig sourceEntry = source[index];
                if (sourceEntry == null)
                {
                    continue;
                }

                ChipExpressionEntryConfig clone =
                    ChipArmamentFormExpressionService.CloneEntry(sourceEntry, prefix);
                if (!string.IsNullOrWhiteSpace(prefix)
                    && !string.IsNullOrWhiteSpace(sourceEntry.ParentEntryId))
                {
                    clone.ParentEntryId = prefix + "_" + sourceEntry.ParentEntryId;
                }

                if (sourceEntry.Trion != null)
                {
                    // UseCost（使用消耗）和 MinimumRequired（最低需求）属于各形态，绝不相加。
                    clone.Trion = new ExpressionSourceTrionConfig
                    {
                        UseCost = sourceEntry.Trion.UseCost,
                        MinimumRequired = sourceEntry.Trion.MinimumRequired,
                        SustainCostBySourceCount = CloneSustainCosts(
                            sourceEntry.Trion.SustainCostBySourceCount)
                    };
                }

                result.Add(clone);
            }

            return result;
        }

        /// <summary>复制一项动作成为制造生成的单形态。</summary>
        private static ChipExpressionModeConfig BuildMode(
            ChipActionPresetDef action,
            IList<ChipExpressionEntryConfig> clonedEntries,
            string prefix)
        {
            ChipExpressionModeConfig sourceMode =
                action.config?.Expression?.Modes != null
                && action.config.Expression.Modes.Count == 1
                    ? action.config.Expression.Modes[0]
                    : null;
            List<string> activeIds = new List<string>();
            if (sourceMode?.ActiveEntryIds != null)
            {
                for (int index = 0; index < sourceMode.ActiveEntryIds.Count; index++)
                {
                    activeIds.Add(prefix + "_" + sourceMode.ActiveEntryIds[index]);
                }
            }
            else
            {
                for (int index = 0; index < clonedEntries.Count; index++)
                {
                    activeIds.Add(clonedEntries[index].Id);
                }
            }

            return new ChipExpressionModeConfig
            {
                ModeKey = action.defName,
                DisplayLabel = action.ResolvedLabel,
                DisplayLabelKey = sourceMode?.DisplayLabelKey,
                GizmoIconTexPath = sourceMode?.GizmoIconTexPath,
                ActiveEntryIds = activeIds,
                DefaultStanceKey = sourceMode?.DefaultStanceKey,
                Stances = CloneStancesWithPrefix(sourceMode?.Stances, prefix)
            };
        }

        /// <summary>复制普通形态列表。</summary>
        private static List<ChipExpressionModeConfig> CloneModes(
            IList<ChipExpressionModeConfig> source)
        {
            if (source == null)
            {
                return null;
            }

            List<ChipExpressionModeConfig> result = new List<ChipExpressionModeConfig>();
            for (int index = 0; index < source.Count; index++)
            {
                ChipExpressionModeConfig mode = source[index];
                if (mode == null)
                {
                    continue;
                }

                result.Add(new ChipExpressionModeConfig
                {
                    ModeKey = mode.ModeKey,
                    DisplayLabel = mode.DisplayLabel,
                    DisplayLabelKey = mode.DisplayLabelKey,
                    GizmoIconTexPath = mode.GizmoIconTexPath,
                    ActiveEntryIds = mode.ActiveEntryIds != null
                        ? new List<string>(mode.ActiveEntryIds)
                        : new List<string>(),
                    DefaultStanceKey = mode.DefaultStanceKey,
                    Stances = CloneStances(mode.Stances)
                });
            }

            return result;
        }

        /// <summary>复制形态内的姿态列表，不改变条目标识。</summary>
        private static List<ChipExpressionStanceConfig> CloneStances(
            IList<ChipExpressionStanceConfig> source)
        {
            return CloneStancesWithPrefix(source, null);
        }

        /// <summary>复制形态内姿态，并按需给双动作条目标识增加前缀。</summary>
        private static List<ChipExpressionStanceConfig> CloneStancesWithPrefix(
            IList<ChipExpressionStanceConfig> source,
            string prefix)
        {
            if (source == null)
            {
                return null;
            }

            List<ChipExpressionStanceConfig> result = new List<ChipExpressionStanceConfig>();
            for (int index = 0; index < source.Count; index++)
            {
                ChipExpressionStanceConfig stance = source[index];
                if (stance == null)
                {
                    continue;
                }

                List<string> activeEntryIds = new List<string>();
                if (stance.ActiveEntryIds != null)
                {
                    for (int entryIndex = 0; entryIndex < stance.ActiveEntryIds.Count; entryIndex++)
                    {
                        string entryId = stance.ActiveEntryIds[entryIndex];
                        activeEntryIds.Add(string.IsNullOrWhiteSpace(prefix)
                            ? entryId
                            : prefix + "_" + entryId);
                    }
                }

                result.Add(new ChipExpressionStanceConfig
                {
                    StanceKey = stance.StanceKey,
                    DisplayLabel = stance.DisplayLabel,
                    DisplayLabelKey = stance.DisplayLabelKey,
                    GizmoIconTexPath = stance.GizmoIconTexPath,
                    ActiveEntryIds = activeEntryIds
                });
            }

            return result;
        }

        /// <summary>复制表达持续消耗阶梯。</summary>
        private static List<ExpressionSustainCostBySourceCountConfig> CloneSustainCosts(
            IList<ExpressionSustainCostBySourceCountConfig> source)
        {
            List<ExpressionSustainCostBySourceCountConfig> result =
                new List<ExpressionSustainCostBySourceCountConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ExpressionSustainCostBySourceCountConfig cost = source[index];
                if (cost != null)
                {
                    result.Add(new ExpressionSustainCostBySourceCountConfig
                    {
                        SourceCount = cost.SourceCount,
                        TotalPerSecond = cost.TotalPerSecond
                    });
                }
            }

            return result;
        }
    }
}
