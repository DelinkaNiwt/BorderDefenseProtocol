using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 近战双击Verb（v2.0 §8.5）——一次TryCastShot中执行左右双侧连击。
    /// 继承Verb_MeleeAttackDamage，重写TryCastShot()。
    ///
    /// 数据获取路径：
    ///   this.caster (Pawn) → pawn.equipment.Primary (触发体)
    ///     → CompTriggerBody → leftActiveTools / rightActiveTools
    ///
    /// 连击数来源：芯片XML配置 meleeBurstCount（通过DefModExtension）
    /// 冷却时间：max(左侧cooldownTime, 右侧cooldownTime)，由Compositor设置
    /// </summary>
    public class Verb_BDPDualMelee : Verb_MeleeAttackDamage
    {
        /// <summary>
        /// 重写TryCastShot：执行左侧连击 + 右侧连击。
        /// 每次伤害产生独立的战斗日志条目，视觉上表现为快速连击。
        /// </summary>
        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) return false;

            var triggerComp = pawn.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null)
                return base.TryCastShot(); // 回退到普通近战

            var leftTools = GetSideTools(triggerComp, SlotSide.Left);
            var rightTools = GetSideTools(triggerComp, SlotSide.Right);

            bool anyHit = false;

            // 阶段1：左侧连击
            if (leftTools != null && leftTools.Count > 0)
            {
                int leftBurst = GetMeleeBurstCount(triggerComp, SlotSide.Left);
                for (int i = 0; i < leftBurst; i++)
                {
                    // 使用base.TryCastShot()执行单次近战攻击
                    // 原版Verb_MeleeAttackDamage.TryCastShot()会处理命中/闪避/伤害
                    if (base.TryCastShot()) anyHit = true;
                }
            }

            // 阶段2：右侧连击（无停顿衔接）
            if (rightTools != null && rightTools.Count > 0)
            {
                int rightBurst = GetMeleeBurstCount(triggerComp, SlotSide.Right);
                for (int i = 0; i < rightBurst; i++)
                {
                    if (base.TryCastShot()) anyHit = true;
                }
            }

            return anyHit;
        }

        /// <summary>获取指定侧的Tool列表（从CompTriggerBody的激活芯片读取）。</summary>
        private static List<Tool> GetSideTools(CompTriggerBody triggerComp, SlotSide side)
        {
            // 通过反射或内部访问获取按侧Tool数据
            // 当前简化实现：从激活槽位的芯片ThingDef读取tools
            var activeSlot = triggerComp.GetActiveSlot(side);
            return activeSlot?.loadedChip?.def?.tools;
        }

        /// <summary>
        /// 获取指定侧芯片的近战连击数。
        /// 从芯片的DefModExtension中读取meleeBurstCount，默认1。
        /// </summary>
        private static int GetMeleeBurstCount(CompTriggerBody triggerComp, SlotSide side)
        {
            var activeSlot = triggerComp.GetActiveSlot(side);
            if (activeSlot?.loadedChip == null) return 1;

            var ext = activeSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            return ext?.meleeBurstCount ?? 1;
        }
    }

    /// <summary>武器芯片的DefModExtension配置（近战连击数等）。</summary>
    public class WeaponChipConfig : DefModExtension
    {
        /// <summary>近战连击数（默认1=单次攻击）。</summary>
        public int meleeBurstCount = 1;
    }
}
