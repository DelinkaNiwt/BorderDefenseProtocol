using UnityEngine;
using Verse;

namespace BDP.Content.Projectiles.BeamTrail
{
    /// <summary>
    /// 光束拖尾预设 Def。
    /// 它只保存可复用外观参数，不保存任何运行时线段状态。
    /// </summary>
    public sealed class BeamTrailPresetDef : Def
    {
        /// <summary>拖尾贴图路径。</summary>
        public string trailTexPath = "Things/Projectile/BDP_BeamTrail";

        /// <summary>拖尾颜色。</summary>
        public Color trailColor = new Color(0.7f, 0.85f, 1f, 1f);

        /// <summary>拖尾宽度。</summary>
        public float trailWidth = 0.1105f;

        /// <summary>单段拖尾寿命，单位为 tick。</summary>
        public int segmentLifetimeTicks = 30;

        /// <summary>线段刚创建时的初始透明度。</summary>
        public float startOpacity = 1f;

        /// <summary>衰减时长比例。</summary>
        public float fadeRatio = 0.8f;

        /// <summary>衰减曲线指数。</summary>
        public float fadeExponent = 10f;

        /// <summary>拖尾额外高度偏移。</summary>
        public float altitudeOffset = 0f;

        /// <summary>首段锚点前推偏移。</summary>
        public float startForwardOffset = 0f;

        /// <summary>是否输出调试日志。</summary>
        public bool debugLogging = false;
    }
}
