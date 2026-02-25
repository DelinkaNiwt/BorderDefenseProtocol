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
    /// 管线架构v4：
    ///   基础接口只保留Priority/OnSpawn/ExposeData。
    ///   具体行为通过管线接口（IBDPTickObserver/IBDPPathResolver/...）组合实现。
    ///   模块按需实现关心的管线接口，新增阶段不影响现有模块。
    /// </summary>
    public interface IBDPProjectileModule : IExposable
    {
        /// <summary>执行优先级（越小越先执行）。</summary>
        int Priority { get; }

        /// <summary>SpawnSetup时调用，初始化模块状态。</summary>
        void OnSpawn(Bullet_BDP host);
    }
}
