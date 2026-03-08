using BDP.Core;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 单发远程Verb——射击前检查并消耗Trion（v4.0 B3远程修复）。
    /// 继承Verb_BDPRangedBase，调用TryCastShotCore(chipThing)使战斗日志显示芯片名。
    ///
    /// 数据获取路径：
    ///   CasterPawn → equipment.Primary → CompTriggerBody.ActivatingSlot
    ///     → loadedChip.def.GetModExtension&lt;VerbChipConfig&gt;().trionCostPerShot
    ///
    /// 与Verb_BDPDualRanged的区别：
    ///   · Verb_BDPShoot用于单侧射击（XML中直接配置verbClass=Verb_BDPShoot）
    ///   · Verb_BDPDualRanged用于双侧交替连射（由DualVerbCompositor合成）
    /// </summary>
    public class Verb_BDPShoot : Verb_BDPRangedBase
    {
        /// <summary>
        /// 重写TryCastShot：射击前从激活武器芯片读取trionCostPerShot，
        /// Trion不足时中止射击，成功后Consume。
        /// B3修复：调用TryCastShotCore传入芯片Thing，使战斗日志显示芯片名。
        /// </summary>
        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) return false;

            var triggerComp = GetTriggerComp();
            float cost = GetChipConfig()?.cost?.trionPerShot ?? 0f;

            // Trion不足时中止射击
            if (cost > 0f)
            {
                var trion = pawn.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost)
                    return false;
            }

            // B3修复：使用芯片Thing作为equipment source
            Thing chipEquipment = GetCurrentChipThing(triggerComp);

            // v9.0 FireMode：连射截断（burst 机制截断法）
            var fm = GetFireMode(chipEquipment);
            if (fm != null)
            {
                var cfg = chipEquipment?.def?.GetModExtension<VerbChipConfig>();
                if (cfg != null)
                {
                    int effective = fm.GetEffectiveBurst(cfg.GetPrimaryBurstCount());
                    int fired = verbProps.burstShotCount - burstShotsLeft;
                    if (fired >= effective) { burstShotsLeft = 0; return false; }
                }
            }

            bool result = TryCastShotCore(chipEquipment);

            // 射击成功后消耗Trion
            if (result && cost > 0f)
            {
                var trion = pawn.GetComp<CompTrion>();
                trion?.Consume(cost);
            }

            return result;
        }


    }
}
