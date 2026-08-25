using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 芯片形态运行的中性底层服务。
    /// 它只解析合法形态、维护根槽位当前形态并提供原子切换，不承担按钮、费用或启停时序。
    /// </summary>
    internal static class TriggerChipModeService
    {
        /// <summary>
        /// 为刚完成正式启用的根槽建立默认形态。
        /// 单形态芯片合法但不保存额外形态真值。
        /// </summary>
        internal static bool TryInitializeActiveRootMode(TriggerSlotState rootSlot, Thing chip)
        {
            if (!IsWritableActiveRoot(rootSlot, chip))
            {
                return false;
            }

            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            if (!HasMultipleModes(config))
            {
                rootSlot.SetCurrentModeKey(null);
                rootSlot.SetCurrentStanceKey(null);
                return config != null;
            }

            ChipExpressionModeConfig defaultMode = FindMode(config, config.DefaultModeKey);
            if (defaultMode == null)
            {
                rootSlot.SetCurrentModeKey(null);
                rootSlot.SetCurrentStanceKey(null);
                return false;
            }

            rootSlot.SetCurrentModeKey(defaultMode.ModeKey);
            rootSlot.SetCurrentStanceKey(ResolveDefaultStanceKey(defaultMode));
            return true;
        }

        /// <summary>
        /// 原子切换到指定形态。
        /// 发布失败或抛出异常时恢复旧形态，并把异常交给调用方记录。
        /// </summary>
        internal static bool TrySwitchActiveRootMode(
            TriggerSlotState rootSlot,
            Thing chip,
            string targetModeKey,
            Func<bool> publish,
            Action<Exception> reportException = null)
        {
            if (!IsWritableActiveRoot(rootSlot, chip)
                || string.IsNullOrWhiteSpace(targetModeKey)
                || publish == null)
            {
                return false;
            }

            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            if (!HasMultipleModes(config))
            {
                return false;
            }

            ChipExpressionModeConfig targetMode = FindMode(config, targetModeKey);
            if (targetMode == null)
            {
                return false;
            }

            string previousModeKey = rootSlot.CurrentModeKey;
            string previousStanceKey = rootSlot.CurrentStanceKey;
            if (string.Equals(previousModeKey, targetMode.ModeKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            rootSlot.SetCurrentModeKey(targetMode.ModeKey);
            rootSlot.SetCurrentStanceKey(ResolveDefaultStanceKey(targetMode));
            try
            {
                if (publish())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                reportException?.Invoke(ex);
            }

            rootSlot.SetCurrentModeKey(previousModeKey);
            rootSlot.SetCurrentStanceKey(previousStanceKey);
            return false;
        }

        /// <summary>
        /// 原子切换当前形态内部的指定姿态。
        /// 发布失败或抛出异常时恢复旧姿态。
        /// </summary>
        internal static bool TrySwitchActiveRootStance(
            TriggerSlotState rootSlot,
            Thing chip,
            string targetStanceKey,
            Func<bool> publish,
            Action<Exception> reportException = null)
        {
            if (!IsWritableActiveRoot(rootSlot, chip)
                || string.IsNullOrWhiteSpace(targetStanceKey)
                || publish == null)
            {
                return false;
            }

            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            ChipExpressionModeConfig mode = FindMode(config, rootSlot.CurrentModeKey);
            ChipExpressionStanceConfig targetStance = FindStance(mode, targetStanceKey);
            if (targetStance == null)
            {
                return false;
            }

            string previousStanceKey = rootSlot.CurrentStanceKey;
            if (string.Equals(previousStanceKey, targetStance.StanceKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            rootSlot.SetCurrentStanceKey(targetStance.StanceKey);
            try
            {
                if (publish())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                reportException?.Invoke(ex);
            }

            rootSlot.SetCurrentStanceKey(previousStanceKey);
            return false;
        }

        /// <summary>
        /// 按 XML（定义文件）书写顺序切到当前形态的下一姿态，末项回绕首项。
        /// </summary>
        internal static bool TryCycleActiveRootStance(
            TriggerSlotState rootSlot,
            Thing chip,
            Func<bool> publish,
            Action<Exception> reportException = null)
        {
            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            ChipExpressionModeConfig mode = FindMode(config, rootSlot != null ? rootSlot.CurrentModeKey : null);
            if (!IsWritableActiveRoot(rootSlot, chip) || !HasMultipleStances(mode))
            {
                return false;
            }

            int currentIndex = FindStanceIndex(mode.Stances, rootSlot.CurrentStanceKey);
            int nextIndex = currentIndex >= 0
                ? (currentIndex + 1) % mode.Stances.Count
                : FindStanceIndex(mode.Stances, mode.DefaultStanceKey);
            if (nextIndex < 0)
            {
                return false;
            }

            return TrySwitchActiveRootStance(
                rootSlot,
                chip,
                mode.Stances[nextIndex].StanceKey,
                publish,
                reportException);
        }

        /// <summary>
        /// 按 XML（定义文件）书写顺序切到下一形态，末项回绕首项。
        /// </summary>
        internal static bool TryCycleActiveRootMode(
            TriggerSlotState rootSlot,
            Thing chip,
            Func<bool> publish,
            Action<Exception> reportException = null)
        {
            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            if (!IsWritableActiveRoot(rootSlot, chip) || !HasMultipleModes(config))
            {
                return false;
            }

            int currentIndex = FindModeIndex(config.Modes, rootSlot.CurrentModeKey);
            int nextIndex = currentIndex >= 0
                ? (currentIndex + 1) % config.Modes.Count
                : FindModeIndex(config.Modes, config.DefaultModeKey);
            if (nextIndex < 0)
            {
                return false;
            }

            return TrySwitchActiveRootMode(
                rootSlot,
                chip,
                config.Modes[nextIndex].ModeKey,
                publish,
                reportException);
        }

        /// <summary>
        /// 读档后把已启用根槽的形态恢复成当前定义允许的真值。
        /// 返回 true 表示保存值被清理或回退，供 owner 决定是否记录诊断。
        /// </summary>
        internal static bool NormalizeRestoredActiveRootMode(
            TriggerSlotState rootSlot,
            Thing chip,
            out string discardedModeKey)
        {
            discardedModeKey = rootSlot != null ? rootSlot.CurrentModeKey : null;
            if (!IsWritableActiveRoot(rootSlot, chip))
            {
                if (rootSlot != null)
                {
                    rootSlot.SetCurrentModeKey(null);
                    rootSlot.SetCurrentStanceKey(null);
                }

                return !string.IsNullOrWhiteSpace(discardedModeKey);
            }

            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            if (!HasMultipleModes(config))
            {
                rootSlot.SetCurrentModeKey(null);
                rootSlot.SetCurrentStanceKey(null);
                return !string.IsNullOrWhiteSpace(discardedModeKey);
            }

            ChipExpressionModeConfig savedMode = FindMode(config, rootSlot.CurrentModeKey);
            if (savedMode != null)
            {
                rootSlot.SetCurrentModeKey(savedMode.ModeKey);
                discardedModeKey = null;
                return false;
            }

            ChipExpressionModeConfig defaultMode = FindMode(config, config.DefaultModeKey);
            rootSlot.SetCurrentModeKey(defaultMode != null ? defaultMode.ModeKey : null);
            rootSlot.SetCurrentStanceKey(defaultMode != null ? ResolveDefaultStanceKey(defaultMode) : null);
            return true;
        }

        /// <summary>
        /// 读档后把当前形态内部的姿态恢复成定义允许的真值。
        /// 有效姿态保留，无效或空值回退到当前形态默认姿态。
        /// </summary>
        internal static bool NormalizeRestoredActiveRootStance(
            TriggerSlotState rootSlot,
            Thing chip,
            out string discardedStanceKey)
        {
            discardedStanceKey = rootSlot != null ? rootSlot.CurrentStanceKey : null;
            if (!IsWritableActiveRoot(rootSlot, chip))
            {
                if (rootSlot != null)
                {
                    rootSlot.SetCurrentStanceKey(null);
                }

                return !string.IsNullOrWhiteSpace(discardedStanceKey);
            }

            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            ChipExpressionModeConfig mode = FindMode(config, rootSlot.CurrentModeKey);
            if (mode == null || mode.Stances == null || mode.Stances.Count == 0)
            {
                rootSlot.SetCurrentStanceKey(null);
                return !string.IsNullOrWhiteSpace(discardedStanceKey);
            }

            ChipExpressionStanceConfig savedStance = FindStance(mode, rootSlot.CurrentStanceKey);
            if (savedStance != null)
            {
                rootSlot.SetCurrentStanceKey(savedStance.StanceKey);
                discardedStanceKey = null;
                return false;
            }

            rootSlot.SetCurrentStanceKey(ResolveDefaultStanceKey(mode));
            return true;
        }

        /// <summary>
        /// 为上层建立保持 XML 顺序的只读形态选项副本。
        /// </summary>
        internal static IReadOnlyList<ChipModeOptionSnapshot> BuildOptions(Thing chip)
        {
            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            if (!HasMultipleModes(config))
            {
                return Array.Empty<ChipModeOptionSnapshot>();
            }

            List<ChipModeOptionSnapshot> result = new List<ChipModeOptionSnapshot>(config.Modes.Count);
            for (int index = 0; index < config.Modes.Count; index++)
            {
                ChipExpressionModeConfig mode = config.Modes[index];
                result.Add(new ChipModeOptionSnapshot
                {
                    ModeKey = mode.ModeKey,
                    DisplayLabel = ResolveDisplayLabel(mode.DisplayLabel, mode.DisplayLabelKey),
                    GizmoIconTexPath = string.IsNullOrWhiteSpace(mode.GizmoIconTexPath)
                        ? null
                        : mode.GizmoIconTexPath
                });
            }

            return result;
        }

        /// <summary>
        /// 为上层建立当前形态内保持 XML 顺序的只读姿态选项副本。
        /// </summary>
        internal static IReadOnlyList<ChipStanceOptionSnapshot> BuildStanceOptions(
            Thing chip,
            string modeKey)
        {
            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            ChipExpressionModeConfig mode = FindMode(config, modeKey);
            if (!HasMultipleStances(mode))
            {
                return Array.Empty<ChipStanceOptionSnapshot>();
            }

            List<ChipStanceOptionSnapshot> result =
                new List<ChipStanceOptionSnapshot>(mode.Stances.Count);
            for (int index = 0; index < mode.Stances.Count; index++)
            {
                ChipExpressionStanceConfig stance = mode.Stances[index];
                result.Add(new ChipStanceOptionSnapshot
                {
                    StanceKey = stance.StanceKey,
                    DisplayLabel = ResolveDisplayLabel(stance.DisplayLabel, stance.DisplayLabelKey),
                    GizmoIconTexPath = string.IsNullOrWhiteSpace(stance.GizmoIconTexPath)
                        ? null
                        : stance.GizmoIconTexPath
                });
            }

            return result;
        }

        /// <summary>
        /// 判断指定形态键是否属于当前芯片的合法多形态定义。
        /// </summary>
        internal static bool IsModeKeyValid(Thing chip, string modeKey)
        {
            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            return HasMultipleModes(config) && FindMode(config, modeKey) != null;
        }

        /// <summary>
        /// 判断指定姿态键是否属于当前芯片的指定合法形态。
        /// </summary>
        internal static bool IsStanceKeyValid(Thing chip, string modeKey, string stanceKey)
        {
            ChipExpressionConfig config = ResolveValidExpressionConfig(chip);
            return FindStance(FindMode(config, modeKey), stanceKey) != null;
        }

        /// <summary>
        /// 从芯片读取结果中取得通过统一校验的表达配置。
        /// </summary>
        private static ChipExpressionConfig ResolveValidExpressionConfig(Thing chip)
        {
            ChipDefinitionReadResult readResult = ChipSurfaceAccess.Read(chip);
            return readResult != null
                && readResult.Validation != null
                && readResult.Validation.IsValid
                && readResult.Contract != null
                && readResult.Contract.Expression != null
                && readResult.Contract.Expression.HasExpressionBlock
                ? readResult.Contract.Expression.Config
                : null;
        }

        /// <summary>
        /// 判断配置是否真的声明了多个可切换形态。
        /// </summary>
        private static bool HasMultipleModes(ChipExpressionConfig config)
        {
            return config != null && config.Modes != null && config.Modes.Count > 1;
        }

        /// <summary>
        /// 判断形态是否真的声明了多个可切换姿态。
        /// </summary>
        private static bool HasMultipleStances(ChipExpressionModeConfig mode)
        {
            return mode != null && mode.Stances != null && mode.Stances.Count > 1;
        }

        /// <summary>
        /// 解析形态默认姿态；无姿态形态返回空。
        /// </summary>
        private static string ResolveDefaultStanceKey(ChipExpressionModeConfig mode)
        {
            ChipExpressionStanceConfig stance = FindStance(mode, mode != null ? mode.DefaultStanceKey : null);
            return stance != null ? stance.StanceKey : null;
        }

        /// <summary>
        /// 优先翻译语言包键，否则使用定义中的直接显示名。
        /// </summary>
        private static string ResolveDisplayLabel(string displayLabel, string displayLabelKey)
        {
            return !string.IsNullOrWhiteSpace(displayLabelKey)
                ? displayLabelKey.Translate().ToString()
                : displayLabel;
        }

        /// <summary>
        /// 按不区分大小写的稳定键查找形态。
        /// </summary>
        private static ChipExpressionModeConfig FindMode(
            ChipExpressionConfig config,
            string modeKey)
        {
            return config != null ? FindMode(config.Modes, modeKey) : null;
        }

        /// <summary>
        /// 按不区分大小写的稳定键查找形态。
        /// </summary>
        private static ChipExpressionModeConfig FindMode(
            List<ChipExpressionModeConfig> modes,
            string modeKey)
        {
            int index = FindModeIndex(modes, modeKey);
            return index >= 0 ? modes[index] : null;
        }

        /// <summary>
        /// 解析指定形态在作者顺序中的位置。
        /// </summary>
        private static int FindModeIndex(
            List<ChipExpressionModeConfig> modes,
            string modeKey)
        {
            if (modes == null || string.IsNullOrWhiteSpace(modeKey))
            {
                return -1;
            }

            for (int index = 0; index < modes.Count; index++)
            {
                ChipExpressionModeConfig mode = modes[index];
                if (mode != null
                    && string.Equals(mode.ModeKey, modeKey, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 按不区分大小写的稳定键查找当前形态内的姿态。
        /// </summary>
        private static ChipExpressionStanceConfig FindStance(
            ChipExpressionModeConfig mode,
            string stanceKey)
        {
            int index = FindStanceIndex(mode != null ? mode.Stances : null, stanceKey);
            return index >= 0 ? mode.Stances[index] : null;
        }

        /// <summary>
        /// 解析姿态在作者顺序中的位置。
        /// </summary>
        private static int FindStanceIndex(
            List<ChipExpressionStanceConfig> stances,
            string stanceKey)
        {
            if (stances == null || string.IsNullOrWhiteSpace(stanceKey))
            {
                return -1;
            }

            for (int index = 0; index < stances.Count; index++)
            {
                ChipExpressionStanceConfig stance = stances[index];
                if (stance != null
                    && string.Equals(stance.StanceKey, stanceKey, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 判断目标是否是当前芯片对应的正式启用根槽。
        /// </summary>
        private static bool IsWritableActiveRoot(TriggerSlotState rootSlot, Thing chip)
        {
            return rootSlot != null
                && rootSlot.IsActive
                && !rootSlot.IsDisabled
                && !rootSlot.IsBindingMirror
                && chip != null
                && rootSlot.LoadedChip == chip;
        }
    }
}
