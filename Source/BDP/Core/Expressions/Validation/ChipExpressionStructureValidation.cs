using System;
using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达静态结构的校验结果。
    /// 它只承载形态、标识、引用和父子顺序问题，不解释具体业务条目。
    /// </summary>
    internal sealed class ChipExpressionStructureValidation
    {
        /// <summary>
        /// 阻止整枚芯片表达成立的结构错误。
        /// </summary>
        internal readonly List<string> Errors = new List<string>();

        /// <summary>
        /// 不阻止表达成立但需要诊断的结构提醒。
        /// </summary>
        internal readonly List<string> Warnings = new List<string>();

        /// <summary>
        /// 当前结构是否可以进入正式解释。
        /// </summary>
        internal bool IsValid
        {
            get { return Errors.Count == 0; }
        }
    }

    /// <summary>
    /// 芯片表达目录与形态选择表的统一结构校验器。
    /// 定义读取和运行解释必须共同使用这一份规则，避免口径漂移。
    /// </summary>
    internal static class ChipExpressionStructureValidator
    {
        /// <summary>
        /// 校验一枚芯片的统一表达目录和可选形态。
        /// </summary>
        internal static ChipExpressionStructureValidation Validate(ChipExpressionConfig config)
        {
            ChipExpressionStructureValidation result = new ChipExpressionStructureValidation();
            if (config == null)
            {
                result.Errors.Add("表达配置不存在。");
                return result;
            }

            Dictionary<string, ChipExpressionEntryConfig> entriesById =
                BuildEntryIndex(config.Entries, result);
            bool hasModes = config.Modes != null && config.Modes.Count > 0;
            if (!hasModes)
            {
                ValidateSingleModeShape(config, entriesById, result);
                return result;
            }

            ValidateMultiModeShape(config, entriesById, result);
            return result;
        }

        /// <summary>
        /// 建立统一表达目录索引，并检查条目标识与父引用的共同结构。
        /// </summary>
        private static Dictionary<string, ChipExpressionEntryConfig> BuildEntryIndex(
            List<ChipExpressionEntryConfig> entries,
            ChipExpressionStructureValidation result)
        {
            Dictionary<string, ChipExpressionEntryConfig> entriesById =
                new Dictionary<string, ChipExpressionEntryConfig>(StringComparer.OrdinalIgnoreCase);
            if (entries == null || entries.Count == 0)
            {
                result.Errors.Add("表达目录 Entries 不能为空。");
                return entriesById;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                ChipExpressionEntryConfig entry = entries[index];
                if (entry == null)
                {
                    result.Errors.Add("表达目录 Entries 在位置 " + index + " 存在空条目。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    result.Errors.Add("表达目录 Entries 在位置 " + index + " 缺少 Id。");
                    continue;
                }

                if (entriesById.ContainsKey(entry.Id))
                {
                    result.Errors.Add("表达条目 Id 重复：" + entry.Id + "。");
                    continue;
                }

                entriesById.Add(entry.Id, entry);
            }

            foreach (KeyValuePair<string, ChipExpressionEntryConfig> pair in entriesById)
            {
                ChipExpressionEntryConfig entry = pair.Value;
                if (entry.RelationKind != ChipExpressionRelationKindConfig.Attached)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.ParentEntryId))
                {
                    result.Errors.Add("依附表达条目 " + entry.Id + " 缺少 ParentEntryId。");
                    continue;
                }

                if (!entriesById.ContainsKey(entry.ParentEntryId))
                {
                    result.Errors.Add(
                        "依附表达条目 " + entry.Id + " 指向不存在的父条目 " + entry.ParentEntryId + "。");
                }
            }

            return entriesById;
        }

        /// <summary>
        /// 校验没有形态列表的单形态写法。
        /// </summary>
        private static void ValidateSingleModeShape(
            ChipExpressionConfig config,
            Dictionary<string, ChipExpressionEntryConfig> entriesById,
            ChipExpressionStructureValidation result)
        {
            if (!string.IsNullOrWhiteSpace(config.DefaultModeKey))
            {
                result.Errors.Add("单形态芯片不得填写 DefaultModeKey。");
            }

            if (config.Entries == null)
            {
                return;
            }

            Dictionary<string, int> positions = BuildCatalogPositions(config.Entries);
            foreach (KeyValuePair<string, ChipExpressionEntryConfig> pair in entriesById)
            {
                ValidateParentOrder(pair.Value, positions, "单形态表达目录", result);
            }
        }

        /// <summary>
        /// 校验多形态的默认键、选择表、引用覆盖和父子顺序。
        /// </summary>
        private static void ValidateMultiModeShape(
            ChipExpressionConfig config,
            Dictionary<string, ChipExpressionEntryConfig> entriesById,
            ChipExpressionStructureValidation result)
        {
            if (string.IsNullOrWhiteSpace(config.DefaultModeKey))
            {
                result.Errors.Add("多形态芯片必须填写 DefaultModeKey。");
            }

            Dictionary<string, ChipExpressionModeConfig> modesByKey =
                new Dictionary<string, ChipExpressionModeConfig>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> referencedEntryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int modeIndex = 0; modeIndex < config.Modes.Count; modeIndex++)
            {
                ChipExpressionModeConfig mode = config.Modes[modeIndex];
                if (mode == null)
                {
                    result.Errors.Add("Modes 在位置 " + modeIndex + " 存在空形态。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(mode.ModeKey))
                {
                    result.Errors.Add("Modes 在位置 " + modeIndex + " 缺少 ModeKey。");
                    continue;
                }

                if (modesByKey.ContainsKey(mode.ModeKey))
                {
                    result.Errors.Add("ModeKey 重复：" + mode.ModeKey + "。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(mode.DisplayLabel)
                    && string.IsNullOrWhiteSpace(mode.DisplayLabelKey))
                {
                    result.Errors.Add("形态 " + mode.ModeKey + " 缺少 DisplayLabel 或 DisplayLabelKey。");
                }

                modesByKey.Add(mode.ModeKey, mode);
                ValidateModeEntries(mode, entriesById, referencedEntryIds, result);
            }

            if (!string.IsNullOrWhiteSpace(config.DefaultModeKey)
                && !modesByKey.ContainsKey(config.DefaultModeKey))
            {
                result.Errors.Add("DefaultModeKey 指向不存在的形态：" + config.DefaultModeKey + "。");
            }

            foreach (string entryId in entriesById.Keys)
            {
                if (!referencedEntryIds.Contains(entryId))
                {
                    result.Errors.Add("多形态表达条目没有被任何形态引用：" + entryId + "。");
                }
            }
        }

        /// <summary>
        /// 校验单个形态的公共条目，以及可选姿态的最终条目闭包。
        /// </summary>
        private static void ValidateModeEntries(
            ChipExpressionModeConfig mode,
            Dictionary<string, ChipExpressionEntryConfig> entriesById,
            HashSet<string> referencedEntryIds,
            ChipExpressionStructureValidation result)
        {
            bool hasStances = mode.Stances != null && mode.Stances.Count > 0;
            if ((mode.ActiveEntryIds == null || mode.ActiveEntryIds.Count == 0) && !hasStances)
            {
                result.Errors.Add("形态 " + mode.ModeKey + " 的 ActiveEntryIds 不能为空。");
                return;
            }

            Dictionary<string, int> positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            AppendSelectedEntries(
                mode.ActiveEntryIds,
                positions,
                entriesById,
                referencedEntryIds,
                "形态 " + mode.ModeKey,
                result);

            if (!hasStances)
            {
                if (!string.IsNullOrWhiteSpace(mode.DefaultStanceKey))
                {
                    result.Errors.Add("没有姿态的形态 " + mode.ModeKey + " 不得填写 DefaultStanceKey。");
                }

                ValidateSelectedParentOrder(positions, entriesById, "形态 " + mode.ModeKey, result);
                return;
            }

            ValidateModeStances(mode, positions, entriesById, referencedEntryIds, result);
        }

        /// <summary>
        /// 校验一个形态内部的姿态键、默认姿态和最终条目闭包。
        /// </summary>
        private static void ValidateModeStances(
            ChipExpressionModeConfig mode,
            Dictionary<string, int> commonPositions,
            Dictionary<string, ChipExpressionEntryConfig> entriesById,
            HashSet<string> referencedEntryIds,
            ChipExpressionStructureValidation result)
        {
            if (string.IsNullOrWhiteSpace(mode.DefaultStanceKey))
            {
                result.Errors.Add("含姿态的形态 " + mode.ModeKey + " 必须填写 DefaultStanceKey。");
            }

            Dictionary<string, ChipExpressionStanceConfig> stancesByKey =
                new Dictionary<string, ChipExpressionStanceConfig>(StringComparer.OrdinalIgnoreCase);
            for (int stanceIndex = 0; stanceIndex < mode.Stances.Count; stanceIndex++)
            {
                ChipExpressionStanceConfig stance = mode.Stances[stanceIndex];
                if (stance == null)
                {
                    result.Errors.Add("形态 " + mode.ModeKey + " 的 Stances 在位置 " + stanceIndex + " 存在空姿态。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(stance.StanceKey))
                {
                    result.Errors.Add("形态 " + mode.ModeKey + " 的 Stances 在位置 " + stanceIndex + " 缺少 StanceKey。");
                    continue;
                }

                if (stancesByKey.ContainsKey(stance.StanceKey))
                {
                    result.Errors.Add("形态 " + mode.ModeKey + " 的 StanceKey 重复：" + stance.StanceKey + "。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(stance.DisplayLabel)
                    && string.IsNullOrWhiteSpace(stance.DisplayLabelKey))
                {
                    result.Errors.Add(
                        "形态 " + mode.ModeKey + " 的姿态 " + stance.StanceKey
                        + " 缺少 DisplayLabel 或 DisplayLabelKey。");
                }

                stancesByKey.Add(stance.StanceKey, stance);
                Dictionary<string, int> effectivePositions =
                    new Dictionary<string, int>(commonPositions, StringComparer.OrdinalIgnoreCase);
                if (stance.ActiveEntryIds == null || stance.ActiveEntryIds.Count == 0)
                {
                    result.Errors.Add(
                        "形态 " + mode.ModeKey + " 的姿态 " + stance.StanceKey
                        + " 的 ActiveEntryIds 不能为空。");
                }
                else
                {
                    AppendSelectedEntries(
                        stance.ActiveEntryIds,
                        effectivePositions,
                        entriesById,
                        referencedEntryIds,
                        "形态 " + mode.ModeKey + " 的姿态 " + stance.StanceKey,
                        result);
                }

                ValidateSelectedParentOrder(
                    effectivePositions,
                    entriesById,
                    "形态 " + mode.ModeKey + " 的姿态 " + stance.StanceKey,
                    result);
            }

            if (!string.IsNullOrWhiteSpace(mode.DefaultStanceKey)
                && !stancesByKey.ContainsKey(mode.DefaultStanceKey))
            {
                result.Errors.Add(
                    "形态 " + mode.ModeKey + " 的 DefaultStanceKey 指向不存在的姿态："
                    + mode.DefaultStanceKey + "。");
            }
        }

        /// <summary>
        /// 把一组选中条目追加到最终位置表，并统一检查空值、重复和目录引用。
        /// </summary>
        private static void AppendSelectedEntries(
            List<string> entryIds,
            Dictionary<string, int> positions,
            Dictionary<string, ChipExpressionEntryConfig> entriesById,
            HashSet<string> referencedEntryIds,
            string scope,
            ChipExpressionStructureValidation result)
        {
            if (entryIds == null)
            {
                return;
            }

            for (int entryIndex = 0; entryIndex < entryIds.Count; entryIndex++)
            {
                string entryId = entryIds[entryIndex];
                if (string.IsNullOrWhiteSpace(entryId))
                {
                    result.Errors.Add(scope + " 的 ActiveEntryIds 在位置 " + entryIndex + " 为空。");
                    continue;
                }

                if (positions.ContainsKey(entryId))
                {
                    result.Errors.Add(scope + " 重复引用表达条目 " + entryId + "。");
                    continue;
                }

                positions.Add(entryId, positions.Count);
                if (!entriesById.ContainsKey(entryId))
                {
                    result.Errors.Add(scope + " 引用了不存在的表达条目 " + entryId + "。");
                    continue;
                }

                referencedEntryIds.Add(entryId);
            }
        }

        /// <summary>
        /// 校验一个最终条目位置表里的依附父子顺序。
        /// </summary>
        private static void ValidateSelectedParentOrder(
            Dictionary<string, int> positions,
            Dictionary<string, ChipExpressionEntryConfig> entriesById,
            string scope,
            ChipExpressionStructureValidation result)
        {
            foreach (string entryId in positions.Keys)
            {
                ChipExpressionEntryConfig entry;
                if (entriesById.TryGetValue(entryId, out entry))
                {
                    ValidateParentOrder(entry, positions, scope, result);
                }
            }
        }

        /// <summary>
        /// 按统一表达目录的作者顺序建立位置表。
        /// </summary>
        private static Dictionary<string, int> BuildCatalogPositions(List<ChipExpressionEntryConfig> entries)
        {
            Dictionary<string, int> positions =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < entries.Count; index++)
            {
                ChipExpressionEntryConfig entry = entries[index];
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.Id)
                    || positions.ContainsKey(entry.Id))
                {
                    continue;
                }

                positions.Add(entry.Id, index);
            }

            return positions;
        }

        /// <summary>
        /// 校验依附型子条目与父条目在当前有效顺序中的相对位置。
        /// </summary>
        private static void ValidateParentOrder(
            ChipExpressionEntryConfig entry,
            Dictionary<string, int> positions,
            string scope,
            ChipExpressionStructureValidation result)
        {
            if (entry == null
                || entry.RelationKind != ChipExpressionRelationKindConfig.Attached
                || string.IsNullOrWhiteSpace(entry.ParentEntryId))
            {
                return;
            }

            int childPosition;
            int parentPosition;
            if (!positions.TryGetValue(entry.Id, out childPosition))
            {
                return;
            }

            if (!positions.TryGetValue(entry.ParentEntryId, out parentPosition))
            {
                result.Errors.Add(
                    scope + " 启用了子条目 " + entry.Id
                    + "，但没有同时启用父条目 " + entry.ParentEntryId + "。");
                return;
            }

            if (parentPosition >= childPosition)
            {
                result.Errors.Add(
                    scope + " 中父条目 " + entry.ParentEntryId
                    + " 必须写在子条目 " + entry.Id + " 之前。");
            }
        }
    }
}
