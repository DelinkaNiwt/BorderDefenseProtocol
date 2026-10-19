using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Expressions;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>把一个或两个芯片功能模板的表达配置复制为成品自己的表达配置。</summary>
    public static class ChipExpressionMergeService
    {
        /// <summary>完整复制单芯片功能模板表达，包括其自身已有的多形态。</summary>
        public static ChipExpressionConfig CloneSingle(ChipExpressionConfig source)
        {
            return CloneSingle(source, new ChipFeatureCloneContext(), null);
        }

        /// <summary>按统一复制上下文复制单个表达配置。</summary>
        public static ChipExpressionConfig CloneSingle(
            ChipExpressionConfig source,
            ChipFeatureCloneContext context,
            string prefix)
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
                Entries = CloneEntries(source.Entries, prefix, context),
                Modes = CloneModes(source.Modes, prefix, context),
                DefaultModeKey = source.DefaultModeKey
            };
        }

        /// <summary>按芯片功能模板顺序生成两个独立形态，第一项固定为默认形态。</summary>
        public static ChipExpressionConfig MergeDual(
            ChipManufacturingPresetDef firstFunction,
            ChipManufacturingPresetDef secondFunction)
        {
            return MergeDual(
                firstFunction,
                secondFunction,
                new ChipFeatureCloneContext());
        }

        /// <summary>使用同一复制上下文合并双预设的表达和功能引用。</summary>
        public static ChipExpressionConfig MergeDual(
            ChipManufacturingPresetDef firstFunction,
            ChipManufacturingPresetDef secondFunction,
            ChipFeatureCloneContext context)
        {
            string firstPrefix = SourcePrefix(0);
            string secondPrefix = SourcePrefix(1);
            List<ChipExpressionEntryConfig> firstEntries =
                CloneEntries(firstFunction.config?.Expression?.Entries, firstPrefix, context);
            List<ChipExpressionEntryConfig> secondEntries =
                CloneEntries(secondFunction.config?.Expression?.Entries, secondPrefix, context);
            List<ChipExpressionEntryConfig> allEntries =
                new List<ChipExpressionEntryConfig>(firstEntries);
            allEntries.AddRange(secondEntries);

            return new ChipExpressionConfig
            {
                Entries = allEntries,
                Modes = new List<ChipExpressionModeConfig>
                {
                    BuildMode(firstFunction, firstEntries, firstPrefix),
                    BuildMode(secondFunction, secondEntries, secondPrefix)
                },
                DefaultModeKey = firstFunction.defName
            };
        }

        /// <summary>按同一复制规则把成品条目还原为原始功能模板与条目，不读取人物当前选择。</summary>
        internal static bool TryResolveSourceEntry(IReadOnlyList<ChipManufacturingPresetDef> functions,
            string generatedEntryId, out ChipManufacturingPresetDef function, out ChipExpressionEntryConfig entry)
        {
            function = null;
            entry = null;
            if (functions == null || string.IsNullOrWhiteSpace(generatedEntryId)) return false;
            for (int index = 0; index < functions.Count; index++)
            {
                IList<ChipExpressionEntryConfig> entries = functions[index]?.config?.Expression?.Entries;
                if (entries == null) continue;
                string prefix = functions.Count > 1 ? SourcePrefix(index) : null;
                foreach (ChipExpressionEntryConfig candidate in entries)
                {
                    if (candidate == null || string.IsNullOrWhiteSpace(candidate.Id)
                        || Prefix(prefix, candidate.Id) != generatedEntryId) continue;
                    function = functions[index];
                    entry = candidate;
                    return true;
                }
            }
            return false;
        }

        /// <summary>制造形态顺序的统一条目前缀，生成与来源还原共用。</summary>
        private static string SourcePrefix(int index) { return "mfg_" + index; }

        /// <summary>复制条目并给双芯片功能模板条目增加稳定前缀。</summary>
        private static List<ChipExpressionEntryConfig> CloneEntries(
            IList<ChipExpressionEntryConfig> source,
            string prefix,
            ChipFeatureCloneContext context)
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
                context?.RegisterEntry(
                    Prefix(prefix, sourceEntry.Id),
                    clone.Id);
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

        /// <summary>给双预设表达条目 Id 加稳定前缀。</summary>
        private static string Prefix(string prefix, string value)
        {
            return string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(value)
                ? value
                : prefix + "_" + value;
        }

        /// <summary>复制一项芯片功能模板成为制造生成的单形态。</summary>
        private static ChipExpressionModeConfig BuildMode(
            ChipManufacturingPresetDef function,
            IList<ChipExpressionEntryConfig> clonedEntries,
            string prefix)
        {
            ChipExpressionModeConfig sourceMode =
                function.config?.Expression?.Modes != null
                && function.config.Expression.Modes.Count == 1
                    ? function.config.Expression.Modes[0]
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
                ModeKey = function.defName,
                DisplayLabel = function.ResolvedLabel,
                DisplayLabelKey = sourceMode?.DisplayLabelKey,
                GizmoIconTexPath = sourceMode?.GizmoIconTexPath,
                ActiveEntryIds = activeIds,
                DefaultStanceKey = sourceMode?.DefaultStanceKey,
                Stances = CloneStancesWithPrefix(sourceMode?.Stances, prefix)
            };
        }

        /// <summary>复制普通形态列表。</summary>
        private static List<ChipExpressionModeConfig> CloneModes(
            IList<ChipExpressionModeConfig> source,
            string prefix,
            ChipFeatureCloneContext context)
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

                List<ChipExpressionStanceConfig> clonedStances =
                    CloneStances(mode.Stances);
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    clonedStances = CloneStancesWithPrefix(mode.Stances, prefix);
                }

                result.Add(new ChipExpressionModeConfig
                {
                    ModeKey = mode.ModeKey,
                    DisplayLabel = mode.DisplayLabel,
                    DisplayLabelKey = mode.DisplayLabelKey,
                    GizmoIconTexPath = mode.GizmoIconTexPath,
                    ActiveEntryIds = mode.ActiveEntryIds != null
                        ? CloneEntryIds(mode.ActiveEntryIds, prefix, context)
                        : new List<string>(),
                    DefaultStanceKey = mode.DefaultStanceKey,
                    Stances = clonedStances
                });
            }

            return result;
        }

        /// <summary>复制形态激活条目并读取统一复制上下文映射。</summary>
        private static List<string> CloneEntryIds(
            IList<string> source,
            string prefix,
            ChipFeatureCloneContext context)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                string entryId = string.IsNullOrWhiteSpace(prefix)
                    ? source[index]
                    : prefix + "_" + source[index];
                result.Add(context == null ? entryId : context.ResolveEntry(entryId));
            }

            return result;
        }

        /// <summary>复制形态内的姿态列表，不改变条目标识。</summary>
        private static List<ChipExpressionStanceConfig> CloneStances(
            IList<ChipExpressionStanceConfig> source)
        {
            return CloneStancesWithPrefix(source, null);
        }

        /// <summary>复制形态内姿态，并按需给双芯片功能模板条目标识增加前缀。</summary>
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
