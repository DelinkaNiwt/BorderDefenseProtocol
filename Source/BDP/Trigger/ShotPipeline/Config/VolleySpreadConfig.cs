namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 齐射散布模块配置
    /// 用于配置齐射模式下的弹丸散布半径
    /// </summary>
    public class VolleySpreadConfig : ShotModuleConfig
    {
        /// <summary>
        /// 齐射散布半径（单位：格）
        /// 弹丸会在目标点周围此半径内随机散布
        /// 默认值：0（无散布）
        /// </summary>
        public float spreadRadius = 0f;

        /// <summary>
        /// 默认优先级：10
        /// 在射击阶段最早执行，注入散布参数
        /// </summary>
        public override int Priority => 10;
    }
}
