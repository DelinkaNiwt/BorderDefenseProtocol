using System.Collections.Generic;
using System.Linq;
using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody部分类 - 槽位管理模块
    ///
    /// 职责：
    /// - 槽位初始化和查询
    /// - 芯片装载/卸载
    /// - 按侧Verb数据管理
    /// - Trion预占用同步
    /// </summary>
    public partial class CompTriggerBody
    {
        // ═══════════════════════════════════════════
        //  槽位初始化和查询
        // ═══════════════════════════════════════════

        /// <summary>懒初始化槽位列表（兼容CharacterEditor等外部工具）。</summary>
        private void EnsureSlotsInitialized()
        {
            if (leftHandSlots == null)
                leftHandSlots = InitSlots(SlotSide.LeftHand, Props.leftHandSlotCount);
            if (Props.hasRightHand && rightHandSlots == null)
                rightHandSlots = InitSlots(SlotSide.RightHand, Props.rightHandSlotCount);
            // v2.1：特殊槽懒初始化
            if (Props.specialSlotCount > 0 && specialSlots == null)
                specialSlots = InitSlots(SlotSide.Special, Props.specialSlotCount);
        }

        /// <summary>获取指定侧的槽位列表（统一替代重复的三元表达式）。</summary>
        private List<ChipSlot> GetSlotsForSide(SlotSide side)
        {
            switch (side)
            {
                case SlotSide.LeftHand:  return leftHandSlots;
                case SlotSide.RightHand: return rightHandSlots;
                case SlotSide.Special:   return specialSlots;
                default:                 return null;
            }
        }

        public ChipSlot GetSlot(SlotSide side, int index)
        {
            var list = GetSlotsForSide(side);
            if (list == null || index < 0 || index >= list.Count) return null;
            return list[index];
        }

        public IEnumerable<ChipSlot> AllSlots()
        {
            if (leftHandSlots != null) foreach (var s in leftHandSlots) yield return s;
            if (rightHandSlots != null) foreach (var s in rightHandSlots) yield return s;
            // v2.1：包含特殊槽
            if (specialSlots != null) foreach (var s in specialSlots) yield return s;
        }

        /// <summary>遍历所有已激活且已装载芯片的槽位（手动yield避免LINQ委托分配）。</summary>
        public IEnumerable<ChipSlot> AllActiveSlots()
        {
            if (leftHandSlots != null)
                foreach (var s in leftHandSlots)
                    if (s.isActive && s.loadedChip != null) yield return s;
            if (rightHandSlots != null)
                foreach (var s in rightHandSlots)
                    if (s.isActive && s.loadedChip != null) yield return s;
            if (specialSlots != null)
                foreach (var s in specialSlots)
                    if (s.isActive && s.loadedChip != null) yield return s;
        }

        public bool HasAnyActiveChip()
            => AllSlots().Any(s => s.isActive);

        /// <summary>指定侧是否有激活芯片（§6.3接口约定，供战斗模块检查）。</summary>
        public bool HasActiveChip(SlotSide side)
            => GetActiveSlot(side) != null;

        public ChipSlot GetActiveSlot(SlotSide side)
        {
            return GetSlotsForSide(side)?.FirstOrDefault(s => s.isActive);
        }

        // ═══════════════════════════════════════════
        //  按侧Verb管理（v2.0 §8.3 新增API）
        // ═══════════════════════════════════════════

        /// <summary>设置指定侧的Verb/Tool数据（供WeaponChipEffect调用）。</summary>
        public void SetSideVerbs(SlotSide side, List<VerbProperties> verbs, List<Tool> tools)
        {
            if (side == SlotSide.LeftHand)
            {
                leftHandActiveVerbProps = verbs;
                leftHandActiveTools = tools;
            }
            else
            {
                rightHandActiveVerbProps = verbs;
                rightHandActiveTools = tools;
            }
        }

        /// <summary>清除指定侧的Verb/Tool数据（供WeaponChipEffect调用）。</summary>
        public void ClearSideVerbs(SlotSide side)
        {
            if (side == SlotSide.LeftHand)
            {
                leftHandActiveVerbProps = null;
                leftHandActiveTools = null;
            }
            else
            {
                rightHandActiveVerbProps = null;
                rightHandActiveTools = null;
            }
        }

        /// <summary>查找芯片所在侧别（遍历所有激活槽位）。</summary>
        public SlotSide GetChipSide(Thing chip)
        {
            foreach (var slot in AllActiveSlots())
                if (slot.loadedChip == chip) return slot.side;
            // 未找到时默认左手槽
            return SlotSide.LeftHand;
        }

        // ═══════════════════════════════════════════
        //  芯片装载/卸载
        // ═══════════════════════════════════════════

        /// <summary>将芯片物品装入指定槽位（芯片被触发体"吸收"，从持有者库存移除）。</summary>
        public bool LoadChip(SlotSide side, int slotIndex, Thing chip)
        {
            if (!Props.allowChipManagement) return false;
            var slot = GetSlot(side, slotIndex);
            if (slot == null || slot.loadedChip != null) return false;

            var chipComp = chip?.TryGetComp<TriggerChipComp>();
            if (chipComp == null) return false;

            // 检查槽位限制（v3.1：改用枚举）
            var chipProps = chipComp.Props as CompProperties_TriggerChip;
            if (chipProps != null)
            {
                var restriction = chipProps.slotRestriction;
                var currentSlot = slot.side;

                // 检查槽位限制
                bool canInsert = true;
                if (restriction == ChipSlotRestriction.SpecialOnly)
                {
                    canInsert = (currentSlot == SlotSide.Special);
                }
                else if (restriction == ChipSlotRestriction.HandsOnly)
                {
                    canInsert = (currentSlot == SlotSide.LeftHand || currentSlot == SlotSide.RightHand);
                }
                // ChipSlotRestriction.None 时 canInsert 保持 true

                if (!canInsert)
                {
                    string restrictionDesc = "未知槽位";
                    if (restriction == ChipSlotRestriction.SpecialOnly)
                    {
                        restrictionDesc = "特殊槽位";
                    }
                    else if (restriction == ChipSlotRestriction.HandsOnly)
                    {
                        restrictionDesc = "左右手槽位";
                    }

                    Messages.Message(
                        $"该芯片只能插入{restrictionDesc}",
                        MessageTypeDefOf.RejectInput);
                    return false;
                }
            }

            slot.loadedChip = chip;
            chip.holdingOwner?.Remove(chip);

            // v1.8：装载后同步预占用值
            SyncReservedAllocationToTrion();

            return true;
        }

        /// <summary>从槽位取出芯片物品（生成到Pawn库存或地面）。</summary>
        public bool UnloadChip(SlotSide side, int slotIndex)
        {
            if (!Props.allowChipManagement) return false;
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;

            if (slot.isActive) DeactivateSlot(slot);

            var chip = slot.loadedChip;
            slot.loadedChip = null;

            var pawn = OwnerPawn;
            if (pawn != null && pawn.inventory != null)
                pawn.inventory.TryAddItemNotForSale(chip);
            else
                GenPlace.TryPlaceThing(chip, parent.Position, parent.Map, ThingPlaceMode.Near);

            // v1.8：卸载后同步预占用值
            SyncReservedAllocationToTrion();

            return true;
        }

        // ═══════════════════════════════════════════
        //  Trion预占用同步
        // ═══════════════════════════════════════════

        /// <summary>
        /// 计算所有已装载芯片的总Trion占用量。
        /// </summary>
        private float CalculateTotalAllocationCost()
        {
            float total = 0f;

            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip != null)
                {
                    var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                    if (chipComp != null)
                    {
                        total += chipComp.Props.allocationCost;
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// 同步预占用值到CompTrion。
        /// 在芯片装载/卸载、装备/卸下时调用。
        /// </summary>
        private void SyncReservedAllocationToTrion()
        {
            var compTrion = OwnerPawn?.GetComp<CompTrion>();
            if (compTrion != null)
            {
                float total = CalculateTotalAllocationCost();
                compTrion.UpdateReservedAllocation(total);
            }
        }
    }
}
