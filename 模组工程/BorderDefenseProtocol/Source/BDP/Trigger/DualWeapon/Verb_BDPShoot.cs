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
    ///     → loadedChip.def.GetModExtension&lt;WeaponChipConfig&gt;().trionCostPerShot
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
            float cost = GetTrionCostPerShot(triggerComp);

            // Trion不足时中止射击
            if (cost > 0f)
            {
                var trion = pawn.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost)
                    return false;
            }

            // B3修复：使用芯片Thing作为equipment source
            Thing chipEquipment = GetCurrentChipThing(triggerComp);
            bool result = TryCastShotCore(chipEquipment);

            // 射击成功后消耗Trion
            if (result && cost > 0f)
            {
                var trion = pawn.GetComp<CompTrion>();
                trion?.Consume(cost);
            }

            return result;
        }

        /// <summary>
        /// 从当前激活的武器芯片读取trionCostPerShot。
        /// 优先通过侧别label精确定位，回退到ActivatingSlot，最终回退到遍历AllActiveSlots。
        /// </summary>
        private float GetTrionCostPerShot(CompTriggerBody triggerComp)
        {
            if (triggerComp == null) return 0f;

            // 优先通过侧别label精确定位（独立Gizmo场景）
            SlotSide? side = DualVerbCompositor.ParseSideLabel(verbProps?.label);
            if (side.HasValue)
            {
                var sideSlot = triggerComp.GetActiveSlot(side.Value);
                if (sideSlot?.loadedChip != null)
                {
                    var cfg = sideSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
                    if (cfg != null) return cfg.trionCostPerShot;
                }
            }

            // 回退：从ActivatingSlot读取
            var slot = triggerComp.ActivatingSlot;
            if (slot?.loadedChip != null)
            {
                var cfg = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg.trionCostPerShot;
            }

            // 最终回退：遍历所有激活槽位找第一个有WeaponChipConfig的
            foreach (var activeSlot in triggerComp.AllActiveSlots())
            {
                var cfg = activeSlot.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg.trionCostPerShot;
            }

            return 0f;
        }
    }
}
