namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 锚点瞄准模块配置
    /// 用于配置引导导弹的锚点散布参数
    /// </summary>
    public class AnchorAimConfig : ShotModuleConfig
    {
        /// <summary>
        /// 锚点散布半径（格）
        /// 每个锚点会在理想位置基础上添加随机偏移
        /// 默认值：1.0（与 GuidedConfig 保持一致）
        /// </summary>
        public float anchorSpread = 1.0f;

        /// <summary>
        /// 默认优先级：20
        /// 在 LosCheckModule(10) 之后执行
        /// </summary>
        public override int Priority => 20;
    }
}
