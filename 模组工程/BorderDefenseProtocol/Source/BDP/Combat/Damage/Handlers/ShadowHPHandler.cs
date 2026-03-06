using BDP.Core;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 影子HP处理器。
    /// 应用伤害到影子HP系统。
    ///
    /// 职责：
    /// - 应用伤害到影子HP：ShadowHPTracker.TakeDamage(part, damage)
    /// - 返回部位是否被破坏的标志
    /// - 不负责触发部位破坏（由CombatBodyDamageHandler统一协调）
    /// </summary>
    public static class ShadowHPHandler
    {
        /// <summary>
        /// 处理影子HP伤害。
        /// </summary>
        /// <param name="ctx">Pipeline共享上下文</param>
        /// <param name="part">受伤部位</param>
        /// <param name="damage">伤害量</param>
        /// <param name="partDestroyed">输出参数：部位是否被破坏</param>
        /// <returns>true=处理成功，false=处理失败</returns>
        public static bool Handle(CombatBodyContext ctx, BodyPartRecord part, float damage, out bool partDestroyed)
        {
            partDestroyed = false;

            if (ctx.ShadowHP == null)
            {
                Log.Error($"[BDP] ShadowHPHandler: {ctx.Pawn.LabelShort} 缺少ShadowHPTracker");
                return false;
            }

            float hpBefore = ctx.ShadowHP.GetHP(part);

            // 已破坏的部位不再受伤
            if (hpBefore <= 0f)
            {
                Log.Message($"[BDP]   ShadowHP [{part.def.defName}]: 已破坏，跳过伤害");
                partDestroyed = true;
                return true;
            }

            ctx.ShadowHP.TakeDamage(part, damage);
            float hpAfter = ctx.ShadowHP.GetHP(part);

            if (hpAfter <= 0f)
            {
                Log.Message($"[BDP]   ShadowHP [{part.def.defName}]: {hpBefore:F1} → {hpAfter:F1} ★破坏★");
                partDestroyed = true;
            }
            else
            {
                Log.Message($"[BDP]   ShadowHP [{part.def.defName}]: {hpBefore:F1} → {hpAfter:F1}");
            }

            return true;
        }
    }
}
