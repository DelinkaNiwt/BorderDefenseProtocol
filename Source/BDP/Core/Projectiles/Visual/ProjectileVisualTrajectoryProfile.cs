using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Projection;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.Visual
{
    /// <summary>
    /// 投射物视觉轨迹参数快照。
    /// 它只承载通用绘制偏移，不承载任何具体模块或飞行模式语义。
    /// </summary>
    public sealed class ProjectileVisualTrajectoryProfile : IExposable
    {
        /// <summary>
        /// 轨迹起点的绘制偏移。
        /// </summary>
        public float StartOffset { get; set; }

        /// <summary>
        /// 轨迹终点的绘制偏移。
        /// </summary>
        public float EndOffset { get; set; }

        /// <summary>
        /// 轨迹中段额外绘制偏移的最高值。
        /// </summary>
        public float ArcOffset { get; set; }

        /// <summary>
        /// 当前参数是否产生实际绘制偏移。
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return Mathf.Abs(StartOffset) > 0.0001f
                    || Mathf.Abs(EndOffset) > 0.0001f
                    || Mathf.Abs(ArcOffset) > 0.0001f;
            }
        }

        /// <summary>
        /// 创建当前参数的独立副本。
        /// </summary>
        public ProjectileVisualTrajectoryProfile Clone()
        {
            return new ProjectileVisualTrajectoryProfile
            {
                StartOffset = StartOffset,
                EndOffset = EndOffset,
                ArcOffset = ArcOffset
            };
        }

        /// <summary>
        /// 持久化视觉轨迹参数。
        /// </summary>
        public void ExposeData()
        {
            float startOffset = StartOffset;
            float endOffset = EndOffset;
            float arcOffset = ArcOffset;

            Scribe_Values.Look(ref startOffset, "startOffset", 0f);
            Scribe_Values.Look(ref endOffset, "endOffset", 0f);
            Scribe_Values.Look(ref arcOffset, "arcOffset", 0f);

            StartOffset = startOffset;
            EndOffset = endOffset;
            ArcOffset = arcOffset;
        }
    }

    /// <summary>
    /// 投射物视觉轨迹的纯取样工具。
    /// 它复用当前飞行路径，只额外生成一个通用绘制偏移。
    /// </summary>
    public static class ProjectileVisualTrajectoryUtility
    {
        /// <summary>
        /// 判断视觉轨迹参数是否有效。
        /// </summary>
        public static bool IsEnabled(ProjectileVisualTrajectoryProfile profile)
        {
            return profile != null && profile.IsEnabled;
        }

        /// <summary>
        /// 按当前物理路径进度与整发视觉进度取样。
        /// 当前物理路径可以在飞行中被模块替换；整发视觉进度不随路径替换重置。
        /// </summary>
        public static ProjectileVisualTrajectorySample Evaluate(
            ProjectileFlightPathSnapshot snapshot,
            float pathProgress,
            float trajectoryProgress,
            ProjectileVisualTrajectoryProfile profile,
            float altitude)
        {
            float clampedPathProgress = Mathf.Clamp01(pathProgress);
            float clampedTrajectoryProgress = Mathf.Clamp01(trajectoryProgress);
            Vector3 basePosition = EvaluateBasePosition(snapshot, clampedPathProgress, altitude);
            Vector3 visualPosition = EvaluateVisualPosition(
                snapshot,
                clampedPathProgress,
                clampedTrajectoryProgress,
                profile,
                altitude);
            Vector3 visualTangent = EvaluateVisualTangent(
                snapshot,
                clampedPathProgress,
                clampedTrajectoryProgress,
                profile,
                altitude);
            float offset = EvaluateOffset(profile, clampedTrajectoryProgress);
            return new ProjectileVisualTrajectorySample(
                clampedTrajectoryProgress,
                basePosition,
                visualPosition,
                visualTangent,
                offset);
        }

        /// <summary>
        /// 按进度计算当前绘制偏移。
        /// 使用原版迫击炮的反抛物线函数形成中段最高、两端归零的轨迹。
        /// </summary>
        public static float EvaluateOffset(
            ProjectileVisualTrajectoryProfile profile,
            float progress)
        {
            if (profile == null)
            {
                return 0f;
            }

            float clampedProgress = Mathf.Clamp01(progress);
            // 保留原始整发进度，使发射瞬间立即拥有向上的视觉切线。
            // 这里只改变绘制偏移，不回写真实路径或 ExactRotation（精确旋转）。
            float baseOffset = Mathf.Lerp(profile.StartOffset, profile.EndOffset, clampedProgress);
            float arcOffset = Mathf.Max(0f, profile.ArcOffset)
                * GenMath.InverseParabola(clampedProgress);
            return baseOffset + arcOffset;
        }

        /// <summary>
        /// 计算未叠加绘制偏移的基础位置。
        /// </summary>
        private static Vector3 EvaluateBasePosition(
            ProjectileFlightPathSnapshot snapshot,
            float progress,
            float altitude)
        {
            if (snapshot == null)
            {
                return Vector3.up * altitude;
            }

            return ProjectileFlightPathUtility.EvaluatePosition(snapshot, progress).Yto0()
                + Vector3.up * altitude;
        }

        /// <summary>
        /// 计算叠加绘制偏移后的视觉位置。
        /// </summary>
        private static Vector3 EvaluateVisualPosition(
            ProjectileFlightPathSnapshot snapshot,
            float pathProgress,
            float trajectoryProgress,
            ProjectileVisualTrajectoryProfile profile,
            float altitude)
        {
            if (snapshot == null)
            {
                return Vector3.up * altitude;
            }

            Vector3 basePosition = EvaluateBasePosition(snapshot, pathProgress, altitude);
            return basePosition + Vector3.forward * EvaluateOffset(profile, trajectoryProgress);
        }

        /// <summary>
        /// 通过相邻视觉位置估算当前绘制切线。
        /// </summary>
        private static Vector3 EvaluateVisualTangent(
            ProjectileFlightPathSnapshot snapshot,
            float pathProgress,
            float trajectoryProgress,
            ProjectileVisualTrajectoryProfile profile,
            float altitude)
        {
            if (snapshot == null)
            {
                return Vector3.forward;
            }

            const float sampleStep = 0.005f;
            float previousPathProgress = Mathf.Max(0f, pathProgress - sampleStep);
            float nextPathProgress = Mathf.Min(1f, pathProgress + sampleStep);
            float previousTrajectoryProgress = Mathf.Max(0f, trajectoryProgress - sampleStep);
            float nextTrajectoryProgress = Mathf.Min(1f, trajectoryProgress + sampleStep);
            Vector3 tangent = EvaluateVisualPosition(
                    snapshot,
                    nextPathProgress,
                    nextTrajectoryProgress,
                    profile,
                    altitude)
                - EvaluateVisualPosition(
                    snapshot,
                    previousPathProgress,
                    previousTrajectoryProgress,
                    profile,
                    altitude);
            if (tangent.sqrMagnitude <= 0.0001f)
            {
                tangent = ProjectileFlightPathUtility.EvaluateTangent(snapshot, pathProgress);
            }

            return tangent.sqrMagnitude <= 0.0001f
                ? Vector3.forward
                : tangent.normalized;
        }
    }

    /// <summary>
    /// 一次视觉轨迹取样结果。
    /// </summary>
    public readonly struct ProjectileVisualTrajectorySample
    {
        /// <summary>当前取样进度。</summary>
        public float Progress { get; }

        /// <summary>未叠加绘制偏移的基础位置。</summary>
        public Vector3 BasePosition { get; }

        /// <summary>叠加绘制偏移后的视觉位置。</summary>
        public Vector3 VisualPosition { get; }

        /// <summary>视觉轨迹切线。</summary>
        public Vector3 VisualTangent { get; }

        /// <summary>当前绘制偏移。</summary>
        public float Offset { get; }

        /// <summary>
        /// 创建一次视觉轨迹取样结果。
        /// </summary>
        public ProjectileVisualTrajectorySample(
            float progress,
            Vector3 basePosition,
            Vector3 visualPosition,
            Vector3 visualTangent,
            float offset)
        {
            Progress = progress;
            BasePosition = basePosition;
            VisualPosition = visualPosition;
            VisualTangent = visualTangent;
            Offset = offset;
        }
    }
}
