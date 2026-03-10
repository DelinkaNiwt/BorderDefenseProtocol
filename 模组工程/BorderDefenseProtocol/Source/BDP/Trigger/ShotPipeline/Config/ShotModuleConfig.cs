namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击管线模块配置基类
    /// 所有模块配置类的共同祖先，用于类型安全的配置传递
    /// </summary>
    public abstract class ShotModuleConfig
    {
        /// <summary>
        /// 模块是否启用（默认启用）
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 执行优先级（升序，值小先执行）
        /// 子类可覆盖以调整执行顺序
        /// </summary>
        public virtual int Priority => 100;
    }
}
