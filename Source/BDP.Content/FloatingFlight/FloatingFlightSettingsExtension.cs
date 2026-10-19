using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空 Hediff 的视觉、过渡和未来资源消耗配置。
    /// </summary>
    public sealed class FloatingFlightSettingsExtension : DefModExtension
    {
        /// <summary>起飞或降落的线性过渡时长，单位为 tick。</summary>
        public int transitionTicks = 30;

        /// <summary>完整浮空时相对地面的视觉高度。</summary>
        public float presentationHeight = 0.6f;

        /// <summary>可选上下起伏振幅；首版默认为零。</summary>
        public float bobAmplitude = 0f;

        /// <summary>上下起伏的完整周期，单位为 tick。</summary>
        public int bobPeriodTicks = 120;

        /// <summary>未来启用时的每秒持续 Trion 消耗；首版为零。</summary>
        public float sustainTrionPerSecond = 0f;
    }
}
