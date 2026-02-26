using BDP.Core;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 单侧齐射Verb——在单次TryCastShot()中发射所有子弹（v6.1新增）。
    /// 继承Verb_BDPRangedBase，复用OrderForceTarget（BDP_ChipRangedAttack job）。
    ///
    /// 与Verb_BDPShoot的区别：
    ///   · Verb_BDPShoot：引擎burst机制逐发射击（每隔N tick一颗）
    ///   · Verb_BDPVolley：burstShotCount=1，TryCastShot内循环发射所有子弹
    ///
    /// 发射数来源：从芯片的WeaponChipConfig.verbProperties[0].burstShotCount读取。
    /// Trion消耗：预检总消耗（volleyCount × trionCostPerShot），不够直接中止。
    ///
    /// 数据获取路径：同Verb_BDPShoot（通过侧别label定位芯片）。
    /// </summary>
    public class Verb_BDPVolley : Verb_BDPRangedBase
    {
        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) return false;

            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return false;

            // 通过侧别label定位芯片
            Thing chipThing = GetCurrentChipThing(triggerComp);
            if (chipThing == null) return false;

            var cfg = chipThing.def.GetModExtension<WeaponChipConfig>();
            if (cfg == null) return false;

            // 从芯片配置读取实际发射数
            int volleyCount = GetBurstCountFromConfig(cfg);
            float costPerShot = cfg.trionCostPerShot;

            // 预检Trion总消耗
            float totalCost = volleyCount * costPerShot;
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost))
                return false;

            // 循环发射所有子弹（每颗独立命中判定）
            bool anyHit = false;
            float spread = cfg.volleySpreadRadius;
            for (int i = 0; i < volleyCount; i++)
            {
                // 视觉偏移：每发子弹射出起点随机偏移
                if (spread > 0f)
                    shotOriginOffset = new Vector3(
                        Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));

                if (TryCastShotCore(chipThing))
                    anyHit = true;
            }
            shotOriginOffset = Vector3.zero;

            // 一次性扣除Trion
            if (anyHit && totalCost > 0f)
                trion?.Consume(totalCost);

            return anyHit;
        }

        /// <summary>从WeaponChipConfig读取burstShotCount（实际齐射发射数）。</summary>
        private static int GetBurstCountFromConfig(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties == null) return 1;
            foreach (var vp in cfg.verbProperties)
            {
                if (vp.burstShotCount > 0) return vp.burstShotCount;
            }
            return 1;
        }
    }
}
