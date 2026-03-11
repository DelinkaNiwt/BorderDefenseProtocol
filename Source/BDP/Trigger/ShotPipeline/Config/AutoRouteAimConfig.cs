namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 自动绕行瞄准模块配置
    /// 控制自动绕行的启用条件和参数
    /// </summary>
    public class AutoRouteAimConfig
    {
        /// <summary>
        /// 是否启用自动绕行
        /// 默认 true（当弹药支持引导飞行时自动启用）
        /// </summary>
        public bool enabled = true;

        /// <summary>
        /// 自动绕行的优先级
        /// 默认 30（高于 LosCheckModule 的 10）
        /// </summary>
        public int priority = 30;
    }
}
