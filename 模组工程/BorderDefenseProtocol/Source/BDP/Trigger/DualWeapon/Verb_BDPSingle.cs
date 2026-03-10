using BDP.Core;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 单侧攻击Verb——v15.0合并逐发和齐射两种发射模式。
    /// 通过firingPattern字段区分发射模式：
    ///   · Sequential：逐发模式，引擎burst机制驱动，弹间有间隔
    ///   · Simultaneous：齐射模式，单次TryCastShot内循环瞬发所有子弹
    ///
    /// firingPattern由CompTriggerBody在创建Verb时从VerbChipConfig读取并设置。
    /// </summary>
    public class Verb_BDPSingle : Verb_BDPRangedBase
    {
        /// <summary>
        /// 发射模式（由CompTriggerBody在创建时从VerbChipConfig读取并设置）。
        /// </summary>
        internal FiringPattern firingPattern;

        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) return false;

            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return false;

            Thing chipThing = GetCurrentChipThing(triggerComp);
            if (chipThing == null) return false;

            // 调试日志：记录单侧攻击信息
            if (burstShotsLeft == verbProps.burstShotCount)  // 仅在burst开始时记录一次
            {
                var cfg = chipThing.def.GetModExtension<VerbChipConfig>();
                int actualBulletCount = cfg?.GetPrimaryBurstCount() ?? verbProps.burstShotCount;

                // 齐射模式：开枪1次，发射N颗子弹
                // 逐发模式：开枪N次，发射N颗子弹
                int shotCount = verbProps.burstShotCount;
                int bulletCount = (firingPattern == FiringPattern.Simultaneous) ? actualBulletCount : verbProps.burstShotCount;

                // v15.0：根据verbProps.isPrimary判断主/副攻击
                bool isSecondary = !verbProps.isPrimary;
                VerbAttackLogger.LogSingleAttack(
                    this,
                    chipThing.def.defName,
                    chipSide,
                    firingPattern,
                    shotCount,
                    bulletCount,
                    isSecondary
                );
            }

            if (firingPattern == FiringPattern.Simultaneous)
                return DoSimultaneousShot(pawn, triggerComp, chipThing);
            else
                return DoSequentialShot(pawn, triggerComp, chipThing);
        }

        /// <summary>
        /// 逐发模式：引擎burst机制驱动，每发子弹之间有间隔。
        /// </summary>
        private bool DoSequentialShot(Pawn pawn, CompTriggerBody triggerComp, Thing chipThing)
        {
            // 获取使用消耗（统一层）
            float cost = ChipUsageCostHelper.GetUsageCost(chipThing);

            // Trion不足时中止射击
            if (cost > 0f && !ChipUsageCostHelper.CanAffordUsage(pawn, chipThing))
            {
                return false;
            }

            // v9.0 FireMode：连射截断（burst 机制截断法）
            var fm = GetFireMode(chipThing);
            if (fm != null)
            {
                var cfg = chipThing?.def?.GetModExtension<VerbChipConfig>();
                if (cfg != null)
                {
                    int effective = fm.GetEffectiveBurst(cfg.GetPrimaryBurstCount());
                    int fired = verbProps.burstShotCount - burstShotsLeft;
                    if (fired >= effective) { burstShotsLeft = 0; return false; }
                }
            }

            bool result = TryCastShotCore(chipThing);

            // 射击成功后消耗Trion（统一层）
            if (result && cost > 0f)
            {
                ChipUsageCostHelper.ConsumeUsageCost(pawn, chipThing);
            }

            return result;
        }

        /// <summary>
        /// 齐射模式：单次TryCastShot内循环瞬发所有子弹，无间隔。
        /// </summary>
        private bool DoSimultaneousShot(Pawn pawn, CompTriggerBody triggerComp, Thing chipThing)
        {
            var cfg = chipThing.def.GetModExtension<VerbChipConfig>();
            if (cfg == null) return false;

            // 从芯片配置读取实际发射数
            int volleyCount = cfg.GetPrimaryBurstCount();
            // v9.0 FireMode：连射数注入
            var fm = GetFireMode(chipThing);
            if (fm != null) volleyCount = fm.GetEffectiveBurst(volleyCount);

            // 预检Trion消耗
            if (!ChipUsageCostHelper.CanAffordUsage(pawn, chipThing))
                return false;

            // 循环发射所有子弹（每颗独立命中判定）
            float spread = cfg.ranged?.volleySpreadRadius ?? 0f;

            // ★ 自动绕行：齐射前计算路由（条件2由Verb类型隐含满足）
            gs.PrepareAutoRoute(caster.Position, currentTarget.Cell,
                caster.Map, cfg.GetPrimaryProjectileDef());

            bool anyHit = FireVolleyLoop(volleyCount, spread, chipThing);

            // 一次性扣除Trion（统一层）- 每次射击动作消耗
            if (anyHit)
                ChipUsageCostHelper.ConsumeUsageCost(pawn, chipThing);

            return anyHit;
        }
    }
}
