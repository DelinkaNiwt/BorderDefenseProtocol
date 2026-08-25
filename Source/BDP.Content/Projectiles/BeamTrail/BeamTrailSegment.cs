using UnityEngine;
using Verse;

namespace BDP.Content.Projectiles.BeamTrail
{
    /// <summary>
    /// 单条光束拖尾线段。
    /// 它是地图组件真正持有并进入存档的对象。
    /// </summary>
    [StaticConstructorOnStartup]
    internal sealed class BeamTrailSegment : IExposable
    {
        /// <summary>
        /// 每帧写入颜色时复用的材质属性块。
        /// 这里用静态块避免重复分配。
        /// </summary>
        private static readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        /// <summary>
        /// 当前线段起点。
        /// </summary>
        public Vector3 Start;

        /// <summary>
        /// 当前线段终点。
        /// </summary>
        public Vector3 End;

        /// <summary>
        /// 当前线段贴图路径。
        /// </summary>
        public string TrailTexPath;

        /// <summary>
        /// 当前线段颜色。
        /// </summary>
        public Color TrailColor;

        /// <summary>
        /// 当前线段是否追加拖尾内芯。
        /// </summary>
        public bool HasTrailCore;

        /// <summary>
        /// 当前线段内芯颜色。
        /// </summary>
        public Color TrailCoreColor;

        /// <summary>
        /// 当前线段内芯相对外层的宽度比例。
        /// </summary>
        public float TrailCoreWidthRatio = 0.45f;

        /// <summary>
        /// 当前线段内芯透明度倍率。
        /// </summary>
        public float TrailCoreOpacity = 1f;

        /// <summary>
        /// 当前线段宽度。
        /// </summary>
        public float TrailWidth;

        /// <summary>
        /// 当前线段寿命。
        /// </summary>
        public int SegmentLifetimeTicks;

        /// <summary>
        /// 当前线段已存活时长。
        /// </summary>
        public int TicksAlive;

        /// <summary>
        /// 当前线段初始透明度。
        /// </summary>
        public float StartOpacity;

        /// <summary>
        /// 当前线段衰减时长比例。
        /// </summary>
        public float FadeRatio;

        /// <summary>
        /// 当前线段衰减曲线指数。
        /// </summary>
        public float FadeExponent;

        /// <summary>
        /// 当前线段额外高度偏移。
        /// </summary>
        public float AltitudeOffset;

        /// <summary>
        /// 用一份外观快照重置当前线段。
        /// </summary>
        /// <param name="start">当前线段起点。</param>
        /// <param name="end">当前线段终点。</param>
        /// <param name="appearance">当前线段使用的外观快照。</param>
        public void Reset(Vector3 start, Vector3 end, BeamTrailAppearanceSnapshot appearance)
        {
            Start = NormalizePoint(start);
            End = NormalizePoint(end);
            TrailTexPath = appearance != null ? appearance.TrailTexPath : "Things/Projectile/BDP_BeamTrail";
            TrailColor = appearance != null ? appearance.TrailColor : Color.white;
            HasTrailCore = appearance != null && appearance.HasTrailCore;
            TrailCoreColor = appearance != null ? appearance.TrailCoreColor : Color.black;
            TrailCoreWidthRatio = appearance != null ? appearance.TrailCoreWidthRatio : 0.45f;
            TrailCoreOpacity = appearance != null ? appearance.TrailCoreOpacity : 1f;
            TrailWidth = appearance != null ? appearance.TrailWidth : 0.1105f;
            SegmentLifetimeTicks = appearance != null ? appearance.SegmentLifetimeTicks : 30;
            TicksAlive = 0;
            StartOpacity = appearance != null ? appearance.StartOpacity : 1f;
            FadeRatio = appearance != null ? appearance.FadeRatio : 0.8f;
            FadeExponent = appearance != null ? appearance.FadeExponent : 10f;
            AltitudeOffset = appearance != null ? appearance.AltitudeOffset : 0f;
        }

        /// <summary>
        /// 推进当前线段寿命。
        /// </summary>
        /// <returns>为真表示当前线段仍可保留；为假表示当前线段应被回收。</returns>
        public bool Tick()
        {
            TicksAlive++;
            return TicksAlive < Mathf.Max(1, SegmentLifetimeTicks);
        }

