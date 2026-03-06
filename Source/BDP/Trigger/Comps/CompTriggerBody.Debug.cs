using System.Collections.Generic;
using System.Linq;
using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody的调试/开发工具部分（Fix-8：partial class提取）。
    /// 包含：GetDebugGizmos, HasEmptySlot,
    ///       FillRandomChip, FillSpecificChip, GetChipMenuOptions,
    ///       ClearSide, Command_ActionWithMenu内部类。
    ///
    /// v2.1：移除GetCombatBodyGizmos，战斗体生成/解除逻辑已迁移到Gene_TrionGland。
    /// </summary>
    public partial class CompTriggerBody
    {
        private IEnumerable<Gizmo> GetDebugGizmos()
        {
            bool hasEmptyLeft = HasEmptySlot(SlotSide.LeftHand);
            yield return new Command_ActionWithMenu
            {
                defaultLabel = "[Dev] 填充左手槽",
                defaultDesc = "左键：随机填充\n右键：选择芯片",
                action = () => FillRandomChip(SlotSide.LeftHand),
                menuOptions = GetChipMenuOptions(SlotSide.LeftHand),
                Disabled = !hasEmptyLeft,
                disabledReason = "左手槽无空槽"
            };
            bool hasEmptyRight = Props.hasRightHand && HasEmptySlot(SlotSide.RightHand);
            yield return new Command_ActionWithMenu
            {
                defaultLabel = "[Dev] 填充右手槽",
                defaultDesc = "左键：随机填充\n右键：选择芯片",
                action = () => FillRandomChip(SlotSide.RightHand),
                menuOptions = GetChipMenuOptions(SlotSide.RightHand),
                Disabled = !hasEmptyRight,
                disabledReason = "右手槽无空槽或无右手槽"
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 清空左手槽",
                defaultDesc = "关闭并卸载左手槽所有芯片（绕过allowChipManagement）",
                action = () => ClearSide(SlotSide.LeftHand)
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 清空右手槽",
                defaultDesc = "关闭并卸载右手槽所有芯片（绕过allowChipManagement）",
                action = () => ClearSide(SlotSide.RightHand)
            };
            // v2.1：特殊槽调试按钮
            if (Props.specialSlotCount > 0)
            {
                bool hasEmptySpecial = HasEmptySlot(SlotSide.Special);
                yield return new Command_ActionWithMenu
                {
                    defaultLabel = "[Dev] 填充特殊槽",
                    defaultDesc = "左键：随机填充\n右键：选择芯片",
                    action = () => FillRandomChip(SlotSide.Special),
                    menuOptions = GetChipMenuOptions(SlotSide.Special),
                    Disabled = !hasEmptySpecial,
                    disabledReason = "特殊槽无空槽"
                };
                yield return new Command_Action
                {
                    defaultLabel = "[Dev] 清空特殊槽",
                    defaultDesc = "关闭并卸载特殊槽所有芯片（绕过allowChipManagement）",
                    action = () => ClearSide(SlotSide.Special)
                };
                yield return new Command_Action
                {
                    defaultLabel = "[Dev] 激活全部特殊槽",
                    defaultDesc = "调用ActivateAllSpecial()",
                    action = ActivateAllSpecial
                };
                yield return new Command_Action
                {
                    defaultLabel = "[Dev] 关闭全部特殊槽",
                    defaultDesc = "调用DeactivateAllSpecial()",
                    action = DeactivateAllSpecial
                };
            }
        }

        private bool HasEmptySlot(SlotSide side)
        {
            return GetSlotsForSide(side)?.Any(s => s.loadedChip == null) ?? false;
        }

        private void FillRandomChip(SlotSide side)
        {
            var chipDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.comps != null && d.comps.Any(c => c is CompProperties_TriggerChip))
                .ToList();
            if (chipDefs.Count == 0)
            {
                Log.Warning("[BDP] FillRandomChip: 未找到任何TriggerChip ThingDef");
                return;
            }
            var emptySlot = GetSlotsForSide(side)?.FirstOrDefault(s => s.loadedChip == null);
            if (emptySlot == null) return;
            LoadChipInternal(side, emptySlot.index, ThingMaker.MakeThing(chipDefs.RandomElement()));
        }

        /// <summary>将指定ThingDef的芯片装入指定侧第一个空槽（供右键菜单调用）。</summary>
        private void FillSpecificChip(SlotSide side, ThingDef chipDef)
        {
            var emptySlot = GetSlotsForSide(side)?.FirstOrDefault(s => s.loadedChip == null);
            if (emptySlot == null) return;
            LoadChipInternal(side, emptySlot.index, ThingMaker.MakeThing(chipDef));
        }

        /// <summary>生成指定侧的芯片选择菜单项列表（供右键FloatMenu）。</summary>
        private List<FloatMenuOption> GetChipMenuOptions(SlotSide side)
        {
            var chipDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.comps != null && d.comps.Any(c => c is CompProperties_TriggerChip))
                .OrderBy(d => d.defName)
                .ToList();
            var options = new List<FloatMenuOption>(chipDefs.Count);
            foreach (var def in chipDefs)
            {
                var d = def; // 闭包捕获
                options.Add(new FloatMenuOption(
                    $"{d.LabelCap} ({d.defName})",
                    () => FillSpecificChip(side, d)));
            }
            return options;
        }

        private void ClearSide(SlotSide side)
        {
            var slots = GetSlotsForSide(side);
            if (slots == null) return;
            foreach (var slot in slots)
            {
                if (slot.isActive) DeactivateSlot(slot);
                slot.loadedChip = null;
            }
        }

        /// <summary>
        /// 支持右键FloatMenu的Command_Action子类。
        /// 左键执行action，右键弹出menuOptions列表。
        /// </summary>
        private class Command_ActionWithMenu : Command_Action
        {
            public List<FloatMenuOption> menuOptions;

            public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
                => menuOptions ?? System.Linq.Enumerable.Empty<FloatMenuOption>();
        }
    }
}
