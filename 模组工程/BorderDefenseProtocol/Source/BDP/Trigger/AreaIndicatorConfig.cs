using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 范围指示器配置类，用于定义武器/能力的影响范围显示。
    /// 可作为 ModExtension 附加到投射物 Def、芯片 Def 或 Verb Def 上。
    /// </summary>
    public class AreaIndicatorConfig : DefModExtension
    {
        /// <summary>指示器类型（圆形、扇形、弧形、矩形）。</summary>
        public AreaIndicatorType indicatorType = AreaIndicatorType.Circle;

        /// <summary>半径来源（从爆炸配置读取或使用自定义值）。</summary>
        public RadiusSource radiusSource = RadiusSource.Explosion;

        /// <summary>自定义半径（当 radiusSource 为 Custom 时使用）。</summary>
        public float customRadius = 3.0f;

        /// <summary>指示器颜色（RGBA，默认为半透明红色）。</summary>
        public Color color = new Color(1.0f, 0.3f, 0.3f, 0.35f);

        /// <summary>填充样式（轮廓或填充）。</summary>
        public FillStyle fillStyle = FillStyle.Filled;
    }

    /// <summary>范围指示器类型枚举。</summary>
    public enum AreaIndicatorType
    {
        /// <summary>圆形范围。</summary>
        Circle,

        /// <summary>扇形范围（未实现）。</summary>
        Sector,

        /// <summary>弧形范围（未实现）。</summary>
        Arc,

        /// <summary>矩形范围（未实现）。</summary>
        Rectangle
    }

    /// <summary>半径来源枚举。</summary>
    public enum RadiusSource
    {
        /// <summary>从投射物的爆炸配置读取半径。</summary>
        Explosion,

        /// <summary>使用自定义半径值。</summary>
        Custom
    }

    /// <summary>填充样式枚举。</summary>
    public enum FillStyle
    {
        /// <summary>仅绘制轮廓（未实现）。</summary>
        Outline,

        /// <summary>填充整个范围。</summary>
        Filled
    }
}
