using System.Linq;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体伤口处理器。
    /// 负责：
    /// 1. 处理战斗体伤口（调用WoundAdapter）
    /// 2. 清理所有战斗体伤口（战斗体解除时）
    /// </summary>
    public static class WoundHandler
    {
        /// <summary>
        /// 处理战斗体伤口。
        /// 在Handler链中被调用，位于ShadowHPHandler之后，CollapseHandler之前。
        /// </summary>
        /// <param name="pawn">受伤Pawn</param>
        /// <param name="part">受伤部位</param>
        /// <param name="damageDef">伤害类型</param>
        /// <param name="damage">伤害值</param>
        /// <param name="dinfo">伤害信息（用于记录武器信息）</param>
        public static void Handle(Pawn pawn, BodyPartRecord part, DamageDef damageDef, float damage, DamageInfo dinfo)
        {
            if (pawn == null || part == null || damageDef == null)
            {
                Log.Warning("[BDP] WoundHandler.Handle: 参数为null");
                return;
            }

            // 计算伤口severity（基于伤害值）
            // 这里使用简单的线性映射：damage / 10 = severity
            // 例如：10点伤害 = 1.0 severity
            float severity = damage / 10f;

            // 添加或合并伤口，传递DamageInfo以记录武器信息
            WoundAdapter.AddOrMergeWound(pawn, part, damageDef, severity, dinfo);
        }

        /// <summary>
        /// 清理所有战斗体伤口。
        /// 在战斗体解除时调用。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        public static void Clear(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Warning("[BDP] WoundHandler.Clear: pawn为null");
                return;
            }

            // 查找所有战斗体伤口
            var combatWounds = pawn.health.hediffSet.hediffs
                .OfType<Hediff_CombatWound>()
                .ToList();

            if (combatWounds.Count == 0)
            {
                return;
            }

            // 移除所有战斗体伤口
            foreach (var wound in combatWounds)
            {
                pawn.health.RemoveHediff(wound);
            }

            Log.Message($"[BDP] 清理战斗体伤口: {pawn.LabelShort}, 移除{combatWounds.Count}个伤口");
        }
    }
}