        /// <summary>
        /// 解析当前线段应显示的透明度。
        /// </summary>
        /// <returns>当前线段的有效透明度。</returns>
        public float ResolveOpacity()
        {
            float safeStartOpacity = Mathf.Clamp01(StartOpacity);
            float effectiveLifetime = Mathf.Max(1f, Mathf.Max(1, SegmentLifetimeTicks) * Mathf.Max(0.01f, FadeRatio));
            float progress = Mathf.Clamp01(TicksAlive / effectiveLifetime);
            return safeStartOpacity * (1f - Mathf.Pow(progress, Mathf.Max(0.1f, FadeExponent)));
        }

        /// <summary>
        /// 用给定材质绘制当前线段。
        /// </summary>
        /// <param name="material">当前线段使用的材质。</param>
        public void Draw(Material material)
        {
            DrawInternal(material, TrailWidth, TrailColor, 1f);
        }

        /// <summary>
        /// 用独立材质绘制当前线段的灰黑内芯。
        /// </summary>
        /// <param name="material">内芯使用的透明材质。</param>
        public void DrawCore(Material material)
        {
            if (!HasTrailCore)
            {
                return;
            }

            DrawInternal(
                material,
                TrailWidth * Mathf.Clamp(TrailCoreWidthRatio, 0.05f, 1f),
                TrailCoreColor,
                Mathf.Clamp01(TrailCoreOpacity));
        }

        /// <summary>
        /// 使用指定外观参数完成一次线段绘制。
        /// </summary>
        /// <param name="material">当前绘制使用的材质。</param>
        /// <param name="width">当前绘制宽度。</param>
        /// <param name="color">当前绘制颜色。</param>
        /// <param name="opacityMultiplier">当前绘制透明度倍率。</param>
        private void DrawInternal(Material material, float width, Color color, float opacityMultiplier)
        {
            if (material == null)
            {
                return;
            }

            float opacity = ResolveOpacity();
            if (opacity <= 0.0001f)
            {
                return;
            }

            Vector3 direction = End - Start;
            float length = direction.MagnitudeHorizontal();
            if (length <= 0.0001f)
            {
                return;
            }

            Vector3 midpoint = (Start + End) * 0.5f;
            midpoint.y = AltitudeLayer.MoteOverhead.AltitudeFor() + AltitudeOffset;

            Quaternion rotation = Quaternion.LookRotation(direction.Yto0());
            Vector3 scale = new Vector3(Mathf.Max(0.01f, width), 1f, length);
            Matrix4x4 matrix = Matrix4x4.TRS(midpoint, rotation, scale);

            Color finalColor = new Color(color.r, color.g, color.b, color.a * opacity * opacityMultiplier);
            propertyBlock.Clear();
            propertyBlock.SetColor("_Color", finalColor);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0, null, 0, propertyBlock);
        }

        /// <summary>
        /// 存读档当前线段状态。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref Start, "start");
            Scribe_Values.Look(ref End, "end");
            Scribe_Values.Look(ref TrailTexPath, "trailTexPath");
            Scribe_Values.Look(ref TrailColor, "trailColor");
            Scribe_Values.Look(ref HasTrailCore, "hasTrailCore", false);
            Scribe_Values.Look(ref TrailCoreColor, "trailCoreColor", Color.black);
            Scribe_Values.Look(ref TrailCoreWidthRatio, "trailCoreWidthRatio", 0.45f);
            Scribe_Values.Look(ref TrailCoreOpacity, "trailCoreOpacity", 1f);
            Scribe_Values.Look(ref TrailWidth, "trailWidth", 0.1105f);
            Scribe_Values.Look(ref SegmentLifetimeTicks, "segmentLifetimeTicks", 30);
            Scribe_Values.Look(ref TicksAlive, "ticksAlive", 0);
            Scribe_Values.Look(ref StartOpacity, "startOpacity", 1f);
            Scribe_Values.Look(ref FadeRatio, "fadeRatio", 0.8f);
            Scribe_Values.Look(ref FadeExponent, "fadeExponent", 10f);
            Scribe_Values.Look(ref AltitudeOffset, "altitudeOffset", 0f);
        }

        /// <summary>
        /// 把当前线段端点压回地图平面。
        /// </summary>
        /// <param name="point">待归一的坐标点。</param>
        /// <returns>归一后的平面坐标点。</returns>
        private static Vector3 NormalizePoint(Vector3 point)
        {
            point.y = 0f;
            return point;
        }
    }
}
