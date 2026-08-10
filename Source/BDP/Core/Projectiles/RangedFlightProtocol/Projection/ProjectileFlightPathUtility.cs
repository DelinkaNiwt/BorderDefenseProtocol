using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Projection
{
    /// <summary>
    /// 投射物路径快照的纯几何工具。
    /// 它只负责建模与取样，不承载任何追踪业务语义。
    /// </summary>
    public static class ProjectileFlightPathUtility
    {
        /// <summary>
        /// 创建一条直线路径快照。
        /// </summary>
        public static ProjectileFlightPathSnapshot CreateLinear(Vector3 start, Vector3 end)
        {
            ProjectileFlightPathSnapshot snapshot = new ProjectileFlightPathSnapshot
            {
                Kind = ProjectileFlightPathKind.Linear,
                Start = start,
                ControlA = start,
                ControlB = end,
                End = end
            };
            snapshot.ApproximateLength = EstimateLength(snapshot);
            return snapshot;
        }

        /// <summary>
        /// 创建一条三次贝塞尔路径快照。
        /// </summary>
        public static ProjectileFlightPathSnapshot CreateCubicBezier(
            Vector3 start,
            Vector3 controlA,
            Vector3 controlB,
            Vector3 end)
        {
            ProjectileFlightPathSnapshot snapshot = new ProjectileFlightPathSnapshot
            {
                Kind = ProjectileFlightPathKind.CubicBezier,
                Start = start,
                ControlA = controlA,
                ControlB = controlB,
                End = end
            };
            snapshot.ApproximateLength = EstimateLength(snapshot);
            return snapshot;
        }

        /// <summary>
        /// 按进度取样路径位置。
        /// </summary>
        public static Vector3 EvaluatePosition(ProjectileFlightPathSnapshot snapshot, float progress)
        {
            if (snapshot == null)
            {
                return Vector3.zero;
            }

            float clampedProgress = Mathf.Clamp01(progress);
            switch (snapshot.Kind)
            {
                case ProjectileFlightPathKind.CubicBezier:
                    return EvaluateCubicBezierPosition(snapshot, clampedProgress);
                default:
                    return Vector3.Lerp(snapshot.Start, snapshot.End, clampedProgress);
            }
        }

        /// <summary>
        /// 按进度取样路径切线。
        /// </summary>
        public static Vector3 EvaluateTangent(ProjectileFlightPathSnapshot snapshot, float progress)
        {
            if (snapshot == null)
            {
                return Vector3.forward;
            }

            float clampedProgress = Mathf.Clamp01(progress);
            Vector3 tangent;
            switch (snapshot.Kind)
            {
                case ProjectileFlightPathKind.CubicBezier:
                    tangent = EvaluateCubicBezierTangent(snapshot, clampedProgress);
                    break;
                default:
                    tangent = (snapshot.End - snapshot.Start).Yto0();
                    break;
            }

            if (tangent.sqrMagnitude <= 0.0001f)
            {
                tangent = (snapshot.End - snapshot.Start).Yto0();
            }

            return tangent.sqrMagnitude <= 0.0001f ? Vector3.forward : tangent.normalized;
        }

        /// <summary>
        /// 按指定进度裁出当前路径的前缀快照。
        /// 它只处理纯几何裁切，不关心任何业务语义。
        /// </summary>
        public static ProjectileFlightPathSnapshot CreatePrefix(ProjectileFlightPathSnapshot snapshot, float endProgress)
        {
            if (snapshot == null)
            {
                return null;
            }

            float clampedEndProgress = Mathf.Clamp01(endProgress);
            if (clampedEndProgress <= 0.0001f)
            {
                return CreateLinear(snapshot.Start, snapshot.Start);
            }

            if (clampedEndProgress >= 0.9999f)
            {
                return Clone(snapshot);
            }

            switch (snapshot.Kind)
            {
                case ProjectileFlightPathKind.CubicBezier:
                    return CreateCubicBezierPrefix(snapshot, clampedEndProgress);
                default:
                    return CreateLinear(snapshot.Start, EvaluatePosition(snapshot, clampedEndProgress));
            }
        }

        /// <summary>
        /// 估算路径长度。
        /// 第一版只做固定采样，不引入弧长表。
        /// </summary>
        public static float EstimateLength(ProjectileFlightPathSnapshot snapshot, int samples = 8)
        {
            if (snapshot == null)
            {
                return 0f;
            }

            int safeSamples = Mathf.Max(1, samples);
            Vector3 previous = EvaluatePosition(snapshot, 0f);
            float totalLength = 0f;
            for (int i = 1; i <= safeSamples; i++)
            {
                float progress = (float)i / safeSamples;
                Vector3 current = EvaluatePosition(snapshot, progress);
                totalLength += (current - previous).magnitude;
                previous = current;
            }

            return totalLength;
        }

        /// <summary>
        /// 取样三次贝塞尔位置。
        /// </summary>
        private static Vector3 EvaluateCubicBezierPosition(ProjectileFlightPathSnapshot snapshot, float progress)
        {
            float inverse = 1f - progress;
            float inverseSquare = inverse * inverse;
            float progressSquare = progress * progress;
            return
                (inverseSquare * inverse * snapshot.Start)
                + (3f * inverseSquare * progress * snapshot.ControlA)
                + (3f * inverse * progressSquare * snapshot.ControlB)
                + (progressSquare * progress * snapshot.End);
        }

        /// <summary>
        /// 取样三次贝塞尔切线。
        /// </summary>
        private static Vector3 EvaluateCubicBezierTangent(ProjectileFlightPathSnapshot snapshot, float progress)
        {
            float inverse = 1f - progress;
            return
                (3f * inverse * inverse * (snapshot.ControlA - snapshot.Start))
                + (6f * inverse * progress * (snapshot.ControlB - snapshot.ControlA))
                + (3f * progress * progress * (snapshot.End - snapshot.ControlB));
        }

        /// <summary>
        /// 复制一份路径快照，避免调用方误改原始实例。
        /// </summary>
        private static ProjectileFlightPathSnapshot Clone(ProjectileFlightPathSnapshot snapshot)
        {
            ProjectileFlightPathSnapshot clone = new ProjectileFlightPathSnapshot
            {
                Kind = snapshot.Kind,
                Start = snapshot.Start,
                ControlA = snapshot.ControlA,
                ControlB = snapshot.ControlB,
                End = snapshot.End,
                ApproximateLength = snapshot.ApproximateLength
            };
            if (clone.ApproximateLength <= 0.001f)
            {
                clone.ApproximateLength = EstimateLength(clone);
            }

            return clone;
        }

        /// <summary>
        /// 对三次贝塞尔路径做前缀裁切，保留原始曲线前半段的几何走势。
        /// </summary>
        private static ProjectileFlightPathSnapshot CreateCubicBezierPrefix(
            ProjectileFlightPathSnapshot snapshot,
            float endProgress)
        {
            Vector3 startToControlA = Vector3.Lerp(snapshot.Start, snapshot.ControlA, endProgress);
            Vector3 controlAToControlB = Vector3.Lerp(snapshot.ControlA, snapshot.ControlB, endProgress);
            Vector3 controlBToEnd = Vector3.Lerp(snapshot.ControlB, snapshot.End, endProgress);
            Vector3 firstBridge = Vector3.Lerp(startToControlA, controlAToControlB, endProgress);
            Vector3 secondBridge = Vector3.Lerp(controlAToControlB, controlBToEnd, endProgress);
            Vector3 prefixEnd = Vector3.Lerp(firstBridge, secondBridge, endProgress);
            return CreateCubicBezier(
                snapshot.Start,
                startToControlA,
                firstBridge,
                prefixEnd);
        }
    }
}
