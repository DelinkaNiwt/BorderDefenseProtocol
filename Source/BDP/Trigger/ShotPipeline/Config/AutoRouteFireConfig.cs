namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 自动绕行射击模块配置
    /// 控制自动绕行路径附加到弹道的行为
    /// </summary>
    public class AutoRouteFireConfig
    {
        /// <summary>
        /// 是否启用自动绕行射击
        /// 默认 true（当 AimResult 包含自动绕行路径时自动附加）
        /// </summary>
        public bool enabled = true;

        /// <summary>
        /// 自动绕行射击的优先级
        /// 默认 40（在 FlightDataModule 之后）
        /// </summary>
        public int priority = 40;
    }
}
