using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Requirements;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口右侧芯片与槽位详情面板。
    /// </summary>
    internal sealed class Panel_ChipDetail
    {
        /// <summary>
        /// 绘制芯片或槽位详情。
        /// </summary>
        internal void DrawChipDetail(
            Rect rect,
            Thing selectedChip,
            ITriggerSlotState selectedSlot,
            ITriggerLoadoutReader reader,
            int availableChipCount)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(10f);

            Text.Font = GameFont.Small;
            Widgets.Label(
                new Rect(inner.x, inner.y, inner.width, 24f),
                "BDP_Window_TriggerAssembly_Detail".Translate());
            Text.Font = GameFont.Tiny;

            if (selectedChip != null)
            {
                DrawSelectedChip(new Rect(inner.x, inner.y + 34f, inner.width, inner.height - 34f), selectedChip);
                return;
            }

            if (selectedSlot != null)
            {
                DrawSelectedSlot(new Rect(inner.x, inner.y + 34f, inner.width, inner.height - 34f), selectedSlot);
                return;
            }

            DrawSummary(new Rect(inner.x, inner.y + 34f, inner.width, inner.height - 34f), reader, availableChipCount);
        }

        /// <summary>
        /// 绘制选中芯片详情。
        /// </summary>
        private static void DrawSelectedChip(Rect rect, Thing chip)
        {
            Widgets.ThingIcon(new Rect(rect.x, rect.y, 54f, 54f), chip);
            Widgets.Label(new Rect(rect.x + 62f, rect.y, rect.width - 62f, 24f), chip.LabelShortCap);

            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(chip);
            bool valid = snapshot != null && snapshot.IsValid;

            float y = rect.y + 68f;
            DrawLine(
                rect,
                ref y,
                "BDP_Window_TriggerAssembly_SlotRegion".Translate(),
                 valid ? FormatSlotRegion(snapshot) : "BDP_Window_TriggerAssembly_Unknown".Translate().ToString());
            DrawLine(
                rect,
                ref y,
                "BDP_Window_TriggerAssembly_SlotOccupancy".Translate(),
                 valid ? FormatSlotOccupancy(snapshot) : "BDP_Window_TriggerAssembly_Unknown".Translate().ToString());
            DrawLine(
                rect,
                ref y,
                "BDP_Window_TriggerAssembly_ActivationDelay".Translate(),
                valid
                    ? "BDP_Window_TriggerAssembly_DelayValue".Translate(
                        (snapshot.ActivationDelayTicks / 60f).ToString("0.#"),
                        (snapshot.DeactivationDelayTicks / 60f).ToString("0.#")).ToString()
                     : "BDP_Window_TriggerAssembly_UnknownValue".Translate().ToString());
            DrawLine(
                rect,
                ref y,
                "BDP_Window_TriggerAssembly_ResidentCost".Translate(),
                 valid ? snapshot.CapacityCost.ToString("0.#") : "BDP_Window_TriggerAssembly_UnknownValue".Translate().ToString());
            DrawLine(
                rect,
                ref y,
                "BDP_Window_TriggerAssembly_ActivationCost".Translate(),
                 valid ? snapshot.ActivationCost.ToString("0.#") : "BDP_Window_TriggerAssembly_UnknownValue".Translate().ToString());
            DrawActivationRequirements(rect, ref y, chip);
        }

        /// <summary>
        /// 在资源行之后绘制唯一的静态激活条件分区。
        /// </summary>
        private static void DrawActivationRequirements(Rect rect, ref float y, Thing chip)
        {
            IReadOnlyList<PawnRequirementSnapshot> requirements =
                ChipActivationRequirementService.Instance.Describe(chip);
            if (requirements == null || requirements.Count == 0)
            {
                return;
            }

            y += 8f;
            Widgets.Label(
                new Rect(rect.x, y, rect.width, 22f),
                "BDP_Window_TriggerAssembly_ActivationRequirements".Translate());
            y += 22f;
            for (int i = 0; i < requirements.Count; i++)
            {
                PawnRequirementSnapshot requirement = requirements[i];
                if (requirement != null)
                {
                    DrawLine(rect, ref y, requirement.Label, requirement.RequiredValueText);
                }
            }
        }

        /// <summary>
        /// 把槽位占用方式转换成玩家可读文字。
        /// </summary>
        private static string FormatSlotOccupancy(ChipDefinitionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "BDP_Window_TriggerAssembly_Unknown".Translate();
            }

            switch (snapshot.SlotOccupancy)
            {
                case ChipSlotOccupancy.Single:
                    return "BDP_Window_TriggerAssembly_SingleSlot".Translate();
                case ChipSlotOccupancy.PairedHands:
                    return "BDP_Window_TriggerAssembly_PairedSlots".Translate();
                default:
                    return "BDP_Window_TriggerAssembly_Unspecified".Translate();
            }
        }

        /// <summary>把槽位区域转换成玩家可读文字。</summary>
        private static string FormatSlotRegion(ChipDefinitionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "BDP_Window_TriggerAssembly_Unknown".Translate();
            }

            return snapshot.SlotRegion == ChipSlotRegion.Special
                ? "BDP_Window_TriggerAssembly_SpecialRegion".Translate()
                : "BDP_Window_TriggerAssembly_MainSubRegion".Translate();
        }

        /// <summary>
        /// 绘制选中槽位详情。
        /// </summary>
        private static void DrawSelectedSlot(Rect rect, ITriggerSlotState slot)
        {
            float y = rect.y;
            DrawLine(rect, ref y, "BDP_Window_TriggerAssembly_Side".Translate(), FormatSide(slot.Side));
            DrawLine(rect, ref y, "BDP_Window_TriggerAssembly_Index".Translate(), slot.Index.ToString());
            DrawLine(
                rect,
                ref y,
                "BDP_Window_TriggerAssembly_LoadedChip".Translate(),
                 slot.LoadedChip != null ? slot.LoadedChip.LabelShortCap : "BDP_None".Translate().ToString());
            DrawLine(rect, ref y, "BDP_Window_TriggerAssembly_Active".Translate(), slot.IsActive ? "BDP_Yes".Translate().ToString() : "BDP_No".Translate().ToString());
            DrawLine(rect, ref y, "BDP_Window_TriggerAssembly_Disabled".Translate(), slot.IsDisabled ? FormatDisableReason(slot.DisabledReason) : "BDP_No".Translate().ToString());
            DrawLine(rect, ref y, "BDP_Window_TriggerAssembly_Mirror".Translate(), slot.IsBindingMirror ? "BDP_Yes".Translate().ToString() : "BDP_No".Translate().ToString());
            DrawLine(
                rect,
                ref y,
                "BDP_Window_TriggerAssembly_Binding".Translate(),
                slot.HasBindingPartner
                    ? FormatSide(slot.BindingPartnerSide) + ":" + slot.BindingPartnerIndex
                     : "BDP_None".Translate().ToString());
        }

        /// <summary>
        /// 绘制未选中时的摘要。
        /// </summary>
        private static void DrawSummary(Rect rect, ITriggerLoadoutReader reader, int availableChipCount)
        {
            int slotCount = 0;
            int loadedCount = 0;
            foreach (ITriggerSlotState slot in reader.GetAllSlots())
            {
                slotCount++;
                if (slot.LoadedChip != null && !slot.IsBindingMirror)
                {
                    loadedCount++;
                }
            }

            float y = rect.y;
            DrawLine(rect, ref y, "BDP_Window_TriggerAssembly_SlotCount".Translate(), slotCount.ToString());
            DrawLine(rect, ref y, "BDP_Window_TriggerAssembly_LoadedCount".Translate(), loadedCount.ToString());
            DrawLine(rect, ref y, "BDP_Window_TriggerAssembly_ContainerChips".Translate(), availableChipCount.ToString());
        }

        /// <summary>把侧别转换成玩家可读名称。</summary>
        private static string FormatSide(TriggerSide side)
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

        /// <summary>把禁用原因转换成玩家可读名称。</summary>
        private static string FormatDisableReason(TriggerDisableReason reason)
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

        /// <summary>
        /// 绘制一行键值详情。
        /// </summary>
        private static void DrawLine(Rect rect, ref float y, string label, string value)
        {
            Widgets.Label(new Rect(rect.x, y, 88f, 22f), label);
            Widgets.Label(new Rect(rect.x + 92f, y, rect.width - 92f, 22f), value);
            y += 24f;
        }
    }
}
