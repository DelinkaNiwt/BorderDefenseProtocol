using BDP.Core;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双侧齐射Verb——在单次TryCastShot()中发射两侧所有子弹（v6.1新增）。
    /// 继承Verb_BDPRangedBase，复用OrderForceTarget（BDP_ChipRangedAttack job）。
    ///
    /// 与Verb_BDPDualRanged的区别：
    ///   · Verb_BDPDualRanged：引擎burst机制交替逐发射击
    ///   · Verb_BDPDualVolley：burstShotCount=1，TryCastShot内循环发射两侧所有子弹
    ///
    /// 发射数来源：从两侧芯片的WeaponChipConfig.verbProperties[0].burstShotCount读取。
    /// 子弹类型：临时替换verbProps.defaultProjectile为对应侧弹药（try-finally安全恢复）。
    /// Trion消耗：预检两侧总消耗，不够直接中止。
    ///
    /// 数据获取路径：同Verb_BDPDualRanged（从CompTriggerBody读取两侧芯片）。
    /// </summary>
    public class Verb_BDPDualVolley : Verb_BDPRangedBase
    {
        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) return false;

            var triggerComp = pawn.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return false;

            // 读取两侧芯片数据
            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null)
                return false;

            var leftCfg = leftSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (leftCfg == null || rightCfg == null) return false;

            int leftCount = GetBurstCount(leftCfg);
            int rightCount = GetBurstCount(rightCfg);
            ThingDef leftProj = GetProjectileDef(leftCfg);
            ThingDef rightProj = GetProjectileDef(rightCfg);

            // 预检两侧Trion总消耗
            float totalCost = leftCount * leftCfg.trionCostPerShot
                            + rightCount * rightCfg.trionCostPerShot;
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost))
                return false;

            bool anyHit = false;
            ThingDef originalProjectile = verbProps.defaultProjectile;
            // 取两侧中较大的散布半径
            float spread = Mathf.Max(leftCfg.volleySpreadRadius, rightCfg.volleySpreadRadius);
            try
            {
                // 发射左侧所有子弹
                if (leftProj != null)
                    verbProps.defaultProjectile = leftProj;
                for (int i = 0; i < leftCount; i++)
                {
                    if (spread > 0f)
                        shotOriginOffset = new Vector3(
                            Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                    if (TryCastShotCore(leftSlot.loadedChip))
                        anyHit = true;
                }

                // 发射右侧所有子弹
                if (rightProj != null)
                    verbProps.defaultProjectile = rightProj;
                for (int i = 0; i < rightCount; i++)
                {
                    if (spread > 0f)
                        shotOriginOffset = new Vector3(
                            Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                    if (TryCastShotCore(rightSlot.loadedChip))
                        anyHit = true;
                }
            }
            finally
            {
                // 恢复原始projectileDef和偏移量
                verbProps.defaultProjectile = originalProjectile;
                shotOriginOffset = Vector3.zero;
            }

            // 一次性扣除Trion
            if (anyHit && totalCost > 0f)
                trion?.Consume(totalCost);

            return anyHit;
        }

        /// <summary>从WeaponChipConfig读取burstShotCount。</summary>
        private static int GetBurstCount(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties == null) return 1;
            foreach (var vp in cfg.verbProperties)
            {
                if (vp.burstShotCount > 0) return vp.burstShotCount;
            }
            return 1;
        }

        /// <summary>从WeaponChipConfig读取defaultProjectile。</summary>
        private static ThingDef GetProjectileDef(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties == null) return null;
            foreach (var vp in cfg.verbProperties)
            {
                if (vp.defaultProjectile != null) return vp.defaultProjectile;
            }
            return null;
        }
    }
}
