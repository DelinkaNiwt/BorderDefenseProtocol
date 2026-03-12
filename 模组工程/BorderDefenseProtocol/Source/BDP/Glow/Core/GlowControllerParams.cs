using Verse;

namespace BDP.Glow
{
    /// <summary>
    /// 发光控制器参数基类——XML配置的数据容器。
    ///
    /// 子类通过继承此类添加特定参数，并在XML中用Class属性指定具体类型：
    ///   &lt;controllerParams Class="BDP.Glow.StaticGlowParams"&gt;
    ///     &lt;intensity&gt;0.9&lt;/intensity&gt;
    ///   &lt;/controllerParams&gt;
    /// </summary>
    public abstract class GlowControllerParams
    {
        // 基类无公共字段，子类按需扩展
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 静态发光参数
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 静态发光控制器参数——固定强度，无动画。
    /// </summary>
    public class StaticGlowParams : GlowControllerParams
    {
        /// <summary>发光强度，范围[0, 1]，默认1.0（最大亮度）。</summary>
        public float intensity = 1.0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 脉冲动画参数
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 脉冲发光控制器参数——周期性强度变化（呼吸灯效果）。
    /// </summary>
    public class PulseGlowParams : GlowControllerParams
    {
        /// <summary>最小强度，范围[0, 1]，默认0.3。</summary>
        public float minIntensity = 0.3f;

        /// <summary>最大强度，范围[0, 1]，默认1.0。</summary>
        public float maxIntensity = 1.0f;

        /// <summary>动画周期（ticks），默认180（约3秒）。</summary>
        public int period = 180;

        /// <summary>动画曲线名称：Linear / SmoothInOut / Sine，默认Sine。</summary>
        public string curve = "Sine";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 淡入淡出参数
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 淡入淡出控制器参数——三段式状态机（FadeIn → Sustain → FadeOut）。
    /// </summary>
    public class FadeGlowParams : GlowControllerParams
    {
        /// <summary>淡入持续时间（ticks），默认60。</summary>
        public int fadeInTicks = 60;

        /// <summary>持续发光时间（ticks），默认120。-1表示永久持续。</summary>
        public int sustainTicks = 120;

        /// <summary>淡出持续时间（ticks），默认60。</summary>
        public int fadeOutTicks = 60;

        /// <summary>是否循环播放，默认false。</summary>
        public bool loop = false;
    }
}
