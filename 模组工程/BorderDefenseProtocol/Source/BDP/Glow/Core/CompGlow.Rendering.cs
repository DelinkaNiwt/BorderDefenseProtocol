using UnityEngine;
using Verse;

namespace BDP.Glow
{
    /// <summary>
    /// CompGlow渲染逻辑（partial class）——负责PostDraw中的发光层绘制。
    ///
    /// 渲染技术参考 HediffComp_BDPShield.DrawShieldBubble()：
    ///   · Matrix4x4.TRS 构建变换矩阵
    ///   · MaterialPropertyBlock 动态设置颜色（避免创建新Material）
    ///   · Graphics.DrawMesh 直接渲染
    /// </summary>
    public partial class CompGlow : ThingComp
    {
        // ── 静态资源缓存（避免每帧分配） ──

        /// <summary>MaterialPropertyBlock静态实例——复用以避免GC。</summary>
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        // ─────────────────────────────────────────────────────────────────────
        // 渲染入口
        // ─────────────────────────────────────────────────────────────────────

        public override void PostDraw()
        {
            if (initFailed || controller == null || glowGraphic == null) return;

            // 获取当前强度
            float intensity = controller.GetGlowIntensity();

            // 强度阈值剔除（低于阈值不绘制，节省DrawCall）
            if (intensity < Props.minIntensityThreshold) return;

            // 距离剔除（超过maxDrawDistance不绘制）
            if (!ShouldDrawAtCurrentDistance()) return;

            DrawGlowLayer(intensity);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 渲染实现
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 绘制发光层。
        /// 参考 HediffComp_BDPShield.DrawShieldBubble() 的渲染模式。
        /// </summary>
        private void DrawGlowLayer(float intensity)
        {
            // 计算绘制位置
            Vector3 drawPos = parent.DrawPos + Props.drawOffset;
            drawPos.y = Props.altitudeLayer.AltitudeFor();

            // 获取发光颜色（控制器可覆盖，否则使用graphicData配置的颜色）
            Color baseColor = glowGraphic.color;
            Color? overrideColor = controller.GetGlowColor();
            Color finalColor = overrideColor ?? baseColor;

            // 叠加强度到Alpha通道
            finalColor.a *= intensity;

            // 构建变换矩阵（位置、无旋转、贴图尺寸）
            Vector2 drawSize = Props.graphicData.drawSize;
            Vector3 scale = new Vector3(drawSize.x, 1f, drawSize.y);
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity, scale);

            // 设置动态颜色（复用静态propBlock，避免创建新Material）
            propBlock.SetColor(ShaderPropertyIDs.Color, finalColor);

            // 绘制发光层
            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                glowGraphic.MatSingle,
                0,      // layer
                null,   // camera（null=当前相机）
                0,      // submeshIndex
                propBlock
            );
        }

        // ─────────────────────────────────────────────────────────────────────
        // 距离剔除
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 检查当前是否应该绘制（距离剔除）。
        /// maxDrawDistance=0时始终绘制。
        /// </summary>
        private bool ShouldDrawAtCurrentDistance()
        {
            if (Props.maxDrawDistance <= 0f) return true;

            // 使用距离平方比较，避免开方运算
            float maxDistSq = Props.maxDrawDistance * Props.maxDrawDistance;
            Vector3 cameraPos = Find.CameraDriver.MapPosition.ToVector3();
            float distSq = (parent.DrawPos - cameraPos).sqrMagnitude;

            return distSq <= maxDistSq;
        }
    }
}
