using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.ImpactVisual
{
    /// <summary>
    /// 命中动态刀刃阵列子特效器。
    /// 根据目标当前身体图形计算数量与覆盖范围，再生成少量独立三角刀刃 Mote。
    /// </summary>
    public sealed class SubEffecter_ImpactBladeBurst : SubEffecter
    {
        /// <summary>
        /// 相邻刀刃根部的目标间距，单位为游戏世界格。
        /// </summary>
        private const float BladeSpacing = 0.42f;

        /// <summary>
        /// 刀刃根部横向覆盖目标显示框的比例，避免特效外溢过大。
        /// </summary>
        private const float CoverageWidthFactor = 0.76f;

        /// <summary>
        /// 根部位置的小幅非均匀偏移，打散等间距阵列。
        /// </summary>
        private const float RootJitterAmplitude = 0.055f;

        /// <summary>
        /// 复用旧版命中特效已经校准过的地面接地偏移。
        /// 该值独立于目标贴图平面高度，避免把透明边界误当成脚底线。
        /// </summary>
        private const float GroundContactOffset = 0.50f;

        /// <summary>
        /// 除动态端点穿刺位外，按根部从左到右分配的非单调混合倾角；前后层共用完整槽位序列，避免分层后变成左右花瓣。
        /// </summary>
        private static readonly float[] BladeAnglePattern =
        {
            -36f,
            24f,
            8f,
            -28f,
            -14f,
            30f,
            18f,
            -22f,
            6f
        };

        /// <summary>
        /// 动态刀刃数量下限，保证窄目标也有完整刺击感。
        /// </summary>
        private const int MinimumBladeCount = 4;

        /// <summary>
        /// 动态刀刃数量上限，避免视觉效果退化为粒子云。
        /// </summary>
        private const int MaximumBladeCount = 9;

        /// <summary>
        /// 最早与最晚刀刃之间的错峰窗口。
        /// </summary>
        private const int MaximumStaggerTicks = 1;

        /// <summary>
        /// 刀刃长度相对目标高度的倍率。
        /// </summary>
        private const float BladeLengthFactor = 0.86f;

        /// <summary>
        /// 刀刃长度安全下限。
        /// </summary>
        private const float MinimumBladeLength = 0.75f;

        /// <summary>
        /// 刀刃长度安全上限。
        /// </summary>
        private const float MaximumBladeLength = 1.85f;

        /// <summary>
        /// 已生成的短命刀刃，用于维护和清理。
        /// </summary>
        private readonly List<Mote_ImpactBlade> spawnedMotes =
            new List<Mote_ImpactBlade>();

        /// <summary>
        /// 防止同一个子特效器被重复触发时再次生成整组刀刃。
        /// </summary>
        private bool burstStarted;

        /// <summary>
        /// 创建动态刀刃阵列子特效器。
        /// </summary>
        /// <param name="def">当前子特效器 Def。</param>
        /// <param name="parent">所属 Effecter。</param>
        public SubEffecter_ImpactBladeBurst(
            SubEffecterDef def,
            Effecter parent)
            : base(def, parent)
        {
        }

        /// <summary>
        /// 首次触发时读取目标尺寸并生成对应数量的刀刃；非 Pawn（人物）目标使用默认尺寸。
        /// </summary>
        /// <param name="A">Effecter 源目标。</param>
        /// <param name="B">Effecter 受击目标。</param>
        /// <param name="overrideSpawnTick">原版兼容的生成 Tick 参数。</param>
        /// <param name="force">原版兼容的强制触发参数。</param>
        public override void SubTrigger(
            TargetInfo A,
            TargetInfo B,
            int overrideSpawnTick = -1,
            bool force = false)
        {
            if (burstStarted)
            {
                return;
            }

            burstStarted = true;
            Thing targetThing = ResolveTargetThing(A, B);
            Map map = A.Map ?? B.Map;
            if (targetThing == null
                || map == null
                || def == null
                || def.moteDef == null)
            {
                return;
            }

            Vector2 visualSize = ResolveTargetVisualSize(targetThing);
            float targetWidth = Mathf.Max(
                0.1f,
                Mathf.Abs(visualSize.x) * CoverageWidthFactor);
            float targetHeight = Mathf.Max(0.1f, Mathf.Abs(visualSize.y));
            int bladeCount = ComputeBladeCount(targetWidth);
            float bladeLength = Mathf.Clamp(
                targetHeight * BladeLengthFactor,
                MinimumBladeLength,
                MaximumBladeLength);
            Vector3 impactPosition = ResolveImpactPosition(A, targetThing);
            float rootZ = impactPosition.z - GroundContactOffset;
            bool frontLayer = IsFrontLayer(def.moteDef);
            IntVec3 spawnCell = impactPosition.ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                return;
            }

            Rand.PushState(BuildRandomSeed(targetThing, frontLayer));
            try
            {
                for (int index = 0; index < bladeCount; index++)
                {
                    if (((index & 1) == 1) != frontLayer)
                    {
                        continue;
                    }

                    float rootX = impactPosition.x
                        + ResolveRootOffset(index, bladeCount) * targetWidth;
                    float width = ResolveBladeWidth(targetWidth, bladeCount);
                    float length = bladeLength * Rand.Range(0.92f, 1.08f);
                    float angle = ResolveBladeAngle(index, bladeCount);
                    int startDelayTicks = ResolveStartDelay(index, bladeCount);

                    Mote_ImpactBlade mote =
                        ThingMaker.MakeThing(def.moteDef) as Mote_ImpactBlade;
                    if (mote == null)
                    {
                        continue;
                    }

                    mote.Configure(width, length, startDelayTicks);
                    mote.exactPosition = new Vector3(rootX, 0f, rootZ);
                    mote.exactRotation = angle;
                    mote.instanceColor = EffectiveColor;
                    mote.Scale = 1f;
                    GenSpawn.Spawn(mote, spawnCell, map);
                    mote.exactPosition = new Vector3(rootX, 0f, rootZ);
                    mote.Maintain();
                    spawnedMotes.Add(mote);
                }
            }
            finally
            {
                Rand.PopState();
            }
        }

        /// <summary>
        /// 维护需要维护的 Mote；位置不会根据后续目标移动重新计算。
        /// </summary>
        /// <param name="A">Effecter 源目标。</param>
        /// <param name="B">Effecter 受击目标。</param>
        public override void SubEffectTick(TargetInfo A, TargetInfo B)
        {
            for (int index = spawnedMotes.Count - 1; index >= 0; index--)
            {
                Mote_ImpactBlade mote = spawnedMotes[index];
                if (mote == null || mote.Destroyed)
                {
                    spawnedMotes.RemoveAt(index);
                    continue;
                }

                mote.Maintain();
            }
        }

        /// <summary>
        /// Effecter 结束时释放所有仍存活的刀刃 Mote。
        /// </summary>
        public override void SubCleanup()
        {
            for (int index = spawnedMotes.Count - 1; index >= 0; index--)
            {
                Mote_ImpactBlade mote = spawnedMotes[index];
                if (mote != null && !mote.Destroyed)
                {
                    mote.Destroy();
                }
            }

            spawnedMotes.Clear();
            base.SubCleanup();
        }

        /// <summary>
        /// 按目标显示宽度计算刀刃数量，并限制在安全范围内。
        /// </summary>
        /// <param name="targetWidth">目标身体图形宽度。</param>
        /// <returns>动态刀刃数量。</returns>
        private static int ComputeBladeCount(float targetWidth)
        {
            return Mathf.Clamp(
                Mathf.CeilToInt(targetWidth / BladeSpacing),
                MinimumBladeCount,
                MaximumBladeCount);
        }

        /// <summary>
        /// 按目标宽度和数量计算单道刀刃底边宽度，使相邻轮廓略有重叠。
        /// </summary>
        /// <param name="targetWidth">目标身体图形宽度。</param>
        /// <param name="bladeCount">当前刀刃数量。</param>
        /// <returns>单道刀刃底边宽度。</returns>
        private static float ResolveBladeWidth(float targetWidth, int bladeCount)
        {
            return Mathf.Clamp(
                targetWidth / Mathf.Max(1, bladeCount) * 1.18f,
                0.24f,
                0.42f);
        }

        /// <summary>
        /// 生成非单调混合方向：保留两道朝内大倾角和两侧外刺，其余槽位同时包含左右倾向。
        /// </summary>
        /// <param name="index">刀刃索引。</param>
        /// <param name="bladeCount">当前刀刃数量。</param>
        /// <returns>刀尖相对底边中心的倾角。</returns>
        private static float ResolveBladeAngle(int index, int bladeCount)
        {
            float angle;
            if (index == 0)
            {
                angle = 44f;
            }
            else if (index == 1)
            {
                angle = -34f;
            }
            else if (index == bladeCount - 2)
            {
                angle = -42f;
            }
            else if (index == bladeCount - 1)
            {
                angle = 34f;
            }
            else
            {
                angle = BladeAnglePattern[index % BladeAnglePattern.Length];
            }

            return angle + Rand.Range(-4f, 4f);
        }

        /// <summary>
        /// 生成非等距根部位置，保持所有底边落在同一条水平地面线上。
        /// </summary>
        /// <param name="index">刀刃索引。</param>
        /// <param name="bladeCount">当前刀刃数量。</param>
        /// <returns>相对覆盖宽度中心的归一化横向偏移。</returns>
        private static float ResolveRootOffset(int index, int bladeCount)
        {
            float normalizedIndex = bladeCount <= 1
                ? 0.5f
                : (float)index / (bladeCount - 1);
            float normalizedOffset = normalizedIndex - 0.5f;
            float jitter;
            switch (index % 9)
            {
                case 0:
                    jitter = -0.018f;
                    break;
                case 1:
                    jitter = 0.037f;
                    break;
                case 2:
                    jitter = -0.046f;
                    break;
                case 3:
                    jitter = 0.022f;
                    break;
                case 4:
                    jitter = -0.031f;
                    break;
                case 5:
                    jitter = 0.049f;
                    break;
                case 6:
                    jitter = -0.041f;
                    break;
                case 7:
                    jitter = 0.014f;
                    break;
                default:
                    jitter = -0.008f;
                    break;
            }

            return Mathf.Clamp(
                normalizedOffset + jitter * (RootJitterAmplitude / 0.05f),
                -0.5f,
                0.5f);
        }

        /// <summary>
        /// 将最早和最晚刀刃分布在 0～1 Tick 内。
        /// </summary>
        /// <param name="index">刀刃索引。</param>
        /// <param name="bladeCount">当前刀刃数量。</param>
        /// <returns>起刺延迟 Tick。</returns>
        private static int ResolveStartDelay(int index, int bladeCount)
        {
            if (bladeCount <= 1)
            {
                return 0;
            }

            return Mathf.RoundToInt(
                (float)index / (bladeCount - 1) * MaximumStaggerTicks);
        }

        /// <summary>
        /// 读取目标身体图形和体型倍率，得到游戏世界中的显示宽高。
        /// </summary>
        /// <param name="targetThing">当前受击目标；Pawn（人物）使用身体图形尺寸。</param>
        /// <returns>目标身体图形显示尺寸。</returns>
        private static Vector2 ResolveTargetVisualSize(Thing targetThing)
        {
            if (!(targetThing is Pawn targetPawn))
            {
                return Vector2.one;
            }

            Vector2 visualSize = targetPawn.DrawSize;
            Graphic bodyGraphic = targetPawn.Drawer != null
                && targetPawn.Drawer.renderer != null
                ? targetPawn.Drawer.renderer.BodyGraphic
                : null;
            if (bodyGraphic != null && bodyGraphic.drawSize != Vector2.zero)
            {
                visualSize = bodyGraphic.drawSize;
                if (targetPawn.Rotation.IsHorizontal && !bodyGraphic.ShouldDrawRotated)
                {
                    visualSize = visualSize.Rotated();
                }
            }

            if (targetPawn.story != null && targetPawn.story.bodyType != null)
            {
                visualSize = Vector2.Scale(
                    visualSize,
                    targetPawn.story.bodyType.bodyGraphicScale);
            }

            if (float.IsNaN(visualSize.x)
                || float.IsInfinity(visualSize.x)
                || float.IsNaN(visualSize.y)
                || float.IsInfinity(visualSize.y))
            {
                return Vector2.one;
            }

            return visualSize;
        }

        /// <summary>
        /// 把 Effecter 的精确偏移叠加到目标中心，形成固定命中锚点。
        /// </summary>
        /// <param name="sourceTarget">Effecter 源目标；命中执行器会将其固定到命中瞬间位置所在格。</param>
        /// <param name="targetThing">当前受击目标，作为安全回退。</param>
        /// <returns>命中瞬间的平面坐标。</returns>
        private Vector3 ResolveImpactPosition(TargetInfo sourceTarget, Thing targetThing)
        {
            Vector3 position = sourceTarget.Cell.IsValid
                ? sourceTarget.Cell.ToVector3Shifted()
                : targetThing.DrawPos;
            if (parent != null)
            {
                position += parent.offset;
            }

            position.y = 0f;
            return position;
        }

        /// <summary>
        /// 从 Effecter 两个目标中取得实际命中的 Thing。
        /// </summary>
        /// <param name="A">Effecter 源目标。</param>
        /// <param name="B">Effecter 受击目标。</param>
        /// <returns>命中 Thing；找不到时返回 null。</returns>
        private static Thing ResolveTargetThing(TargetInfo A, TargetInfo B)
        {
            return B.HasThing
                ? B.Thing
                : A.HasThing
                    ? A.Thing
                    : null;
        }

        /// <summary>
        /// 通过 Mote 图形高度判断当前子项是否为前景层。
        /// </summary>
        /// <param name="moteDef">当前 Mote Def。</param>
        /// <returns>前景层返回 true。</returns>
        private static bool IsFrontLayer(ThingDef moteDef)
        {
            return moteDef != null
                && string.Equals(
                    moteDef.defName,
                    "BDP_Mote_ImpactBlade_Front",
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// 为前后层生成稳定但不同的随机序列。
        /// </summary>
        /// <param name="targetThing">当前受击 Thing（实体）。</param>
        /// <param name="frontLayer">是否为前景层。</param>
        /// <returns>随机种子。</returns>
        private static int BuildRandomSeed(Thing targetThing, bool frontLayer)
        {
            int seed = targetThing.thingIDNumber * 397;
            return frontLayer ? seed + 17 : seed + 31;
        }
    }
}
