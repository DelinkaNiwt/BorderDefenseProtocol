using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 到达决策数据包——ticksToImpact≤0时决定继续飞还是命中。
    /// 模块可设置Continue=true来阻止Impact（如引导飞行重定向到下一锚点）。
    /// </summary>
    public struct ArrivalContext
    {
        // ── 输入（只读）──
        /// <summary>当前命中目标（可能为null）。</summary>
        public readonly Thing HitTarget;

        // ── 可修改 ──
        /// <summary>是否继续飞行（true=不执行Impact，模块已重定向）。</summary>
        public bool Continue;

        public ArrivalContext(Thing hitTarget)
        {
            HitTarget = hitTarget;
            Continue = false;
        }
    }

    /// <summary>
    /// 到达决策管线接口——决定继续飞还是命中。
    /// 用于引导飞行（到达中间锚点时重定向）、追踪、穿透、分裂、延迟引爆等。
    /// 执行顺序：ticksToImpact≤0时，在ImpactHandler之前。
    /// </summary>
    public interface IBDPArrivalHandler
    {
        /// <summary>处理到达决策。设置ctx.Continue=true表示继续飞行。</summary>
        void HandleArrival(Bullet_BDP host, ref ArrivalContext ctx);
    }
}
