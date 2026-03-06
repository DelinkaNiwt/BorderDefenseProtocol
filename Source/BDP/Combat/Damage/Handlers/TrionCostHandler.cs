using BDP.Core;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// Trion消耗处理器。
    /// 计算并扣除战斗体受伤时的Trion消耗。
    ///
    /// 职责：
    /// - 计算Trion消耗：cost = damage * trionCostPerDamage
    /// - 调用CompTrion.Consume(cost)
    /// - 如果Trion不足，返回false触发破裂
    /// </summary>
    public static class TrionCostHandler
    {
        // TODO: 从XML配置读取，临时硬编码
        private const float TRION_COST_PER_DAMAGE = 0.5f;

        /// <summary>
        /// 处理Trion消耗。
        /// </summary>
        /// <param name="ctx">Pipeline共享上下文</param>
        /// <param name="damage">伤害量</param>
        /// <returns>true=消耗成功，false=Trion不足（触发破裂）</returns>
        public static bool Handle(CombatBodyContext ctx, float damage)
        {
            if (ctx.CompTrion == null)
            {
                Log.Error($"[BDP] TrionCostHandler: {ctx.Pawn.LabelShort} 缺少CompTrion");
                return false;
            }

            float cost = damage * TRION_COST_PER_DAMAGE;
            bool success = ctx.CompTrion.Consume(cost);

            if (!success)
            {
                Log.Message($"[BDP] TrionCostHandler: {ctx.Pawn.LabelShort} Trion不足，触发破裂");
            }

            return success;
        }
    }
}
