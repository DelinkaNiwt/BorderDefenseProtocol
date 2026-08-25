using UnityEngine;

namespace BDP.Core.Projectiles.Visual
{
    /// <summary>
    /// 投射物视觉外观覆盖。
    /// Core 只承载中性的可选值，不认识任何具体视觉内容类型。
    /// </summary>
    public sealed class ProjectileVisualAppearanceOverrides
    {
        /// <summary>
        /// 当前是否显式覆盖拖尾颜色。
        /// </summary>
        public bool HasTrailColor { get; private set; }

        /// <summary>
        /// 当前显式覆盖的拖尾颜色。
        /// </summary>
        public Color TrailColor { get; private set; }

        /// <summary>
        /// 当前是否追加拖尾内芯。
        /// </summary>
        public bool HasTrailCore { get; private set; }

        /// <summary>
        /// 当前拖尾内芯颜色。
        /// </summary>
        public Color TrailCoreColor { get; private set; }

        /// <summary>
        /// 当前拖尾内芯相对外层的宽度比例。
        /// </summary>
        public float TrailCoreWidthRatio { get; private set; }

        /// <summary>
        /// 当前拖尾内芯透明度倍率。
        /// </summary>
        public float TrailCoreOpacity { get; private set; }

        /// <summary>
        /// 创建一份投射物视觉外观覆盖。
        /// </summary>
        /// <param name="hasTrailColor">是否启用拖尾颜色覆盖。</param>
        /// <param name="trailColor">拖尾颜色覆盖值。</param>
        public ProjectileVisualAppearanceOverrides(bool hasTrailColor, Color trailColor)
            : this(hasTrailColor, trailColor, false, Color.black, 0.45f, 1f)
        {
        }

        /// <summary>
        /// 创建一份包含拖尾内芯参数的投射物视觉外观覆盖。
        /// </summary>
        /// <param name="hasTrailColor">是否启用拖尾颜色覆盖。</param>
        /// <param name="trailColor">拖尾颜色覆盖值。</param>
        /// <param name="hasTrailCore">是否追加拖尾内芯。</param>
        /// <param name="trailCoreColor">拖尾内芯颜色。</param>
        /// <param name="trailCoreWidthRatio">拖尾内芯相对外层的宽度比例。</param>
        /// <param name="trailCoreOpacity">拖尾内芯透明度倍率。</param>
        public ProjectileVisualAppearanceOverrides(
            bool hasTrailColor,
            Color trailColor,
            bool hasTrailCore,
            Color trailCoreColor,
            float trailCoreWidthRatio,
            float trailCoreOpacity)
        {
            HasTrailColor = hasTrailColor;
            TrailColor = trailColor;
            HasTrailCore = hasTrailCore;
            TrailCoreColor = trailCoreColor;
            TrailCoreWidthRatio = trailCoreWidthRatio;
            TrailCoreOpacity = trailCoreOpacity;
        }
    }
}
