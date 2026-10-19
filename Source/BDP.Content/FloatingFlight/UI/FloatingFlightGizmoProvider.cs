using System;
using System.Collections.Generic;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Content.FloatingFlight.UI
{
    /// <summary>
    /// 为已征召且拥有浮空芯片能力的玩家 Pawn 提供模式按钮。
    /// 按钮使用 Pawn 个体偏好，多个 Pawn 仅在当前模式和操作合法性都一致时聚合。
    /// </summary>
    public sealed class FloatingFlightGizmoProvider : ITriggerExternalGizmoProvider
    {
        /// <summary>构建当前 Trigger 宿主 Pawn 的浮空按钮。</summary>
        public IEnumerable<Gizmo> BuildGizmos(TriggerExternalGizmoContext context)
        {
            Pawn pawn = context?.OwnerPawn;
            if (pawn == null
                || !pawn.IsPlayerControlled
                || !pawn.Drafted
                || !FloatingFlightCapabilityResolver.HasCapability(pawn))
            {
                yield break;
            }

            FloatingFlightMode current = FloatingFlightModeResolver.ResolvePreferredMode(pawn);
            FloatingFlightMode next = FloatingFlightModeResolver.ResolveNextSelectableMode(pawn);
            bool canAlways = FloatingFlightModeResolver.CanSelectAlways(pawn);
            Pawn capturedPawn = pawn;
            string currentLabel = ModeLabel(current);
            string nextLabel = ModeLabel(next);
            Command_FloatingFlightMode command = new Command_FloatingFlightMode
            {
                defaultLabel = "BDP_Command_FloatingFlight_Switch".Translate(ShortModeLabel(current)),
                defaultDesc = BuildDescription(current, next, canAlways),
                icon = ResolveIcon(context.LoadoutReader),
                groupable = true,
                groupKey = BuildGroupKey(current, canAlways),
                groupKeyIgnoreContent = BuildGroupKey(current, canAlways),
                action = delegate
                {
                    FloatingFlightModeResolver.CyclePreferredMode(capturedPawn);
                },
                RightClickOptionsGetter = delegate
                {
                    return BuildRightClickOptions(capturedPawn);
                }
            };

            yield return command;
        }

        /// <summary>构建带屋顶抑制提示的按钮说明。</summary>
        private static string BuildDescription(
            FloatingFlightMode current,
            FloatingFlightMode next,
            bool canAlways)
        {
            string description = "BDP_Command_FloatingFlight_Description".Translate(
                ModeLabel(current),
                ModeLabel(next));
            if (current == FloatingFlightMode.Always && !canAlways)
            {
                description += "\n" + "BDP_Command_FloatingFlight_AlwaysSuppressed".Translate();
            }

            return description;
        }

        /// <summary>构建当前 Pawn 的右键模式菜单。</summary>
        private static IEnumerable<FloatMenuOption> BuildRightClickOptions(Pawn pawn)
        {
            FloatingFlightMode current = FloatingFlightModeResolver.ResolvePreferredMode(pawn);
            FloatingFlightMode[] modes =
            {
                FloatingFlightMode.Off,
                FloatingFlightMode.MoveOnly,
                FloatingFlightMode.Always
            };

            for (int index = 0; index < modes.Length; index++)
            {
                FloatingFlightMode mode = modes[index];
                string modeLabel = ModeLabel(mode);
                if (mode == current)
                {
                    yield return new FloatMenuOption(
                        "BDP_Command_FloatingFlight_CurrentOption".Translate(modeLabel).ToString(),
                        null);
                    continue;
                }

                if (mode == FloatingFlightMode.Always
                    && !FloatingFlightModeResolver.CanSelectAlways(pawn))
                {
                    FloatMenuOption blocked = new FloatMenuOption(modeLabel, null);
                    blocked.tooltip = new TipSignal(
                        "BDP_Command_FloatingFlight_AlwaysBlocked".Translate().ToString());
                    yield return blocked;
                    continue;
                }

                FloatingFlightMode capturedMode = mode;
                yield return new FloatMenuOption(
                    modeLabel,
                    delegate
                    {
                        FloatingFlightModeResolver.TrySetPreferredMode(
                            pawn,
                            capturedMode,
                            out string rejectionKey);
                    });
            }
        }

        /// <summary>读取当前 Trigger 中第一枚激活芯片的图标作为按钮回退图标。</summary>
        private static Texture2D ResolveIcon(ITriggerLoadoutReader reader)
        {
            if (reader != null)
            {
                foreach (ITriggerSlotState slot in reader.GetActiveSlots())
                {
                    if (slot?.LoadedChip?.def?.uiIcon != null)
                    {
                        return slot.LoadedChip.def.uiIcon;
                    }
                }
            }

            return BaseContent.BadTex;
        }

        /// <summary>把当前模式和“始终浮空”操作合法性编码进原版聚合键。</summary>
        private static int BuildGroupKey(FloatingFlightMode mode, bool canAlways)
        {
            return ("BDP_FloatingFlight:" + (int)mode + ":" + canAlways).GetHashCode();
        }

        /// <summary>读取三态模式的本地化显示名。</summary>
        private static string ModeLabel(FloatingFlightMode mode)
        {
            switch (mode)
            {
                case FloatingFlightMode.MoveOnly:
                    return "BDP_FloatingFlight_Mode_MoveOnly".Translate().ToString();
                case FloatingFlightMode.Always:
                    return "BDP_FloatingFlight_Mode_Always".Translate().ToString();
                default:
                    return "BDP_FloatingFlight_Mode_Off".Translate().ToString();
            }
        }

        /// <summary>读取按钮使用的短模式名，避免完整说明挤占按钮标签。</summary>
        private static string ShortModeLabel(FloatingFlightMode mode)
        {
            switch (mode)
            {
                case FloatingFlightMode.MoveOnly:
                    return "BDP_FloatingFlight_ModeShort_MoveOnly".Translate().ToString();
                case FloatingFlightMode.Always:
                    return "BDP_FloatingFlight_ModeShort_Always".Translate().ToString();
                default:
                    return "BDP_FloatingFlight_ModeShort_Off".Translate().ToString();
            }
        }
    }
}
