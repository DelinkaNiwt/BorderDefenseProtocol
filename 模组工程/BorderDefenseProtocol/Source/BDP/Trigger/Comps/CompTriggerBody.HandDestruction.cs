using BDP.Core;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody部分类 - 手部破坏联动模块
    ///
    /// 职责：
    /// - 手部破坏检测（OnHandDestroyed, OnPartDestroyed）
    /// - 槽位禁用（ForceDeactivateLeftSlots, ForceDeactivateRightSlots）
    /// - 侧别禁用查询（IsSideDisabled）
    /// </summary>
    public partial class CompTriggerBody
    {
        /// <summary>
        /// 查询指定侧是否被禁用（手部/手臂被毁）。
        /// 只要该侧有任一槽位被禁用即返回true。
        /// </summary>
        public bool IsSideDisabled(SlotSide side)
        {
            var slots = side == SlotSide.LeftHand ? leftHandSlots
                      : side == SlotSide.RightHand ? rightHandSlots
                      : specialSlots;
            if (slots == null) return false;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].isDisabled) return true;
            return false;
        }

        /// <summary>
        /// 处理手部破坏（实例方法）。
        /// </summary>
        private void OnHandDestroyed(HandSide side)
        {
            Log.Message($"[BDP] 检测到{(side == HandSide.Left ? "左手" : "右手")}被破坏，强制关闭对应槽位");

            if (side == HandSide.Left)
            {
                ForceDeactivateLeftSlots("左手缺失");
            }
            else
            {
                ForceDeactivateRightSlots("右手缺失");
            }
        }

        /// <summary>
        /// 强制禁用左手槽位（手部/手臂被毁时调用）。
        /// 芯片保留在槽位，但标记为禁用，激活的芯片被关闭。
        /// </summary>
        private void ForceDeactivateLeftSlots(string reason)
        {
            if (leftHandSlots == null) return;

            // 获取装备者（使用CompEquippable的Holder属性）
            Pawn pawn = Holder;
            if (pawn == null)
            {
                Log.Warning("[BDP] ForceDeactivateLeftSlots: 无法获取装备者Pawn");
                return;
            }

            foreach (var slot in leftHandSlots)
            {
                // 关闭激活的芯片（但不清除loadedChip）
                if (slot.isActive)
                {
                    slot.isActive = false;
                    Log.Message($"[BDP] 左手槽位[{slot.index}]芯片关闭: {slot.loadedChip?.Label ?? "null"} 原因={reason}");
                }
                // 标记槽位为禁用
                slot.isDisabled = true;
            }

            // 清理左手Verb
            ClearSideVerbs(SlotSide.LeftHand);
            RebuildVerbs(pawn);
        }

        /// <summary>
        /// 强制禁用右手槽位（手部/手臂被毁时调用）。
        /// 芯片保留在槽位，但标记为禁用，激活的芯片被关闭。
        /// </summary>
        private void ForceDeactivateRightSlots(string reason)
        {
            if (rightHandSlots == null) return;

            // 获取装备者（使用CompEquippable的Holder属性）
            Pawn pawn = Holder;
            if (pawn == null)
            {
                Log.Warning("[BDP] ForceDeactivateRightSlots: 无法获取装备者Pawn");
                return;
            }

            foreach (var slot in rightHandSlots)
            {
                // 关闭激活的芯片（但不清除loadedChip）
                if (slot.isActive)
                {
                    slot.isActive = false;
                    Log.Message($"[BDP] 右手槽位[{slot.index}]芯片关闭: {slot.loadedChip?.Label ?? "null"} 原因={reason}");
                }
                // 标记槽位为禁用
                slot.isDisabled = true;
            }

            // 清理右手Verb
            ClearSideVerbs(SlotSide.RightHand);
            RebuildVerbs(pawn);
        }
    }
}
