using UnityEngine;
using Verse;

namespace BDP.Glow
{
    /// <summary>
    /// 发光效果组件的XML配置类。
    ///
    /// XML用法示例：
    ///   &lt;li Class="BDP.Glow.CompProperties_BDPGlow"&gt;
    ///     &lt;graphicData&gt;
    ///       &lt;texPath&gt;Things/Item/Chip/EnergyCore_Glow&lt;/texPath&gt;
    ///       &lt;shaderType&gt;MoteGlow&lt;/shaderType&gt;
    ///       &lt;drawSize&gt;1.0&lt;/drawSize&gt;
    ///       &lt;color&gt;(0.3, 0.8, 1.0)&lt;/color&gt;
    ///     &lt;/graphicData&gt;
    ///     &lt;controllerClass&gt;BDP.Glow.Controllers.StaticGlowController&lt;/controllerClass&gt;
    ///     &lt;controllerParams Class="BDP.Glow.StaticGlowParams"&gt;
    ///       &lt;intensity&gt;0.9&lt;/intensity&gt;
    ///     &lt;/controllerParams&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class CompProperties_BDPGlow : CompProperties
    {
        // ── 必填配置 ──

        /// <summary>发光层贴图配置（使用RimWorld标准GraphicData）。</summary>
        public GraphicData graphicData;

        /// <summary>控制器类全名（支持反射创建），默认使用StaticGlowController。</summary>
        public string controllerClass = "BDP.Glow.Controllers.StaticGlowController";

        /// <summary>控制器参数对象（XML中用Class属性指定具体子类）。</summary>
        public GlowControllerParams controllerParams;

        // ── 可选配置 ──

        /// <summary>绘制偏移（相对于Thing中心），默认零向量。</summary>
        public Vector3 drawOffset = Vector3.zero;

        /// <summary>高度层级，默认MoteOverhead。</summary>
        public AltitudeLayer altitudeLayer = AltitudeLayer.MoteOverhead;

        /// <summary>最大可见距离（格），超过此距离不绘制。0=不限制。</summary>
        public float maxDrawDistance = 0f;

        /// <summary>强度阈值，低于此值不绘制（性能优化）。</summary>
        public float minIntensityThreshold = 0.01f;

        /// <summary>Tick间隔（0=每tick更新，>0=每N tick更新一次）。</summary>
        public int tickInterval = 0;

        public CompProperties_BDPGlow()
        {
            compClass = typeof(CompGlow);
        }
    }
}
