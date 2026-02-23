using BDP.Core;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 远程交替连射Verb（v2.0 §8.6，v4.0 B3远程修复）——交替子弹类型，叠加连射数。
    /// 继承Verb_BDPRangedBase，调用TryCastShotCore(chipThing)使战斗日志显示芯片名。
    ///
    /// 连射数合成：总burstShotCount = 左连射数 + 右连射数
    /// 子弹交替规则：L, R, L, R... 直到一方用完，剩余补齐
    /// 射程：min(左, 右)  瞄准/冷却：max(左, 右)
    ///
    /// 数据获取路径：同Verb_BDPMelee
    /// </summary>
    public class Verb_BDPDualRanged : Verb_BDPRangedBase
    {
        // 当前burst中的发射序号（用于交替子弹类型）
        private int dualBurstIndex = 0;

        // 两侧的剩余发射数（每次burst开始时重置）
        private int leftRemaining = 0;
        private int rightRemaining = 0;

        // B4修复：缓存两侧的projectileDef，用于per-shot子弹类型切换
        private ThingDef leftProjectileDef;
        private ThingDef rightProjectileDef;

        /// <summary>
        /// 重写TryCastShot：根据当前burst序号选择对应侧的子弹类型。
        /// 交替规则：偶数发用左侧，奇数发用右侧，一方用完后全部用另一方。
        /// 每发射击前按侧读取trionCostPerShot并Consume。
        /// B3修复：调用TryCastShotCore传入当前侧芯片Thing，使战斗日志显示芯片名。
        /// </summary>
        protected override bool TryCastShot()
        {
            // 首发时初始化交替状态
            if (dualBurstIndex == 0)
                InitDualBurst();

            // 确定当前发使用哪一侧，并消耗Trion
            SlotSide shotSide = GetCurrentShotSide();
            float cost = GetSideTrionCost(shotSide);
            if (cost > 0f)
            {
                var trion = CasterPawn?.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost)
                    return false; // Trion不足，中止射击
                trion.Consume(cost);
            }

            // B3修复：使用当前侧芯片Thing作为equipment source
            Thing chipEquipment = GetSideChipThing(shotSide);

            // B4修复：根据侧别切换projectileDef，射击前临时替换verbProps.defaultProjectile
            // Bug5修复：用try-finally确保异常时也能恢复originalProjectile
            ThingDef originalProjectile = verbProps.defaultProjectile;
            ThingDef sideProjectile = shotSide == SlotSide.LeftHand ? leftProjectileDef : rightProjectileDef;
            bool result;
            try
            {
                if (sideProjectile != null)
                    verbProps.defaultProjectile = sideProjectile;

                result = TryCastShotCore(chipEquipment);
            }
            finally
            {
                // 恢复原始projectileDef（避免污染后续逻辑）
                verbProps.defaultProjectile = originalProjectile;
            }

            dualBurstIndex++;

            // burst结束时重置（burstShotsLeft在base class中于TryCastShot返回后才递减，
            // 所以最后一发时burstShotsLeft=1而非0，用<=1判断）
            if (burstShotsLeft <= 1)
                dualBurstIndex = 0;

            return result;
        }

        /// <summary>初始化双射burst状态：从CompTriggerBody读取两侧连射数和projectileDef。</summary>
        private void InitDualBurst()
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null)
            {
                leftRemaining = verbProps.burstShotCount;
                rightRemaining = 0;
                leftProjectileDef = null;
                rightProjectileDef = null;
                return;
            }

            // 从两侧激活芯片的VerbProperties读取burstShotCount
            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);

            leftRemaining = GetBurstCount(leftSlot);
            rightRemaining = GetBurstCount(rightSlot);

            // B4修复：读取两侧的projectileDef
            leftProjectileDef = GetProjectileDef(leftSlot);
            rightProjectileDef = GetProjectileDef(rightSlot);
        }

        /// <summary>
        /// 从芯片的WeaponChipConfig.verbProperties读取burstShotCount（T36数据源切换）。
        /// 原因：武器数据不能放ThingDef.Verbs，否则IsWeapon=true。
        /// </summary>
        private static int GetBurstCount(ChipSlot slot)
        {
            if (slot?.loadedChip == null) return 0;
            var cfg = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (cfg?.verbProperties == null) return 0;
            foreach (var vp in cfg.verbProperties)
            {
                if (vp.burstShotCount > 0) return vp.burstShotCount;
            }
            return 1;
        }

        /// <summary>
        /// B4修复：从芯片的WeaponChipConfig.verbProperties读取defaultProjectile。
        /// 每侧芯片可能有不同的子弹类型，交替射击时需要切换。
        /// </summary>
        private static ThingDef GetProjectileDef(ChipSlot slot)
        {
            if (slot?.loadedChip == null) return null;
            var cfg = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (cfg?.verbProperties == null) return null;
            foreach (var vp in cfg.verbProperties)
            {
                if (vp.defaultProjectile != null) return vp.defaultProjectile;
            }
            return null;
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
                    return SlotSide.LeftHand;
                }
                else
                {
                    rightRemaining--;
                    return SlotSide.RightHand;
                }
            }
            else if (leftRemaining > 0)
            {
                leftRemaining--;
                return SlotSide.LeftHand;
            }
            else
            {
                rightRemaining--;
                return SlotSide.RightHand;
            }
        }

        /// <summary>
        /// 获取指定侧芯片的trionCostPerShot。
        /// 从WeaponChipConfig读取（T36数据源）。
        /// </summary>
        private float GetSideTrionCost(SlotSide side)
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return 0f;

            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) return 0f;

            var cfg = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            return cfg?.trionCostPerShot ?? 0f;
        }

        /// <summary>获取指定侧芯片的Thing实例（B3：供TryCastShotCore使用）。</summary>
        private Thing GetSideChipThing(SlotSide side)
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            return triggerComp?.GetActiveSlot(side)?.loadedChip;
        }
    }
}
