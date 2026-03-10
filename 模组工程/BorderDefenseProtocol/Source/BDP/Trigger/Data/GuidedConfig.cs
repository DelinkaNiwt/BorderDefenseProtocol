using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 引导飞行配置（变化弹机制）。
    /// 定义子弹的引导飞行参数，包括锚点数量和散布。
    /// </summary>
    public class GuidedConfig
    {
        /// <summary>
        /// 最大锚点数量（不含最终目标）。
        /// 锚点是子弹飞行路径上的中间点，子弹会依次飞向这些锚点。
        /// 例如：maxAnchors=3时，飞行路径为 起点 → 锚点1 → 锚点2 → 锚点3 → 目标
        /// </summary>
        public int maxAnchors = 3;

        /// <summary>
        /// 锚点散布基础半径（格）。
        /// 每个锚点会在理想位置基础上添加随机偏移，偏移量按递增系数计算：
        /// actualAnchor[i] = idealAnchor[i] + Random.insideUnitCircle * anchorSpread * (i / totalAnchors)
        ///
        /// 效果：
        /// - 第一段偏移最小（接近直线）
        /// - 最后一段偏移最大（更多曲线）
        /// - 齐射时每颗子弹独立计算，形成不同的飞行轨迹
        ///
        /// 推荐值：
        /// - 0.1-0.2 = 轻微曲线（精确制导）
        /// - 0.3-0.5 = 明显曲线（标准变化弹）
        /// - 0.6+ = 剧烈曲线（高机动规避）
        /// </summary>
        public float anchorSpread = 0.3f;
    }
}
