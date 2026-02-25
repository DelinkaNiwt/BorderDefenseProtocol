using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 命中效果数据包——Impact时依次执行所有命中效果。
    /// 与旧架构的first-handler-wins不同，所有模块依次执行。
    /// </summary>
    public struct ImpactContext
    {
        // ── 输入（只读）──
        /// <summary>命中目标（可能为null=命中地面）。</summary>
        public readonly Thing HitThing;

        // ── 可修改 ──
        /// <summary>是否已有模块处理了Impact（true=跳过base.Impact）。</summary>
        public bool Handled;

        public ImpactContext(Thing hitThing)
        {
            HitThing = hitThing;
            Handled = false;
        }
    }

    /// <summary>
    /// 命中效果管线接口——依次执行命中效果（爆炸/分裂/伤害修饰）。
    /// 所有实现此接口的模块都会被调用（不再是first-handler-wins）。
    /// 执行顺序：ArrivalHandler之后（当ctx.Continue==false时）。
    /// </summary>
    public interface IBDPImpactHandler
    {
        /// <summary>处理命中效果。设置ctx.Handled=true表示已替代默认Impact。</summary>
        void HandleImpact(Bullet_BDP host, ref ImpactContext ctx);
    }
}
