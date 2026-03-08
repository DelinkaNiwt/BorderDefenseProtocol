using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 光束拖尾配置——挂在投射物ThingDef的modExtensions上。
    /// 不挂此扩展 → 无拖尾（回退原版Bullet行为）。
    /// 挂了但enabled=false → 也无拖尾，方便调试开关。
    /// </summary>
    public class BeamTrailConfig : DefModExtension
    {
        /// <summary>总开关，方便调试时关闭拖尾。</summary>
        public bool enabled = true;

        /// <summary>拖尾宽度（世界单位）。</summary>
        public float trailWidth = 0.15f;

        /// <summary>拖尾颜色（与贴图叠加，alpha控制整体透明度）。</summary>
        public Color trailColor = Color.white;

        /// <summary>拖尾贴图路径（水平渐变：左透明→右明亮）。</summary>
        public string trailTexPath = "Things/Projectile/BDP_BeamTrail";

        /// <summary>每段线段存活tick数（决定拖尾可见时长，越大拖尾越长）。</summary>
        public int segmentDuration = 8;

        /// <summary>初始不透明度（0~1，线段创建时的起始alpha）。</summary>
        public float startOpacity = 0.9f;

        /// <summary>
        /// 衰减时间比例（相对于segmentDuration的比例）。
        /// 1.0=在整个生命周期内匀速衰减（默认），0.8=在80%时间内衰减完毕（更快消失），
        /// 0=立刻衰减（无拖尾）。
        /// </summary>
        public float decayTime = 1.0f;

        /// <summary>
        /// 衰减锐度（幂次）。控制衰减曲线形状：
        /// 1.0=线性衰减（烟雾感），3.0=大部分时间保持高亮然后急速截断（光束感）。
        /// 值越大，光束"硬边"效果越明显。
        /// </summary>
        public float decaySharpness = 1.0f;
    }
}
