using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 独立拖尾线段对象——每tick由投射物创建一段(prev→current)。
    /// 由BDPEffectMapComponent统一管理生命周期和渲染。
    /// 投射物销毁后，已创建的线段自然渐隐消亡。
    /// 支持对象池复用，避免频繁GC。
    /// </summary>
    [StaticConstructorOnStartup]
    public class BDPTrailSegment
    {
        /// <summary>线段起点（上一tick位置）。</summary>
        public Vector3 origin;

        /// <summary>线段终点（当前tick位置）。</summary>
        public Vector3 destination;

        private Material material;
        private Color baseColor;
        private float width;
        private int duration;
        private float startOpacity;
        private float decayTime;
        private float decaySharpness;
        private int ticksAlive;

        /// <summary>当前不透明度（随时间衰减→0）。</summary>
        public float Opacity { get; private set; }

        /// <summary>静态MaterialPropertyBlock，避免每帧分配。</summary>
        private static readonly MaterialPropertyBlock propBlock
            = new MaterialPropertyBlock();

        /// <summary>
        /// 重置所有字段，供对象池复用。
        /// 调用后等同于新创建的实例。
        /// </summary>
        public void Reset(
            Vector3 origin, Vector3 destination,
            Material material, Color baseColor,
            float width, int duration, float startOpacity,
            float decayTime, float decaySharpness)
        {
            this.origin = origin;
            this.destination = destination;
            this.material = material;
            this.baseColor = baseColor;
            this.width = width;
            this.duration = duration;
            this.startOpacity = startOpacity;
            this.decayTime = decayTime;
            this.decaySharpness = decaySharpness;
            this.ticksAlive = 0;
            this.Opacity = startOpacity;
        }

        /// <summary>
        /// 每tick调用。递增存活计数，计算衰减后的不透明度。
        /// </summary>
        /// <returns>true=仍存活，false=已过期应移除。</returns>
        public bool Tick()
        {
            ticksAlive++;
            if (ticksAlive >= duration) return false;

            // decayTime控制衰减时间比例，decaySharpness控制曲线形状
            float effectiveDuration = duration * decayTime;
            float progress = effectiveDuration <= 0f
                ? 1f
                : Mathf.Min(1f, (float)ticksAlive / effectiveDuration);
            float shapedProgress = Mathf.Pow(progress, decaySharpness);
            Opacity = startOpacity * (1f - shapedProgress);
            return true;
        }

        /// <summary>
        /// 渲染此线段。用Matrix4x4.TRS + MaterialPropertyBlock实现带颜色的渐隐。
        /// </summary>
        public void Draw()
        {
            if (Opacity <= 0f) return;

            Vector3 look = destination - origin;
            float length = look.MagnitudeHorizontal();
            if (length < 0.01f) return;

            Vector3 midpoint = origin + look / 2f;
            midpoint.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Quaternion rot = Quaternion.LookRotation(look.Yto0());
            Vector3 scale = new Vector3(width, 1f, length);
            Matrix4x4 matrix = Matrix4x4.TRS(midpoint, rot, scale);

            Color finalColor = new Color(baseColor.r, baseColor.g, baseColor.b, Opacity);
            propBlock.SetColor("_Color", finalColor);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0, null, 0, propBlock);
        }
    }
}
