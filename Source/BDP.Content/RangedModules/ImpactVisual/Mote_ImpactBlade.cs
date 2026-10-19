using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.ImpactVisual
{
    /// <summary>
    /// 命中用三角刀刃 Mote。
    /// 直接绘制三角网格，避免 Fleck 矩形平面在缩放和倾斜时造成悬空或宽板。
    /// </summary>
    public sealed class Mote_ImpactBlade : Mote
    {
        /// <summary>
        /// 未经过动态阵列配置时使用的默认刀刃长度。
        /// </summary>
        private const float DefaultBladeLength = 1.5f;

        /// <summary>
        /// 未经过动态阵列配置时使用的默认底边宽度。
        /// </summary>
        private const float DefaultBladeBaseWidth = 0.40f;

        /// <summary>
        /// 刀刃从接地底边长到完整长度所需的 Tick。
        /// </summary>
        private const float BladeGrowthTicks = 1f;

        /// <summary>
        /// 动态阵列传入的最小可见底边宽度。
        /// </summary>
        private const float MinimumBladeWidth = 0.28f;

        /// <summary>
        /// 角度输入的安全上限，防止错误 Def 产生无限大的尖端偏移。
        /// </summary>
        private const float MaximumLeanAngle = 50f;

        /// <summary>
        /// 网格缓存比较阈值。
        /// </summary>
        private const float MeshEpsilon = 0.0005f;

        /// <summary>
        /// 当前刀刃的世界底边宽度。
        /// </summary>
        private float configuredBladeWidth = DefaultBladeBaseWidth;

        /// <summary>
        /// 当前刀刃的世界完整长度。
        /// </summary>
        private float configuredBladeLength = DefaultBladeLength;

        /// <summary>
        /// 当前刀刃从生成到开始刺出的延迟 Tick。
        /// </summary>
        public int StartDelayTicks { get; private set; }

        /// <summary>
        /// 当前刀刃网格；每个短命 Mote 只缓存一份。
        /// </summary>
        private Mesh bladeMesh;

        /// <summary>
        /// 当前网格缓存的宽度。
        /// </summary>
        private float cachedWidth = -1f;

        /// <summary>
        /// 当前网格缓存的长度。
        /// </summary>
        private float cachedLength = -1f;

        /// <summary>
        /// 当前网格缓存的尖端偏移。
        /// </summary>
        private float cachedApexOffset = float.NaN;

        /// <summary>
        /// 当前刀刃的材质属性块，避免每帧创建材质。
        /// </summary>
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        /// <summary>
        /// 配置动态刀刃的尺寸和错峰起刺时间。
        /// </summary>
        /// <param name="bladeWidth">水平底边世界宽度。</param>
        /// <param name="bladeLength">完整刀刃世界长度。</param>
        /// <param name="startDelayTicks">从生成到开始刺出的 Tick。</param>
        public void Configure(
            float bladeWidth,
            float bladeLength,
            int startDelayTicks)
        {
            configuredBladeWidth = Mathf.Max(MinimumBladeWidth, bladeWidth);
            configuredBladeLength = Mathf.Max(0.1f, bladeLength);
            StartDelayTicks = Mathf.Max(0, startDelayTicks);
        }

        /// <summary>
        /// 绘制刀刃三角网格和绿色材质。
        /// </summary>
        /// <param name="drawLoc">RimWorld 传入的绘制位置。</param>
        /// <param name="flip">原版绘制翻转标记；刀刃固定朝屏幕上方，因此不使用。</param>
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (paused || (Find.UIRoot != null && Find.UIRoot.HideMotes))
            {
                return;
            }

            if (def == null || def.graphic == null || def.graphic.MatSingle == null)
            {
                return;
            }

            float alpha = Mathf.Clamp01(Alpha);
            if (alpha <= 0.0001f)
            {
                return;
            }

            float ageTicks = AgeSecsPausable * 60f;
            if (ageTicks < StartDelayTicks)
            {
                return;
            }

            float lengthScale = Mathf.Clamp01(
                (ageTicks - StartDelayTicks) / BladeGrowthTicks);
            if (lengthScale <= 0.0001f)
            {
                return;
            }

            float width = configuredBladeWidth;
            float length = configuredBladeLength * lengthScale;
            float angle = Mathf.Clamp(exactRotation, -MaximumLeanAngle, MaximumLeanAngle);
            float apexOffset = Mathf.Tan(angle * Mathf.Deg2Rad) * length;

            EnsureBladeMesh(width, length, apexOffset);

            Vector3 position = exactPosition;
            position.y = def.altitudeLayer.AltitudeFor() + yOffset;

            Color color = new Color(0.45f, 1f, 0.55f, alpha);
            color.r *= instanceColor.r;
            color.g *= instanceColor.g;
            color.b *= instanceColor.b;
            color.a *= instanceColor.a;
            propertyBlock.Clear();
            propertyBlock.SetColor(ShaderPropertyIDs.Color, color);

            Graphics.DrawMesh(
                bladeMesh,
                Matrix4x4.TRS(position, Quaternion.identity, Vector3.one),
                def.graphic.MatSingle,
                0,
                null,
                0,
                propertyBlock);
        }

        /// <summary>
        /// 按当前尺寸和尖端偏移重建三角网格。
        /// 底边位于局部 Z=0，尖端位于局部 Z 正方向，因此底边永远水平接地。
        /// </summary>
        /// <param name="width">当前底边宽度。</param>
        /// <param name="length">当前刀刃长度。</param>
        /// <param name="apexOffset">尖端相对底边中心的水平偏移。</param>
        private void EnsureBladeMesh(float width, float length, float apexOffset)
        {
            if (bladeMesh != null
                && Mathf.Abs(cachedWidth - width) <= MeshEpsilon
                && Mathf.Abs(cachedLength - length) <= MeshEpsilon
                && Mathf.Abs(cachedApexOffset - apexOffset) <= MeshEpsilon)
            {
                return;
            }

            if (bladeMesh == null)
            {
                bladeMesh = new Mesh
                {
                    name = "BDP_ImpactBladeTriangle"
                };
            }
            else
            {
                bladeMesh.Clear();
            }

            float halfWidth = width * 0.5f;
            bladeMesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, 0f),
                new Vector3(apexOffset, 0f, length),
                new Vector3(halfWidth, 0f, 0f)
            };
            bladeMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0.5f, 1f),
                new Vector2(1f, 0f)
            };
            bladeMesh.triangles = new[] { 0, 1, 2 };
            bladeMesh.RecalculateNormals();
            bladeMesh.RecalculateBounds();

            cachedWidth = width;
            cachedLength = length;
            cachedApexOffset = apexOffset;
        }

        /// <summary>
        /// 销毁 Mote 时释放本次短命特效专用的三角网格。
        /// </summary>
        /// <param name="mode">原版销毁模式。</param>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (bladeMesh != null)
            {
                Object.Destroy(bladeMesh);
                bladeMesh = null;
            }

            base.Destroy(mode);
        }
    }
}
