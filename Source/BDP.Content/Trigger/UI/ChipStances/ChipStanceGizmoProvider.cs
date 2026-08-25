using System;
using System.Collections.Generic;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Content.Trigger.UI.ChipStances
{
    /// <summary>
    /// 为获准开放玩家芯片控制的触发体提供通用姿态按钮。
    /// 只要当前形态声明多个姿态，内容芯片就会自动取得按钮。
    /// </summary>
    public sealed class ChipStanceGizmoProvider : ITriggerExternalGizmoProvider
    {
        /// <summary>
        /// 按正式槽位顺序构建当前形态内部的姿态按钮。
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
                IReadOnlyList<ChipStanceOptionSnapshot> stanceOptions =
                    reader.GetChipStanceOptions(chip);
                if (stanceOptions == null || stanceOptions.Count <= 1)
                {
                    continue;
                }

                string currentStanceKey = reader.GetChipStanceKey(chip);
                ChipStanceOptionSnapshot currentStance = FindStance(stanceOptions, currentStanceKey);
                ChipStanceOptionSnapshot nextStance = FindNextStance(stanceOptions, currentStanceKey);
                if (currentStance == null || nextStance == null)
                {
                    continue;
                }

                Thing capturedChip = chip;
                yield return new Command_ChipStance
                {
                    defaultLabel = "BDP_Command_ChipStance_Switch".Translate(currentStance.DisplayLabel),
                    defaultDesc = "BDP_Command_ChipStance_Desc".Translate(
                        currentStance.DisplayLabel,
                        nextStance.DisplayLabel),
                    icon = ResolveIcon(currentStance.GizmoIconTexPath, chip),
                    // 姿态属于芯片实例真值；禁止原版把同名同图标按钮合并后传播一次输入。
                    groupable = false,
                    action = delegate
                    {
                        commands.RequestCycleChipStance(capturedChip);
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
        /// </summary>
        private static bool IsPlayerControlAllowed(Pawn pawn)
        {
            ThingWithComps equipment = pawn?.equipment?.Primary;
            return equipment?.def?.GetModExtension<TriggerLoadoutPanelExtension>() != null;
        }

        /// <summary>
        /// 动态构建当前形态内按作者顺序排列的右键姿态菜单。
        /// </summary>
        private static IEnumerable<FloatMenuOption> BuildRightClickOptions(
            ITriggerLoadoutReader reader,
            ITriggerLoadoutCommands commands,
            Thing chip)
        {
            IReadOnlyList<ChipStanceOptionSnapshot> stanceOptions = reader?.GetChipStanceOptions(chip);
            if (stanceOptions == null)
            {
                yield break;
            }

            string currentStanceKey = reader.GetChipStanceKey(chip);
            for (int index = 0; index < stanceOptions.Count; index++)
            {
                ChipStanceOptionSnapshot option = stanceOptions[index];
                if (option == null || string.IsNullOrWhiteSpace(option.StanceKey))
                {
                    continue;
                }

                bool isCurrent = string.Equals(
                    option.StanceKey,
                    currentStanceKey,
                    StringComparison.OrdinalIgnoreCase);
                string label = isCurrent
                    ? "BDP_Command_ChipStance_CurrentOption".Translate(option.DisplayLabel).ToString()
                    : option.DisplayLabel;
                if (isCurrent)
                {
                    yield return new FloatMenuOption(label, null);
                    continue;
                }

                string targetStanceKey = option.StanceKey;
                yield return new FloatMenuOption(
                    label,
                    delegate
                    {
                        commands?.RequestSwitchChipStance(chip, targetStanceKey);
                    });
            }
        }

        /// <summary>
        /// 查找当前姿态。
        /// </summary>
        private static ChipStanceOptionSnapshot FindStance(
            IReadOnlyList<ChipStanceOptionSnapshot> stanceOptions,
            string stanceKey)
        {
            if (stanceOptions == null || string.IsNullOrWhiteSpace(stanceKey))
            {
                return null;
            }

            for (int index = 0; index < stanceOptions.Count; index++)
            {
                ChipStanceOptionSnapshot option = stanceOptions[index];
                if (option != null
                    && string.Equals(option.StanceKey, stanceKey, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            return null;
        }

        /// <summary>
        /// 按作者顺序解析下一姿态，末项回绕首项。
        /// </summary>
        private static ChipStanceOptionSnapshot FindNextStance(
            IReadOnlyList<ChipStanceOptionSnapshot> stanceOptions,
            string currentStanceKey)
        {
            if (stanceOptions == null || stanceOptions.Count == 0)
            {
                return null;
            }

            for (int index = 0; index < stanceOptions.Count; index++)
            {
                ChipStanceOptionSnapshot option = stanceOptions[index];
                if (option != null
                    && string.Equals(option.StanceKey, currentStanceKey, StringComparison.OrdinalIgnoreCase))
                {
                    return stanceOptions[(index + 1) % stanceOptions.Count];
                }
            }

            return stanceOptions[0];
        }

        /// <summary>
        /// 优先读取姿态贴图；路径为空或资源不存在时回退芯片物品图标。
        /// </summary>
        private static Texture2D ResolveIcon(string gizmoIconTexPath, Thing chip)
        {
            Texture2D stanceIcon = string.IsNullOrWhiteSpace(gizmoIconTexPath)
                ? null
                : ContentFinder<Texture2D>.Get(gizmoIconTexPath, false);
            Texture2D chipIcon = chip != null && chip.def != null ? chip.def.uiIcon : null;
            return stanceIcon ?? chipIcon ?? BaseContent.BadTex;
        }
    }
}
