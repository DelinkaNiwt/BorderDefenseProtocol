using UnityEngine;

namespace BDP.Content.Projectiles.BeamTrail
{
    /// <summary>
    /// 光束拖尾外观快照。
    /// 它把当前拖尾配置中与渲染直接相关的字段冻结到一次投射物运行时实例里，
    /// 避免运行时每次再回头读共享配置。
    /// </summary>
    internal sealed class BeamTrailAppearanceSnapshot
    {
        /// <summary>
        /// 拖尾贴图路径。
        /// </summary>
        public string TrailTexPath { get; private set; }

        /// <summary>
        /// 拖尾颜色。
        /// </summary>
        public Color TrailColor { get; private set; }

        /// <summary>
        /// 拖尾宽度。
        /// </summary>
        public float TrailWidth { get; private set; }

        /// <summary>
        /// 单段拖尾寿命。
        /// </summary>
        public int SegmentLifetimeTicks { get; private set; }

        /// <summary>
        /// 初始透明度。
        /// </summary>
        public float StartOpacity { get; private set; }

        /// <summary>
        /// 衰减时长比例。
        /// </summary>
        public float FadeRatio { get; private set; }

        /// <summary>
        /// 衰减曲线指数。
        /// </summary>
        public float FadeExponent { get; private set; }

        /// <summary>
        /// 拖尾额外高度偏移。
        /// </summary>
        public float AltitudeOffset { get; private set; }

        /// <summary>
        /// 首段锚点前推偏移。
        /// </summary>
        public float StartForwardOffset { get; private set; }

        /// <summary>
        /// 调试日志开关。
        /// </summary>
        public bool DebugLogging { get; private set; }

        /// <summary>
        /// 从拖尾预设创建一份外观快照。
        /// </summary>
        /// <param name="preset">当前拖尾预设。</param>
        /// <returns>已经做过安全回退的一份拖尾外观快照。</returns>
        public static BeamTrailAppearanceSnapshot CreateFrom(BeamTrailPresetDef preset)
        {
            return new BeamTrailAppearanceSnapshot
            {
                TrailTexPath = !string.IsNullOrWhiteSpace(preset != null ? preset.trailTexPath : null)
                    ? preset.trailTexPath
                    : "Things/Projectile/BDP_BeamTrail",
                TrailColor = preset != null ? preset.trailColor : Color.white,
                TrailWidth = Mathf.Max(0.01f, preset != null ? preset.trailWidth : 0.1105f),
                SegmentLifetimeTicks = Mathf.Max(1, preset != null ? preset.segmentLifetimeTicks : 30),
                StartOpacity = Mathf.Clamp01(preset != null ? preset.startOpacity : 1f),
                FadeRatio = Mathf.Max(0.01f, preset != null ? preset.fadeRatio : 0.8f),
                FadeExponent = Mathf.Max(0.1f, preset != null ? preset.fadeExponent : 10f),
                AltitudeOffset = preset != null ? preset.altitudeOffset : 0f,
                StartForwardOffset = Mathf.Max(0f, preset != null ? preset.startForwardOffset : 0f),
                DebugLogging = preset != null && preset.debugLogging
            };
        }
    }
}
