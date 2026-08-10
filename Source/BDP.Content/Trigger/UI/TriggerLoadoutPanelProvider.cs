using System.Collections.Generic;
using BDP.Core.Requirements;
using BDP.Core.Trion.External;
using BDP.Core.Trigger;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Trigger.UI
{
    /// <summary>
    /// 插入 Trion 状态卡右侧的触发体芯片控制面板。
    /// 只通过 Trigger 正式接口读写，不直接访问 Trigger 内部槽位状态。
    /// </summary>
    public sealed class TriggerLoadoutPanelProvider : ITrionGizmoPanelExtensionProvider
    {
        /// <summary>
        /// 面板内边距。
        /// </summary>
        private const float PanelPadding = 4f;

        /// <summary>
        /// 主/副侧行标签宽度。
        /// </summary>
        private const float RowLabelWidth = 18f;

        /// <summary>
        /// 单个槽位格尺寸。
        /// </summary>
        private const float SlotSize = 28f;

        /// <summary>
        /// 槽位格之间的水平间距。
        /// </summary>
        private const float SlotGap = 5f;

        /// <summary>
        /// 单侧槽位行高度。
        /// </summary>
        private const float RowHeight = 30f;

        /// <summary>
        /// 主/副槽位行之间的垂直间距。
        /// </summary>
        private const float RowGap = 4f;

        /// <summary>
        /// 槽位外层暗凹底色。
        /// </summary>
        private static readonly Color SlotOuterColor = new Color(0.05f, 0.06f, 0.07f, 0.58f);

        /// <summary>
        /// 槽位内层柔和填充色。
        /// </summary>
        private static readonly Color SlotInnerColor = new Color(0.10f, 0.12f, 0.14f, 0.72f);

        /// <summary>
        /// 空槽位低对比边框色。
        /// </summary>
        private static readonly Color EmptySlotBorderColor = new Color(0.25f, 0.28f, 0.32f, 0.62f);

        /// <summary>
        /// 已装未激活槽位边框色。
        /// </summary>
        private static readonly Color LoadedBorderColor = new Color(0.66f, 0.56f, 0.24f, 0.76f);

        /// <summary>
        /// 激活槽位边框色。
        /// </summary>
        private static readonly Color ActiveBorderColor = new Color(0.34f, 0.86f, 0.58f, 0.90f);

        /// <summary>
        /// 等待冲突芯片关闭时使用的亮琥珀色边框。
        /// </summary>
        private static readonly Color WaitingBorderColor = new Color(1f, 0.76f, 0.18f, 0.96f);

        /// <summary>
        /// 禁用槽位边框色。
        /// </summary>
        private static readonly Color DisabledBorderColor = new Color(0.72f, 0.24f, 0.22f, 0.82f);

        /// <summary>
        /// 镜像槽位边框色。
        /// </summary>
        private static readonly Color MirrorBorderColor = new Color(0.42f, 0.62f, 0.92f, 0.82f);

        /// <summary>
        /// 槽位行标签颜色。
        /// </summary>
        private static readonly Color LabelColor = new Color(0.78f, 0.82f, 0.88f, 1f);

        /// <summary>
        /// 前摇进度条颜色。
        /// </summary>
        private static readonly Color WarmupBarColor = new Color(0.28f, 0.70f, 0.94f, 1f);

        /// <summary>
        /// 后摇进度条颜色。
        /// </summary>
        private static readonly Color WinddownBarColor = new Color(0.92f, 0.50f, 0.22f, 1f);

        /// <summary>
        /// 返回当前 Pawn 的主/副触发体槽位面板宽度。
        /// 没有触发体或没有槽位时返回 0，让 Trion Gizmo 保持原宽度。
        /// </summary>
        public float GetWidth(TrionGizmoExtensionContext context)
        {
            Pawn pawn = ResolvePawn(context);
            if (!IsPanelAllowed(pawn))
            {
                return 0f;
            }

            ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            if (reader == null)
            {
                return 0f;
            }

            IEnumerable<ITriggerSlotState> mainSlots = reader.GetSlots(TriggerSide.Main);
            IEnumerable<ITriggerSlotState> subSlots = reader.GetSlots(TriggerSide.Sub);
            int columns = Mathf.Max(CountSlots(mainSlots), CountSlots(subSlots));
            if (columns <= 0)
            {
                return 0f;
            }

            return PanelPadding * 2f
                + RowLabelWidth
                + columns * SlotSize
                + Mathf.Max(0, columns - 1) * SlotGap;
        }

        /// <summary>
        /// 绘制主/副触发体芯片槽面板，并处理槽位点击。
        /// </summary>
        public GizmoResult DrawPanel(TrionGizmoExtensionContext context, Rect panelRect, GizmoRenderParms parms)
        {
            Pawn pawn = ResolvePawn(context);
            if (!IsPanelAllowed(pawn))
            {
                return new GizmoResult(GizmoState.Clear);
            }

            ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            ITriggerInteractionReader interactionReader = TriggerSurfaceAccess.ResolveInteractionReader(pawn);
            ITriggerLoadoutCommands commands = TriggerSurfaceAccess.ResolveLoadoutCommands(pawn);
            if (reader == null)
            {
                return new GizmoResult(GizmoState.Clear);
            }

            Rect innerRect = panelRect.ContractedBy(PanelPadding);
            Rect mainRect = new Rect(innerRect.x, innerRect.y, innerRect.width, RowHeight);
            Rect subRect = new Rect(innerRect.x, mainRect.yMax + RowGap, innerRect.width, RowHeight);

            IEnumerable<ITriggerSlotState> mainSlots = reader.GetSlots(TriggerSide.Main);
            IEnumerable<ITriggerSlotState> subSlots = reader.GetSlots(TriggerSide.Sub);

            bool interacted = false;
            interacted |= DrawSideRow(
                mainRect,
                "BDP_Side_Main".Translate().ToString(),
                TriggerSide.Main,
                mainSlots,
                interactionReader,
                reader,
                commands);
            interacted |= DrawSideRow(
                subRect,
                "BDP_Side_Sub".Translate().ToString(),
                TriggerSide.Sub,
                subSlots,
                interactionReader,
                reader,
                commands);

            return interacted
                ? new GizmoResult(GizmoState.Interacted)
                : new GizmoResult(GizmoState.Clear);
        }

        /// <summary>
        /// 从 Trion Gizmo 上下文中解析 Pawn。
        /// </summary>
        private static Pawn ResolvePawn(TrionGizmoExtensionContext context)
        {
            return context != null ? context.Owner as Pawn : null;
        }

        /// <summary>
        /// 判断当前主装备是否明确许可普通玩家使用芯片控制面板。
        /// </summary>
        private static bool IsPanelAllowed(Pawn pawn)
        {
            ThingWithComps equipment = pawn?.equipment?.Primary;
            return equipment?.def?.GetModExtension<TriggerLoadoutPanelExtension>() != null;
        }

        /// <summary>
        /// 统计指定侧的槽位数量。
        /// </summary>
        private static int CountSlots(IEnumerable<ITriggerSlotState> slots)
        {
            if (slots == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ITriggerSlotState ignored in slots)
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// 绘制一侧的槽位行。
        /// </summary>
        private static bool DrawSideRow(
            Rect rowRect,
            string rowLabel,
            TriggerSide side,
            IEnumerable<ITriggerSlotState> slots,
            ITriggerInteractionReader interactionReader,
            ITriggerLoadoutReader reader,
            ITriggerLoadoutCommands commands)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Color oldColor = GUI.color;
            GUI.color = LabelColor;
            Widgets.Label(new Rect(rowRect.x, rowRect.y, RowLabelWidth, rowRect.height), rowLabel);
            GUI.color = oldColor;
            Text.Anchor = TextAnchor.UpperLeft;

            if (slots == null)
            {
                return false;
            }

            bool interacted = false;
            int visualIndex = 0;
            foreach (ITriggerSlotState slot in slots)
            {
                Rect slotRect = new Rect(
                    rowRect.x + RowLabelWidth + visualIndex * (SlotSize + SlotGap),
                    rowRect.y + (rowRect.height - SlotSize) * 0.5f,
                    SlotSize,
                    SlotSize);
                ITriggerSlotInteractionState interaction = interactionReader != null
                    ? interactionReader.GetSlotInteraction(side, slot.Index)
                    : null;
                interacted |= DrawSlotCell(slotRect, side, slot, interaction, reader, commands);
                visualIndex++;
            }

            return interacted;
        }

        /// <summary>
        /// 绘制单个槽位格，并在点击时提交芯片控制请求。
        /// </summary>
        private static bool DrawSlotCell(
            Rect slotRect,
            TriggerSide side,
            ITriggerSlotState slot,
            ITriggerSlotInteractionState interaction,
            ITriggerLoadoutReader reader,
            ITriggerLoadoutCommands commands)
        {
            if (slot == null)
            {
                return false;
            }

            DrawSlotRecess(slotRect);
            DrawLoadedChipIcon(slotRect, slot, interaction);
            DrawSoftSlotBorder(slotRect, slot, interaction);
            DrawSwitchProgress(slotRect, side, slot, reader);

            if (Mouse.IsOver(slotRect))
            {
                Widgets.DrawHighlight(slotRect);
            }

            TooltipHandler.TipRegion(slotRect, BuildSlotTooltip(slot, interaction, reader));
            return Widgets.ButtonInvisible(slotRect) && SubmitSlotInteraction(slot, interaction, commands);
        }

        /// <summary>
        /// 绘制槽位的内凹背景。
        /// </summary>
        private static void DrawSlotRecess(Rect slotRect)
        {
            Widgets.DrawBoxSolid(slotRect, SlotOuterColor);
            Widgets.DrawBoxSolid(slotRect.ContractedBy(1f), SlotInnerColor);
        }

        /// <summary>
        /// 绘制槽位内芯片图标。
        /// </summary>
        private static void DrawLoadedChipIcon(
            Rect slotRect,
            ITriggerSlotState slot,
            ITriggerSlotInteractionState interaction)
        {
            if (slot.LoadedChip == null)
            {
                return;
            }

            Color oldColor = GUI.color;
            if (slot.IsDisabled)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
            }
            else if (IsWaitingTarget(interaction))
            {
                GUI.color = Color.white;
            }
            else if (!slot.IsActive)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.72f);
            }

            Rect iconRect = slotRect.ContractedBy(4f);
            GUI.DrawTexture(iconRect, slot.LoadedChip.def.uiIcon ?? BaseContent.BadTex);
            GUI.color = oldColor;
        }

        /// <summary>
        /// 绘制槽位低对比边框。
        /// </summary>
        private static void DrawSoftSlotBorder(Rect slotRect, ITriggerSlotState slot, ITriggerSlotInteractionState interaction)
        {
            Color oldColor = GUI.color;
            GUI.color = ResolveBorderColor(slot, interaction);
            Widgets.DrawBox(slotRect, 1);
            GUI.color = oldColor;
        }

        /// <summary>
        /// 解析槽位状态边框颜色。
        /// </summary>
        private static Color ResolveBorderColor(ITriggerSlotState slot, ITriggerSlotInteractionState interaction)
        {
            if (slot.IsDisabled)
            {
                return DisabledBorderColor;
            }

            if (IsWaitingTarget(interaction))
            {
                return WaitingBorderColor;
            }

            if (interaction != null && interaction.OperationKind == TriggerInteractionOperationKind.Mirror)
            {
                return MirrorBorderColor;
            }

            if (slot.IsActive)
            {
                return ActiveBorderColor;
            }

            return slot.LoadedChip != null ? LoadedBorderColor : EmptySlotBorderColor;
        }

        /// <summary>
        /// 判断当前交互投影是否表示正在等待冲突者关闭的目标。
        /// </summary>
        private static bool IsWaitingTarget(ITriggerSlotInteractionState interaction)
        {
            return interaction != null
                && interaction.Reason == TriggerInteractionReason.WaitingForConflicts;
        }

        /// <summary>
        /// 提交单个槽位的玩家操作。
        /// </summary>
        private static bool SubmitSlotInteraction(
            ITriggerSlotState slot,
            ITriggerSlotInteractionState interaction,
            ITriggerLoadoutCommands commands)
        {
            if (slot == null || slot.LoadedChip == null)
            {
                return false;
            }

            if (commands == null)
            {
                Messages.Message("BDP_Message_Chip_OperationUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (interaction == null)
            {
                Messages.Message("BDP_Message_Chip_OperationUnavailable".Translate(), MessageTypeDefOf.NeutralEvent, false);
                return false;
            }

            if (interaction.Availability != TriggerInteractionAvailability.Available)
            {
                if (interaction.Reason == TriggerInteractionReason.ActivationRequirementsUnmet)
                {
                    Messages.Message(
                        BuildActivationRequirementFailureMessage(slot, interaction),
                        MessageTypeDefOf.RejectInput,
                        false);
                    return false;
                }

                Messages.Message(
                    "BDP_Message_Chip_OperationUnavailableReason".Translate(
                        DescribeInteractionReason(interaction.Reason)),
                    MessageTypeDefOf.NeutralEvent,
                    false);
                return false;
            }

            if (interaction.OperationKind == TriggerInteractionOperationKind.Activate
                || interaction.OperationKind == TriggerInteractionOperationKind.SwitchTo)
            {
                return commands.RequestActivate(interaction.ControlSide, interaction.ControlSlotIndex);
            }

            if (interaction.OperationKind == TriggerInteractionOperationKind.Deactivate)
            {
                return commands.RequestDeactivate(interaction.ControlSide);
            }

            if (interaction.OperationKind == TriggerInteractionOperationKind.Mirror)
            {
                Messages.Message(
                    "BDP_Message_TriggerLoadout_Binding".Translate(
                        BuildSideLabel(slot.BindingRootSide),
                        GetPlayerSlotNumber(slot.BindingRootIndex)),
                    MessageTypeDefOf.NeutralEvent,
                    false);
                return false;
            }

            return false;
        }

        /// <summary>
        /// 绘制当前槽位的切换进度条。
        /// </summary>
        private static void DrawSwitchProgress(Rect slotRect, TriggerSide side, ITriggerSlotState slot, ITriggerLoadoutReader reader)
        {
            ITriggerSwitchState switchState = reader != null ? reader.GetSwitchState(side) : null;
            if (switchState == null || !switchState.IsActive)
            {
                return;
            }

            if (switchState.Phase == SwitchPhase.WaitingForConflicts)
            {
                return;
            }

            Rect barRect = new Rect(slotRect.x + 3f, slotRect.yMax - 4f, slotRect.width - 6f, 2f);
            float progress = CalculateSwitchProgress(switchState);
            if (switchState.Phase == SwitchPhase.Activating && slot.Index == switchState.TargetSlotIndex)
            {
                DrawWarmupProgressBar(barRect, progress);
                return;
            }

            if (switchState.Phase == SwitchPhase.Deactivating && slot.Index == switchState.DeactivatingSlotIndex)
            {
                DrawWinddownProgressBar(barRect, progress);
            }
        }

        /// <summary>
        /// 绘制前摇进度条。
        /// 前摇从左向右增长。
        /// </summary>
        private static void DrawWarmupProgressBar(Rect rect, float progress)
        {
            float width = rect.width * Mathf.Clamp01(progress);
            if (width <= 0f)
            {
                return;
            }

            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, width, rect.height), WarmupBarColor);
        }

        /// <summary>
        /// 绘制后摇进度条。
        /// 后摇保持左侧起点不动，右侧逐步退回，表达正在离开当前槽位。
        /// </summary>
        private static void DrawWinddownProgressBar(Rect rect, float progress)
        {
            float remaining = Mathf.Clamp01(1f - progress);
            float width = rect.width * remaining;
            if (width <= 0f)
            {
                return;
            }

            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, width, rect.height), WinddownBarColor);
        }

        /// <summary>
        /// 计算当前切换阶段进度。
        /// </summary>
        private static float CalculateSwitchProgress(ITriggerSwitchState switchState)
        {
            if (switchState == null || !switchState.IsActive)
            {
                return 0f;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int remainingTicks = Mathf.Max(0, switchState.PhaseEndTick - currentTick);
            int duration = switchState.Phase == SwitchPhase.Deactivating
                ? switchState.DeactivationDelayDuration
                : switchState.ActivationDelayDuration;
            if (duration <= 0)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (remainingTicks / (float)duration));
        }

        /// <summary>
        /// 构建单槽位悬浮提示文本。
        /// </summary>
        private static string BuildSlotTooltip(
            ITriggerSlotState slot,
            ITriggerSlotInteractionState interaction,
            ITriggerLoadoutReader reader)
        {
            string chipText = slot.LoadedChip != null
                ? slot.LoadedChip.LabelShortCap
                : "BDP_Message_TriggerLoadout_EmptySlot".Translate().ToString();
            string stateText = slot.IsDisabled
                ? "BDP_Message_TriggerLoadout_Disabled".Translate(DescribeDisableReason(slot.DisabledReason)).ToString()
                : slot.IsActive
                ? "BDP_Message_TriggerLoadout_Active".Translate().ToString()
                    : slot.LoadedChip != null
                        ? "BDP_Message_TriggerLoadout_Standby".Translate().ToString()
                        : "BDP_Message_TriggerLoadout_Empty".Translate().ToString();
            string bindingText = slot.IsBindingMirror
                ? "\n" + "BDP_Message_TriggerLoadout_Binding".Translate(
                    BuildSideLabel(slot.BindingRootSide),
                    GetPlayerSlotNumber(slot.BindingRootIndex)).ToString()
                : string.Empty;
            string switchText = BuildSwitchTooltip(slot.Side, slot.Index, reader);

            return BuildPlayerSlotLabel(slot.Side, slot.Index)
                + "\n" + "BDP_Message_TriggerLoadout_Chip".Translate(chipText)
                + "\n" + "BDP_Message_TriggerLoadout_State".Translate(stateText)
                + bindingText
                + "\n" + "BDP_Message_TriggerLoadout_Action".Translate(
                    BuildPlayerActionText(slot, interaction))
                + (string.IsNullOrEmpty(switchText) ? string.Empty : "\n" + switchText)
                + BuildActivationRequirementTooltip(interaction);
        }

        /// <summary>
        /// 从 Core 快照构造唯一的槽位激活条件分区。
        /// </summary>
        private static string BuildActivationRequirementTooltip(
            ITriggerSlotInteractionState interaction)
        {
            IReadOnlyList<PawnRequirementSnapshot> requirements =
                interaction != null ? interaction.ActivationRequirements : null;
            if (requirements == null || requirements.Count == 0)
            {
                return string.Empty;
            }

            string text = "\n\n" + "BDP_Message_TriggerLoadout_ActivationRequirements".Translate();
            for (int i = 0; i < requirements.Count; i++)
            {
                PawnRequirementSnapshot requirement = requirements[i];
                if (requirement == null)
                {
                    continue;
                }

                string color = requirement.IsSatisfied ? "#84C68A" : "#FF6B6B";
                text += "\n" + "BDP_Message_TriggerLoadout_RequirementLine".Translate(
                    requirement.Label,
                    color,
                    requirement.CurrentValueText,
                    requirement.RequiredValueText);
            }

            return text;
        }

        /// <summary>
        /// 从 Core 失败快照构造一次完整的点击拒绝提示。
        /// </summary>
        private static string BuildActivationRequirementFailureMessage(
            ITriggerSlotState slot,
            ITriggerSlotInteractionState interaction)
        {
            string message = "BDP_Message_Chip_ActivationFailure".Translate(
                slot?.LoadedChip != null
                    ? slot.LoadedChip.LabelShortCap
                    : "BDP_Message_Chip_Default".Translate().ToString());
            IReadOnlyList<PawnRequirementSnapshot> requirements =
                interaction != null ? interaction.ActivationRequirements : null;
            if (requirements == null)
            {
                return message;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                PawnRequirementSnapshot requirement = requirements[i];
                if (requirement != null && !requirement.IsSatisfied)
                {
                    message += "\n- " + requirement.FailureReason;
                }
            }

            return message;
        }

        /// <summary>
        /// 把底层从 0 开始的槽位索引转换为玩家从 1 开始的显示编号。
        /// </summary>
        private static int GetPlayerSlotNumber(int internalSlotIndex)
        {
            return internalSlotIndex + 1;
        }

        /// <summary>
        /// 构建面向玩家的侧别与槽位标题。
        /// </summary>
        private static string BuildPlayerSlotLabel(TriggerSide side, int internalSlotIndex)
        {
            switch (side)
            {
                case TriggerSide.Main:
                    return "BDP_Slot_Main".Translate(GetPlayerSlotNumber(internalSlotIndex)).ToString();
                case TriggerSide.Sub:
                    return "BDP_Slot_Sub".Translate(GetPlayerSlotNumber(internalSlotIndex)).ToString();
                case TriggerSide.Special:
                    return "BDP_Slot_Special".Translate(GetPlayerSlotNumber(internalSlotIndex)).ToString();
                default:
                    return "BDP_Slot_Default".Translate(GetPlayerSlotNumber(internalSlotIndex)).ToString();
            }
        }

        /// <summary>
        /// 构建玩家可直接理解的当前点击动作。
        /// </summary>
        private static string BuildPlayerActionText(
            ITriggerSlotState slot,
            ITriggerSlotInteractionState interaction)
        {
            if (interaction == null)
            {
                return "BDP_Message_Chip_OperationUnavailable".Translate();
            }

            if (interaction.Availability != TriggerInteractionAvailability.Available)
            {
                return "BDP_Message_Chip_OperationUnavailableReason".Translate(
                    DescribeInteractionReason(interaction.Reason));
            }

            switch (interaction.OperationKind)
            {
                case TriggerInteractionOperationKind.Activate:
                    return "BDP_Message_TriggerLoadout_ClickActivate".Translate();
                case TriggerInteractionOperationKind.SwitchTo:
                    return "BDP_Message_TriggerLoadout_ClickSwitch".Translate();
                case TriggerInteractionOperationKind.Deactivate:
                    return "BDP_Message_TriggerLoadout_ClickDeactivate".Translate();
                case TriggerInteractionOperationKind.Mirror:
                    return "BDP_Message_TriggerLoadout_Binding".Translate(
                        BuildSideLabel(slot.BindingRootSide),
                        GetPlayerSlotNumber(slot.BindingRootIndex));
                default:
                    return "BDP_Message_Chip_OperationUnavailable".Translate();
            }
        }

        /// <summary>
        /// 构建切换状态提示文本。
        /// </summary>
        private static string BuildSwitchTooltip(TriggerSide side, int slotIndex, ITriggerLoadoutReader reader)
        {
            ITriggerSwitchState switchState = reader != null ? reader.GetSwitchState(side) : null;
            if (switchState == null || !switchState.IsActive)
            {
                return string.Empty;
            }

            if (switchState.Phase == SwitchPhase.WaitingForConflicts
                && switchState.TargetSlotIndex == slotIndex)
            {
                return "BDP_Message_TriggerLoadout_WaitConflict".Translate();
            }

            float progress = CalculateSwitchProgress(switchState);
            if (switchState.Phase == SwitchPhase.Deactivating && switchState.DeactivatingSlotIndex == slotIndex)
            {
                return "BDP_Message_TriggerLoadout_Deactivating".Translate(
                    Mathf.RoundToInt((1f - progress) * 100f));
            }

            if (switchState.Phase == SwitchPhase.Activating && switchState.TargetSlotIndex == slotIndex)
            {
                return "BDP_Message_TriggerLoadout_Activating".Translate(
                    Mathf.RoundToInt(progress * 100f));
            }

            return string.Empty;
        }

        /// <summary>
        /// 读取侧别显示名。
        /// </summary>
        private static string BuildSideLabel(TriggerSide side)
        {
            switch (side)
            {
                case TriggerSide.Main:
                    return "BDP_Side_MainFull".Translate();
                case TriggerSide.Sub:
                    return "BDP_Side_SubFull".Translate();
                case TriggerSide.Special:
                    return "BDP_Side_SpecialFull".Translate();
                default:
                    return side.ToString();
            }
        }

        /// <summary>
        /// 描述槽位当前无法操作的原因。
        /// </summary>
        private static string DescribeInteractionReason(TriggerInteractionReason reason)
        {
            switch (reason)
            {
                case TriggerInteractionReason.None:
                    return "BDP_Message_TriggerLoadout_NoSpecialReason".Translate();
                case TriggerInteractionReason.MissingSlot:
                    return "BDP_Message_TriggerLoadout_MissingSlot".Translate();
                case TriggerInteractionReason.EmptySlot:
                    return "BDP_Message_TriggerLoadout_Empty".Translate();
                case TriggerInteractionReason.Disabled:
                    return "BDP_Message_TriggerLoadout_DisabledSlot".Translate();
                case TriggerInteractionReason.SwitchingInProgress:
                    return "BDP_Message_TriggerLoadout_Switching".Translate();
                case TriggerInteractionReason.WaitingForConflicts:
                    return "BDP_Message_TriggerLoadout_WaitConflict".Translate();
                case TriggerInteractionReason.MirrorControlledByRoot:
                    return "BDP_Message_TriggerLoadout_Controlled".Translate();
                case TriggerInteractionReason.AlreadyActive:
                    return "BDP_Message_TriggerLoadout_AlreadyActive".Translate();
                case TriggerInteractionReason.BattleModeUnavailable:
                    return "BDP_Message_TriggerLoadout_BattleUnavailable".Translate();
                case TriggerInteractionReason.ActivationRequirementsUnmet:
                    return "BDP_Message_TriggerLoadout_RequirementsUnmet".Translate();
                case TriggerInteractionReason.NoFormalAction:
                    return "BDP_Message_TriggerLoadout_NoAction".Translate();
                default:
                    return reason.ToString();
            }
        }

        /// <summary>
        /// 描述槽位禁用原因。
        /// </summary>
        private static string DescribeDisableReason(TriggerDisableReason reason)
        {
            switch (reason)
            {
                case TriggerDisableReason.None:
                    return "BDP_Message_TriggerLoadout_NotDisabled".Translate();
                case TriggerDisableReason.MissingRequiredBodyPart:
                    return "BDP_Message_TriggerLoadout_MissingBodyPart".Translate();
                case TriggerDisableReason.CombatBodyUnavailable:
                    return "BDP_Message_TriggerLoadout_BattleUnavailable".Translate();
                default:
                    return reason.ToString();
            }
        }
    }
}
