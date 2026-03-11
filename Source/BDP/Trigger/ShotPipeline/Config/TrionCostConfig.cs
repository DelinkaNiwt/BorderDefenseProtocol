namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// Trion 消耗模块配置
    /// 用于在 XML 中配置 TrionCostModule（未来扩展）
    /// </summary>
    public class TrionCostConfig : ShotModuleConfig
    {
        /// <summary>
        /// 消耗倍率（默认 1.0）
        /// 可用于全局调整 Trion 消耗量
        /// </summary>
        public float CostMultiplier { get; set; } = 1f;

        /// <summary>
        /// 是否跳过 Trion 消耗检查（默认 false）
        /// 用于测试或特殊场景
        /// </summary>
        public bool SkipConsumption { get; set; } = false;

        public override int Priority => 20;
    }
}
