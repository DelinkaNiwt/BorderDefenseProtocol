using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Content.Trigger.UI.ChipModes
{
    /// <summary>
    /// 为获准开放玩家芯片控制的触发体提供通用形态按钮。
    /// 具体芯片只需在 XML（定义文件）中声明形态，不需要逐枚注册 C#（C#语言）代码。
    /// </summary>
    public sealed class ChipModeGizmoProvider : ITriggerExternalGizmoProvider
    {
        /// <summary>
        /// 按主侧、副侧、特殊侧和槽位索引的正式顺序构建形态按钮。
        /// </summary>
        public IEnumerable<Gizmo> BuildGizmos(TriggerExternalGizmoContext context)
        {
            if (!IsPlayerControlAllowed(context?.OwnerPawn)
                || context.LoadoutReader == null
                || context.LoadoutCommands == null)
            {
                yield break;
            }

            ITriggerLoadoutReader reader = context.LoadoutReader;
            ITriggerLoadoutCommands commands = context.LoadoutCommands;
            foreach (ITriggerSlotState slot in reader.GetAllSlots())
            {
                if (slot == null
                    || !slot.IsActive
                    || slot.IsBindingMirror
                    || slot.LoadedChip == null)
                {
                    continue;
                }

                Thing chip = slot.LoadedChip;
                IReadOnlyList<ChipModeOptionSnapshot> modeOptions =
                    reader.GetChipModeOptions(chip);
                if (modeOptions == null || modeOptions.Count <= 1)
                {
                    continue;
                }

                string currentModeKey = reader.GetChipModeKey(chip);
                ChipModeOptionSnapshot currentMode = FindMode(modeOptions, currentModeKey);
                ChipModeOptionSnapshot nextMode = FindNextMode(modeOptions, currentModeKey);
                if (currentMode == null || nextMode == null)
                {
                    continue;
                }

                Thing capturedChip = chip;
                string currentModeLabel = BuildModeActionLabel(currentMode.DisplayLabel, chip);
                string nextModeLabel = BuildModeActionLabel(nextMode.DisplayLabel, chip);
                string switchLabel = "BDP_Command_ChipMode_Switch".Translate(currentModeLabel);
                yield return new Command_ChipMode
                {
                    defaultLabel = switchLabel,
                    defaultDesc = "BDP_Command_ChipMode_Desc".Translate(
                        currentModeLabel,
                        nextModeLabel),
                    icon = ResolveIcon(currentMode.GizmoIconTexPath, chip),
                    action = delegate
                    {
                        commands.RequestCycleChipMode(capturedChip);
                    },
                    RightClickOptionsGetter = delegate
                    {
                        return BuildRightClickOptions(reader, commands, capturedChip);
                    }
                };
            }
        }

        /// <summary>
        /// 判断当前主装备是否明确许可普通玩家控制芯片。
        /// 与 Trion 芯片面板共用同一许可，避免隐藏业务触发体意外暴露按钮。
        /// </summary>
        private static bool IsPlayerControlAllowed(Pawn pawn)
        {
            ThingWithComps equipment = pawn?.equipment?.Primary;
            return equipment?.def?.GetModExtension<TriggerLoadoutPanelExtension>() != null;
        }

        /// <summary>
        /// 动态构建按 XML（定义文件）顺序排列的右键形态菜单。
        /// </summary>
        private static IEnumerable<FloatMenuOption> BuildRightClickOptions(
            ITriggerLoadoutReader reader,
            ITriggerLoadoutCommands commands,
            Thing chip)
        {
            IReadOnlyList<ChipModeOptionSnapshot> modeOptions =
                reader?.GetChipModeOptions(chip);
            if (modeOptions == null)
            {
                yield break;
            }

            string currentModeKey = reader.GetChipModeKey(chip);
            for (int index = 0; index < modeOptions.Count; index++)
            {
                ChipModeOptionSnapshot option = modeOptions[index];
                if (option == null || string.IsNullOrWhiteSpace(option.ModeKey))
                {
                    continue;
                }

                bool isCurrent = string.Equals(
                    option.ModeKey,
                    currentModeKey,
                    StringComparison.OrdinalIgnoreCase);
                string modeActionLabel = BuildModeActionLabel(option.DisplayLabel, chip);
                string label = isCurrent
                    ? "BDP_Command_ChipMode_CurrentOption".Translate(modeActionLabel).ToString()
                    : modeActionLabel;
                if (isCurrent)
                {
                    yield return new FloatMenuOption(label, null);
                    continue;
                }

                string targetModeKey = option.ModeKey;
                yield return new FloatMenuOption(
                    label,
                    delegate
                    {
                        commands?.RequestSwitchChipMode(chip, targetModeKey);
                    });
            }
        }

        /// <summary>
        /// 查找当前形态。
        /// </summary>
        private static ChipModeOptionSnapshot FindMode(
            IReadOnlyList<ChipModeOptionSnapshot> modeOptions,
            string modeKey)
        {
            if (modeOptions == null || string.IsNullOrWhiteSpace(modeKey))
            {
                return null;
            }

            for (int index = 0; index < modeOptions.Count; index++)
            {
                ChipModeOptionSnapshot option = modeOptions[index];
                if (option != null
                    && string.Equals(option.ModeKey, modeKey, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            return null;
        }

        /// <summary>
        /// 按作者顺序解析下一形态，末项回绕首项。
        /// </summary>
        private static ChipModeOptionSnapshot FindNextMode(
            IReadOnlyList<ChipModeOptionSnapshot> modeOptions,
            string currentModeKey)
        {
            if (modeOptions == null || modeOptions.Count == 0)
            {
                return null;
            }

            for (int index = 0; index < modeOptions.Count; index++)
            {
                ChipModeOptionSnapshot option = modeOptions[index];
                if (option != null
                    && string.Equals(
                        option.ModeKey,
                        currentModeKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return modeOptions[(index + 1) % modeOptions.Count];
                }
            }

            return modeOptions[0];
        }

        /// <summary>
        /// 构建带枪械类别后缀的形态动作名。
        /// 格式："毒蛇(手枪型)" 或 "毒蛇"（无枪械类别时）。
        /// 标签从芯片配置直接读取，不查 DefDatabase。
        /// </summary>
        private static string BuildModeActionLabel(string actionName, Thing chip)
        {
            string label = ResolveSourceVariantLabel(chip);
            if (string.IsNullOrWhiteSpace(label))
            {
                return actionName;
            }

            return actionName + "(" + label + ")";
        }

        /// <summary>
        /// 从 Core 中性来源快照读取可空变体显示标签。
        /// </summary>
        private static string ResolveSourceVariantLabel(Thing chip)
        {
            return chip != null
                ? ChipInstanceSurfaceAccess.ReadSourceReference(chip).SourceVariantLabel
                : null;
        }

        /// <summary>
        /// 优先读取形态贴图；路径为空或资源不存在时回退芯片物品图标。
        /// </summary>
        private static Texture2D ResolveIcon(string gizmoIconTexPath, Thing chip)
        {
            Texture2D modeIcon = string.IsNullOrWhiteSpace(gizmoIconTexPath)
                ? null
                : ContentFinder<Texture2D>.Get(gizmoIconTexPath, false);
            Texture2D chipIcon = chip != null && chip.def != null
                ? chip.def.uiIcon
                : null;
            return modeIcon ?? chipIcon ?? BaseContent.BadTex;
        }
    }
}
