using System.Collections.Generic;
using BDP.Core;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody的战斗体支持部分类（阶段3.3）。
    ///
    /// 职责：
    /// - 战斗体Trion占用和释放
    /// - 特殊槽芯片激活/关闭
    /// - ICombatBodySupport接口实现
    /// - 战斗体状态管理
    /// </summary>
    public partial class CompTriggerBody
    {
        // ═══════════════════════════════════════════
        //  战斗体Trion占用管理
        // ═══════════════════════════════════════════

        /// <summary>
        /// 为战斗体分配Trion（v2.2重构版）。
        ///
        /// 原子性保证：
        ///   1. 先计算总需求量
        ///   2. 检查是否足够（原子性检查）
        ///   3. 设置战斗体激活标志
        ///   4. 逐个Allocate（此时已确保足够）
        ///
        /// 注意：此方法只负责Allocate，不激活特殊槽芯片（由ActivateAllSpecial单独调用）。
        /// </summary>
        public bool TryAllocateTrionForCombatBody()
        {

            var trion = TrionComp;
            if (trion == null)
            {
                Log.Error($"[BDP] TryAllocateTrionForCombatBody失败: TrionComp为null");
                return false;
            }

            // 1. 计算总需求量
            float totalCost = 0f;
            var slotsWithCost = new List<(ChipSlot slot, float cost)>();

            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null && chipComp.Props.allocationCost > 0f)
                {
                    totalCost += chipComp.Props.allocationCost;
                    slotsWithCost.Add((slot, chipComp.Props.allocationCost));
                }
            }

            // 2. 检查是否足够（原子性检查）
            if (trion.Available < totalCost)
            {
                // 改为普通日志，避免红色报错
                return false;
            }

            // 3. 设置战斗体激活标志（必须在Allocate之前设置，因为某些检查依赖此标志）
            IsCombatBodyActive = true;

            // 4. 逐个Allocate（此时已确保足够，理论上不会失败）
            float totalAllocated = 0f;
            int successCount = 0;

            foreach (var (slot, cost) in slotsWithCost)
            {
                bool ok = trion.Allocate(cost);
                if (ok)
                {
                    totalAllocated += cost;
                    successCount++;
                }
                else
                {
                    // 理论上不应该走到这里（因为已经预检查过）
                    Log.Error($"[BDP] Allocate失败（异常情况）: {slot.loadedChip.def.defName} cost={cost:F1} available={trion.Available:F1}");
                }
            }


            // 5. 返回成功
            return true;
        }

        /// <summary>
        /// 开始战斗体激活流程（v2.2重构版 - 已废弃，保留用于兼容性）。
        ///
        /// ⚠️ 此方法已被拆分为TryAllocateTrionForCombatBody + ActivateAllSpecial。
        /// 新代码应使用拆分后的方法以保证原子性。
        ///
        /// 由Gene_TrionGland在战斗体激活时调用。
        /// 流程：设置标志 → 逐个芯片Allocate → 激活特殊槽。
        /// </summary>
        [System.Obsolete("已废弃，请使用TryAllocateTrionForCombatBody + ActivateAllSpecial")]
        public void BeginCombatBodyActivation()
        {
            Log.Message($"[BDP] BeginCombatBodyActivation() 被调用（已废弃）");

            // 1. 设置战斗体激活标志
            IsCombatBodyActive = true;

            // 2. 遍历所有芯片，逐个Allocate（锁定Trion占用）
            var trion = TrionComp;
            float totalAllocated = 0f;
            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null && chipComp.Props.allocationCost > 0f)
                {
                    bool ok = trion?.Allocate(chipComp.Props.allocationCost) ?? false;
                    if (ok)
                    {
                        totalAllocated += chipComp.Props.allocationCost;
                    }
                    else
                    {
                        Log.Warning($"[BDP] Allocate失败: {slot.loadedChip.def.defName} cost={chipComp.Props.allocationCost} available={trion?.Available ?? 0f:F1}");
                    }
                }
            }

            // 3. 激活特殊槽芯片
            ActivateAllSpecial();

            Log.Message($"[BDP] 战斗体激活完成 (allocated={totalAllocated:F1}, trion={trion?.Cur:F1}/{trion?.Max:F1})");
        }

        /// <summary>
        /// 激活所有特殊槽（全部同时激活）。
        /// 特殊槽不参与切换状态机，不受切换冷却影响（不变量⑨⑫）。
        /// 由战斗体模块在战斗体生成时调用。v2.1新增。
        /// </summary>
        public void ActivateAllSpecial()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
            {
                if (slot.loadedChip == null || slot.isActive) continue;
                if (CanActivateChip(SlotSide.Special, slot.index))
                    DoActivate(slot);
            }
        }

        /// <summary>
        /// 关闭所有特殊槽（全部同时关闭）。
        /// 由战斗体模块在战斗体解除时调用。v2.1新增。
        /// </summary>
        public void DeactivateAllSpecial()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
                if (slot.isActive) DeactivateSlot(slot, null);
        }

        // ═══════════════════════════════════════════
        //  战斗体管理
        // ═══════════════════════════════════════════

        /// <summary>
        /// 解除战斗体：关闭所有芯片 → 释放全部Trion占用 → 标记未激活。
        /// 释放逻辑基于trion.Allocated（Single Source of Truth），不依赖芯片是否仍在槽位中。
        /// </summary>
        /// <param name="pawnOverride">显式指定Pawn（用于Notify_Unequipped中，此时OwnerPawn可能已为null）</param>
        public void DismissCombatBody(Pawn pawnOverride = null)
        {
            DeactivateAll(pawnOverride);
            // 清除所有槽位的禁用标志（战斗体解除 → 部位恢复 → 槽位可用）
            foreach (var slot in AllSlots())
                slot.isDisabled = false;
            var trion = (pawnOverride ?? OwnerPawn)?.GetComp<CompTrion>();
            if (trion != null) trion.Release(trion.Allocated);
            IsCombatBodyActive = false;
        }

        /// <summary>
        /// 检查当前Trion是否足够生成战斗体（所有已装载芯片的allocationCost总和）。
        /// </summary>
        public bool CanGenerateCombatBody()
        {
            var trion = TrionComp;
            if (trion == null) return false;
            float totalCost = 0f;
            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null)
                    totalCost += chipComp.Props.allocationCost;
            }
            return trion.Available >= totalCost;
        }

        /// <summary>
        /// Trion可用值耗尽回调——自动解除战斗体。
        /// 由CompTrion.OnAvailableDepleted事件触发。
        /// </summary>
        private void OnTrionDepleted()
        {
            if (!IsCombatBodyActive) return;
            DismissCombatBody();
        }
        // ═══════════════════════════════════════════
        //  ICombatBodySupport接口实现（v11.0战斗体系统重构）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 实现ICombatBodySupport.TryAllocateForCombatBody。
        /// 委托给现有的TryAllocateTrionForCombatBody方法。
        /// </summary>
        bool ICombatBodySupport.TryAllocateForCombatBody()
        {
            return TryAllocateTrionForCombatBody();
        }

        /// <summary>
        /// 实现ICombatBodySupport.ReleaseFromCombatBody。
        /// 委托给DismissCombatBody方法，执行完整的解除逻辑。
        /// </summary>
        void ICombatBodySupport.ReleaseFromCombatBody()
        {
            DismissCombatBody();
        }

        /// <summary>
        /// 实现ICombatBodySupport.ActivateSpecialSlots。
        /// 委托给现有的ActivateAllSpecial方法。
        /// </summary>
        void ICombatBodySupport.ActivateSpecialSlots()
        {
            ActivateAllSpecial();
        }

        /// <summary>
        /// 实现ICombatBodySupport.DeactivateSpecialSlots。
        /// 关闭所有特殊槽芯片。
        /// </summary>
        void ICombatBodySupport.DeactivateSpecialSlots()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
            {
                if (slot.loadedChip != null && slot.isActive)
                {
                    DeactivateSlot(slot);
                }
            }
        }
    }
}
