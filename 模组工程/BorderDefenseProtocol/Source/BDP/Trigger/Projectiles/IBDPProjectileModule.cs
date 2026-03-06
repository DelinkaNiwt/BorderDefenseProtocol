using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP投射物模块基础接口——定义模块生命周期基础钩子。
    /// 模块通过DefModExtension配置自动挂载到Bullet_BDP宿主上。
    ///
    /// 优先级约定：数值越小越先执行。
    ///   10 = 路径修改（GuidedModule）
    ///   50 = 伤害效果（ExplosionModule）
    ///   100 = 视觉效果（TrailModule）
    ///
    /// 管线架构v5：
    ///   基础接口只保留Priority/OnSpawn/ExposeData。
    ///   具体行为通过管线接口组合实现：
    ///     IBDPLifecyclePolicy / IBDPFlightIntentProvider / IBDPVisualObserver
    ///     IBDPArrivalPolicy / IBDPHitResolver / IBDPPositionModifier / IBDPSpeedModifier
    ///   模块只产出意图，宿主统一执行。Phase是模块间唯一协作媒介。
    /// </summary>
    public interface IBDPProjectileModule : IExposable
    {
        /// <summary>执行优先级（越小越先执行）。</summary>
        int Priority { get; }

        /// <summary>SpawnSetup时调用，初始化模块状态。</summary>
        void OnSpawn(Bullet_BDP host);
    }
}
