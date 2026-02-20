using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 远程交替连射Verb（v2.0 §8.6）——交替子弹类型，叠加连射数。
    /// 继承Verb_Shoot，重写burst相关逻辑。
    ///
    /// 连射数合成：总burstShotCount = 左连射数 + 右连射数
    /// 子弹交替规则：L, R, L, R... 直到一方用完，剩余补齐
    /// 射程：min(左, 右)  瞄准/冷却：max(左, 右)
    ///
    /// 数据获取路径：同Verb_BDPDualMelee
    /// </summary>
    public class Verb_BDPDualRanged : Verb_Shoot
    {
        // 当前burst中的发射序号（用于交替子弹类型）
        private int dualBurstIndex = 0;

        // 两侧的剩余发射数（每次burst开始时重置）
        private int leftRemaining = 0;
        private int rightRemaining = 0;

        /// <summary>
        /// 重写TryCastShot：根据当前burst序号选择对应侧的子弹类型。
        /// 交替规则：偶数发用左侧，奇数发用右侧，一方用完后全部用另一方。
        /// </summary>
        protected override bool TryCastShot()
        {
            // 首发时初始化交替状态
            if (dualBurstIndex == 0)
                InitDualBurst();

            // 确定当前发使用哪一侧
            // 当前简化实现：直接调用base.TryCastShot()
            // TODO: 根据侧别切换projectileDef（需要在每发射击前修改verbProps.defaultProjectile）
            bool result = base.TryCastShot();

            dualBurstIndex++;

            // burst结束时重置
            if (burstShotsLeft <= 0)
                dualBurstIndex = 0;

            return result;
        }

        /// <summary>初始化双射burst状态：从CompTriggerBody读取两侧连射数。</summary>
        private void InitDualBurst()
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null)
            {
                leftRemaining = verbProps.burstShotCount;
                rightRemaining = 0;
                return;
            }

            // 从两侧激活芯片的VerbProperties读取burstShotCount
            var leftSlot = triggerComp.GetActiveSlot(SlotSide.Left);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.Right);

            leftRemaining = GetBurstCount(leftSlot);
            rightRemaining = GetBurstCount(rightSlot);
        }

        /// <summary>从芯片的ThingDef.Verbs读取burstShotCount，默认1。</summary>
        private static int GetBurstCount(ChipSlot slot)
        {
            if (slot?.loadedChip?.def?.Verbs == null) return 0;
            foreach (var vp in slot.loadedChip.def.Verbs)
            {
                if (vp.burstShotCount > 0) return vp.burstShotCount;
            }
            return 1;
        }

        /// <summary>
        /// 确定当前发应使用哪一侧。
        /// 交替规则：偶数发左侧，奇数发右侧，一方用完后全部用另一方。
        /// </summary>
        private SlotSide GetCurrentShotSide()
        {
            if (leftRemaining > 0 && rightRemaining > 0)
            {
                // 交替：偶数发左侧，奇数发右侧
                if (dualBurstIndex % 2 == 0)
                {
                    leftRemaining--;
                    return SlotSide.Left;
                }
                else
                {
                    rightRemaining--;
                    return SlotSide.Right;
                }
            }
            else if (leftRemaining > 0)
            {
                leftRemaining--;
                return SlotSide.Left;
            }
            else
            {
                rightRemaining--;
                return SlotSide.Right;
            }
        }
    }
}
