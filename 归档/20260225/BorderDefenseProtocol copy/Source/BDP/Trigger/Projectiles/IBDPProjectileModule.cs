using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP投射物模块接口——定义模块生命周期钩子。
    /// 模块通过DefModExtension配置自动挂载到Bullet_BDP宿主上。
    ///
    /// 优先级约定：数值越小越先执行。
    ///   10 = 路径修改（GuidedModule）
    ///   50 = 伤害效果（ExplosionModule）
    ///   100 = 视觉效果（TrailModule）
    ///
    /// OnImpact/OnPreImpact返回true表示"已处理，宿主跳过后续逻辑"。
    /// </summary>
    public interface IBDPProjectileModule : IExposable
    {
        /// <summary>执行优先级（越小越先执行）。</summary>
        int Priority { get; }

        /// <summary>SpawnSetup时调用，初始化模块状态。</summary>
        void OnSpawn(Bullet_BDP host);

        /// <summary>每tick调用（在base.TickInterval之后）。</summary>
        void OnTick(Bullet_BDP host);

        /// <summary>
        /// ImpactSomething前调用。返回true=拦截（宿主不执行base.ImpactSomething）。
        /// 用于引导飞行：到达中间锚点时重定向而非Impact。
        /// </summary>
        bool OnPreImpact(Bullet_BDP host);

        /// <summary>
        /// Impact时调用（first-handler-wins）。返回true=已处理Impact（宿主跳过base.Impact）。
        /// 用于爆炸模块：替代默认的单体伤害为范围爆炸。
        /// </summary>
        bool OnImpact(Bullet_BDP host, Thing hitThing);

        /// <summary>Impact后调用（所有模块都会执行）。用于清理或后处理。</summary>
        void OnPostImpact(Bullet_BDP host, Thing hitThing);
    }
}
