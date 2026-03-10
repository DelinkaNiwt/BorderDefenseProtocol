using BDP.Core;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody部分类 - 手部破坏联动模块
    ///
    /// 职责：
    /// - 手部完整性检测（CheckHandIntegrity）
    /// - 槽位禁用（ForceDeactivateLeftSlots, ForceDeactivateRightSlots）
    /// - 侧别禁用查询（IsSideDisabled）
    ///
    /// 触发时机：
    /// - 由 Patch_Pawn_PostApplyDamage 在伤害结算后调用 CheckHandIntegrity
    /// - PostApplyDamage 时 MissingPart 已由原版 HediffSet.AddDirect 自动创建
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
        /// 检查手部完整性，发现手部缺失时禁用对应侧槽位。
        ///
        /// 检测逻辑：遍历Pawn身上的Hediff_MissingPart，查找手部相关部位
        /// （Hand、Arm、Shoulder），匹配左右侧后调用ForceDeactivate。
        ///
        /// 幂等性：ForceDeactivateLeftSlots/ForceDeactivateRightSlots内部
        /// 对已禁用槽位不会重复操作，因此可安全多次调用。
        ///
        /// 由 Patch_Pawn_PostApplyDamage 在伤害结算后调用。
        /// </summary>
        public void CheckHandIntegrity(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;

            // 已禁用的侧不需要再检查
            bool leftAlreadyDisabled = IsSideDisabled(SlotSide.LeftHand);
            bool rightAlreadyDisabled = !Props.hasRightHand || IsSideDisabled(SlotSide.RightHand);

            // 两侧都已禁用，无需遍历
            if (leftAlreadyDisabled && rightAlreadyDisabled) return;

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (!(hediff is Hediff_MissingPart missingPart)) continue;
                if (missingPart.Part == null) continue;

                // 检查是否是手部相关部位（Hand、Arm、Shoulder）
                // 只要这些部位缺失，其子树中的Hand必然也缺失
                string defName = missingPart.Part.def.defName;
                if (defName != "Hand" && defName != "Arm" && defName != "Shoulder") continue;

                // 判断左右侧
                HandSide? handSide = GetHandSideFromPart(missingPart.Part);
                if (handSide == null) continue;

                if (handSide == HandSide.Left && !leftAlreadyDisabled)
                {
                    OnHandDestroyed(HandSide.Left);
                    leftAlreadyDisabled = true;
                }
                else if (handSide == HandSide.Right && !rightAlreadyDisabled)
                {
                    OnHandDestroyed(HandSide.Right);
                    rightAlreadyDisabled = true;
                }

                // 两侧都已禁用，提前退出
                if (leftAlreadyDisabled && rightAlreadyDisabled) break;
            }
        }

        /// <summary>
        /// 通过部位的woundAnchorTag或LabelShort判断左右侧。
        /// 从被缺失的部位开始向父部位回溯，直到找到左右标识。
        /// </summary>
        private HandSide? GetHandSideFromPart(BodyPartRecord part)
        {
            var current = part;
            while (current != null)
            {
                // 优先使用 woundAnchorTag（原版标准方法）
                if (!string.IsNullOrEmpty(current.woundAnchorTag))
                {
                    if (current.woundAnchorTag.Contains("Left")) return HandSide.Left;
                    if (current.woundAnchorTag.Contains("Right")) return HandSide.Right;
                }

                // 降级：LabelShort（兼容没有标签的情况）
                string label = current.LabelShort?.ToLower();
                if (label != null)
                {
                    if (label.Contains("left") || label.Contains("左")) return HandSide.Left;
                    if (label.Contains("right") || label.Contains("右")) return HandSide.Right;
                }

                current = current.parent;
            }

            Log.Warning($"[BDP] 无法判断部位侧边: {part.LabelShort} (defName: {part.def.defName})");
            return null;
        }

        /// <summary>
        /// 处理手部破坏（实例方法）。
        /// </summary>
        private void OnHandDestroyed(HandSide side)
        {
            Log.Message($"[BDP] 检测到{(side == HandSide.Left ? "左手" : "右手")}被破坏，强制关闭对应槽位");

            SlotSide slotSide = side == HandSide.Left ? SlotSide.LeftHand : SlotSide.RightHand;
            string reason = side == HandSide.Left ? "左手缺失" : "右手缺失";
            ForceDeactivateSideSlots(slotSide, reason);
        }

        /// <summary>
        /// 强制禁用指定侧槽位（手部/手臂被毁时调用）。
        /// 芯片保留在槽位，但标记为禁用，激活的芯片被关闭。
        /// </summary>
        private void ForceDeactivateSideSlots(SlotSide side, string reason)
        {
            var slots = side == SlotSide.LeftHand ? leftHandSlots : rightHandSlots;
            if (slots == null) return;

            // 获取装备者（使用CompEquippable的Holder属性）
            Pawn pawn = Holder;
            if (pawn == null)
            {
                Log.Warning($"[BDP] ForceDeactivateSideSlots({side}): 无法获取装备者Pawn");
                return;
            }

            string sideLabel = side == SlotSide.LeftHand ? "左手" : "右手";
            foreach (var slot in slots)
            {
                // 关闭激活的芯片（但不清除loadedChip）
                if (slot.isActive)
                {
                    slot.isActive = false;
                    Log.Message($"[BDP] {sideLabel}槽位[{slot.index}]芯片关闭: {slot.loadedChip?.Label ?? "null"} 原因={reason}");
                }
                // 标记槽位为禁用
                slot.isDisabled = true;
            }

            // 清理该侧Verb
            SetSideVerbs(side, null, null);
            RebuildVerbs(pawn);
        }
    }
}
