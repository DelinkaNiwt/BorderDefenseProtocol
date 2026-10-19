using System.Collections.Generic;
using BDP.Content.RangedModules.OrderedSpread;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.PathInput;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Projection;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.RoutePath
{
    /// <summary>
    /// 路线段解析器。
    /// 负责冻结路径与散布的纯计算，不直接接触投射物宿主状态。
    /// </summary>
    public static class RouteSegmentResolver
    {
        /// <summary>归一化一组锚点，去掉连续重复格。</summary>
        public static List<PathAnchor> NormalizeAnchors(IReadOnlyList<PathAnchor> source)
        {
            List<PathAnchor> result = new List<PathAnchor>();
            if (source == null) return result;

            for (int i = 0; i < source.Count; i++)
            {
                PathAnchor anchor = source[i];
                if (anchor == null) continue;

                PathAnchor clone = anchor.CloneTyped();
                if (result.Count > 0 && result[result.Count - 1].ToCell() == clone.ToCell()) continue;
                result.Add(clone);
            }
            return result;
        }

        /// <summary>把确认快照写入投射物路径上下文。</summary>
        public static void PopulatePathSnapshot(
            RoutePathContext target, RouteConfirmedSnapshot snapshot,
            float intermediateSpreadRadius, float finalSpreadRadius, float highAccuracySpreadScale)
        {
            if (target == null) return;
            target.Reset();
            if (snapshot == null) return;

            target.Anchors = NormalizeAnchors(snapshot.Anchors);
            target.HasFinalTarget = snapshot.HasFinalTarget;
            target.FinalTarget = snapshot.FinalTarget;
            target.FinalIsThing = snapshot.FinalIsThing;
            target.PathSource = snapshot.PathSource;
            target.IntermediateSpreadRadius = Mathf.Max(0f, intermediateSpreadRadius);
            target.FinalSpreadRadius = Mathf.Max(0f, finalSpreadRadius);
            target.HighAccuracySpreadScale = Mathf.Clamp01(highAccuracySpreadScale);
            target.HasFrozenFinalDestination = snapshot.HasFinalTarget && snapshot.FinalTarget.IsValid;
            target.FrozenFinalDestination = target.HasFrozenFinalDestination
                ? ResolveFrozenFinalDestination(snapshot.FinalTarget) : Vector3.zero;
        }

        /// <summary>把唯一一次发射裁定应用于路线，保留锚点且不重新抽取命中或掩体。</summary>
        public static void ApplyLaunchResolution(RoutePathContext snapshot, ProjectileLaunchResolution resolution)
        {
            if (snapshot == null || resolution == null) return;
            if (resolution.ContinuationMode == ProjectileLaunchContinuationMode.WildMissOffsetRoute)
            {
                snapshot.HasWildMissOffset = true;
                snapshot.WildMissOffset = resolution.WildMissOffset.Yto0();
            }
            if (!resolution.TerminalTarget.IsValid) return;

            snapshot.HasFinalTarget = true;
            snapshot.FinalTarget = resolution.TerminalTarget;
            snapshot.FinalIsThing = resolution.TerminalTarget.HasThing;
            snapshot.HasFrozenFinalDestination = true;
            snapshot.FrozenFinalDestination = resolution.TerminalDestination.Yto0();
            // 掩体已是本发确定的拦截结果，不能在末段再加一次散布把落点推离掩体。
            snapshot.FinalSpreadRadius = 0f;
        }

        /// <summary>解析首段目标。</summary>
        public static bool TryResolveFirstLegTarget(RouteConfirmedSnapshot snapshot, out LocalTargetInfo firstTarget)
        {
            List<LocalTargetInfo> segments = BuildSegments(snapshot);
            if (segments.Count <= 0) { firstTarget = LocalTargetInfo.Invalid; return false; }
            firstTarget = segments[0];
            return firstTarget.IsValid;
        }

        /// <summary>解析当前飞行段对应的正式目标。</summary>
        public static bool TryResolveCurrentLegTarget(RoutePathContext snapshot, out LocalTargetInfo currentTarget)
        {
            List<LocalTargetInfo> segments = BuildSegments(snapshot);
            int legIndex = ResolveCurrentLegIndex(snapshot, segments.Count);
            if (!TryResolveSegmentAtIndex(segments, legIndex, out currentTarget))
            { currentTarget = LocalTargetInfo.Invalid; return false; }
            return currentTarget.IsValid;
        }

        /// <summary>解析下一段目标与目的地。</summary>
        public static bool TryResolveNextLeg(
            RoutePathContext snapshot, out LocalTargetInfo nextTarget, out Vector3 nextDestination)
        {
            List<LocalTargetInfo> segments = BuildSegments(snapshot);
            int nextIndex = ResolveCurrentLegIndex(snapshot, segments.Count) + 1;
            if (!TryResolveSegmentAtIndex(segments, nextIndex, out nextTarget))
            { nextTarget = LocalTargetInfo.Invalid; nextDestination = Vector3.zero; return false; }
            nextDestination = ResolveSegmentBaseDestination(snapshot, nextTarget, nextIndex, segments.Count);
            return nextTarget.IsValid;
        }

        /// <summary>为当前段构造线性飞行路径快照（含散布偏移）。</summary>
        public static ProjectileFlightPathSnapshot BuildCurrentLegFlightPathSnapshot(
            RoutePathContext snapshot,
            ProjectileAccuracySnapshot accuracySnapshot, Vector3 start,
            string attackInstanceId, string resultId, int emitIndex)
        {
            List<LocalTargetInfo> segments = BuildSegments(snapshot);
            int legIndex = ResolveCurrentLegIndex(snapshot, segments.Count);
            return TryBuildLegFlightPath(
                snapshot,
                segments,
                legIndex,
                accuracySnapshot,
                start,
                attackInstanceId,
                resultId,
                emitIndex,
                out ProjectileFlightPathSnapshot path)
                ? path
                : null;
        }

        /// <summary>
        /// 解析当前到达点之后的续段；只按精度采样偏移，不读取墙体或回接名义点。
        /// </summary>
        public static bool TryResolveContinuation(
            RoutePathContext snapshot,
            Vector3 start,
            ProjectileAccuracySnapshot accuracySnapshot,
            string attackInstanceId,
            string resultId,
            int emitSequence,
            out LocalTargetInfo nextTarget,
            out ProjectileFlightPathSnapshot nextPath)
        {
            nextTarget = LocalTargetInfo.Invalid;
            nextPath = null;

            List<LocalTargetInfo> segments = BuildSegments(snapshot);
            int currentIndex = ResolveCurrentLegIndex(snapshot, segments.Count);
            int nextIndex = currentIndex + 1;
            if (!TryResolveSegmentAtIndex(segments, nextIndex, out LocalTargetInfo nextNominalTarget))
            {
                return false;
            }

            if (TryBuildLegFlightPath(
                    snapshot,
                    segments,
                    nextIndex,
                    accuracySnapshot,
                    start,
                    attackInstanceId,
                    resultId,
                    emitSequence,
                    out nextPath))
            {
                nextTarget = nextNominalTarget;
                return true;
            }
            return false;
        }

        /// <summary>推进到下一段。</summary>
        public static bool TryAdvanceLeg(RoutePathContext snapshot)
        {
            List<LocalTargetInfo> segments = BuildSegments(snapshot);
            if (snapshot == null || segments.Count <= 0) return false;
            int legIndex = ResolveCurrentLegIndex(snapshot, segments.Count);
            int nextIndex = legIndex + 1;
            if (nextIndex < 0 || nextIndex >= segments.Count) return false;
            snapshot.CurrentLegIndex = nextIndex;
            return true;
        }

        private static List<LocalTargetInfo> BuildSegments(RoutePathContext snapshot)
        {
            List<LocalTargetInfo> result = new List<LocalTargetInfo>();
            if (snapshot == null) return result;
            AppendAnchorTargets(result, snapshot.Anchors);
            AppendFinalTarget(result, snapshot.HasFinalTarget, snapshot.FinalTarget);
            return result;
        }

        private static List<LocalTargetInfo> BuildSegments(RouteConfirmedSnapshot snapshot)
        {
            List<LocalTargetInfo> result = new List<LocalTargetInfo>();
            if (snapshot == null) return result;
            AppendAnchorTargets(result, snapshot.Anchors);
            AppendFinalTarget(result, snapshot.HasFinalTarget, snapshot.FinalTarget);
            return result;
        }

        /// <summary>为指定名义段生成按冻结精度缩放的稳定偏航快照，撞墙由宿主正常处理。</summary>
        private static bool TryBuildLegFlightPath(
            RoutePathContext snapshot,
            IReadOnlyList<LocalTargetInfo> segments,
            int legIndex,
            ProjectileAccuracySnapshot accuracySnapshot,
            Vector3 start,
            string attackInstanceId,
            string resultId,
            int emitSequence,
            out ProjectileFlightPathSnapshot path)
        {
            path = null;
            if (!TryResolveSegmentAtIndex(segments, legIndex, out LocalTargetInfo currentTarget))
            {
                return false;
            }

            bool isFinal = legIndex == segments.Count - 1;
            Vector3 nominalDestination = ResolveSegmentBaseDestination(
                snapshot,
                currentTarget,
                legIndex,
                segments.Count);
            float spreadRadius = OrderedSpreadSampler.ResolveScaledRadius(
                accuracySnapshot,
                isFinal ? snapshot.FinalSpreadRadius : snapshot.IntermediateSpreadRadius,
                snapshot.HighAccuracySpreadScale);
            Vector3 sampledOffset = OrderedSpreadSampler.SampleOffset(
                spreadRadius,
                attackInstanceId,
                resultId,
                emitSequence,
                legIndex,
                isFinal
                    ? OrderedSpreadSampler.SampleKind.Final
                    : OrderedSpreadSampler.SampleKind.Intermediate);
            // 每段围绕名义点独立采样；不检测障碍、不折半、不因撞墙风险收回偏移。
            path = ProjectileFlightPathUtility.CreateLinear(
                NormalizePoint(start),
                (nominalDestination + sampledOffset).Yto0());
            return path != null;
        }

        private static Vector3 ResolveSegmentBaseDestination(
            RoutePathContext snapshot, LocalTargetInfo segmentTarget,
            int segmentIndex, int segmentCount)
        {
            bool isFinal = segmentCount > 0 && segmentIndex == segmentCount - 1;
            if (isFinal && snapshot != null && snapshot.HasFrozenFinalDestination)
                return NormalizePoint(snapshot.FrozenFinalDestination)
                    + (snapshot.HasWildMissOffset ? snapshot.WildMissOffset.Yto0() : Vector3.zero);
            return segmentTarget.IsValid
                ? NormalizePoint(segmentTarget.Cell.ToVector3Shifted())
                    + (snapshot != null && snapshot.HasWildMissOffset
                        ? snapshot.WildMissOffset.Yto0() : Vector3.zero)
                : Vector3.zero;
        }

        private static Vector3 ResolveFrozenFinalDestination(LocalTargetInfo finalTarget)
            => finalTarget.IsValid ? NormalizePoint(finalTarget.Cell.ToVector3Shifted()) : Vector3.zero;

        private static Vector3 NormalizePoint(Vector3 point) => point.Yto0();

        private static void AppendAnchorTargets(List<LocalTargetInfo> target, IReadOnlyList<PathAnchor> anchors)
        {
            List<PathAnchor> normalized = NormalizeAnchors(anchors);
            for (int i = 0; i < normalized.Count; i++)
                AppendSegment(target, new LocalTargetInfo(normalized[i].ToCell()));
        }

        private static void AppendFinalTarget(List<LocalTargetInfo> target, bool hasFinal, LocalTargetInfo final)
        { if (hasFinal && final.IsValid) AppendSegment(target, final); }

        private static void AppendSegment(List<LocalTargetInfo> target, LocalTargetInfo segment)
        {
            if (target == null || !segment.IsValid) return;
            if (target.Count > 0 && TargetsEquivalent(target[target.Count - 1], segment)) return;
            target.Add(segment);
        }

        private static int ResolveCurrentLegIndex(RoutePathContext snapshot, int segmentCount)
        {
            if (segmentCount <= 0) return 0;
            int index = snapshot != null ? snapshot.CurrentLegIndex : 0;
            if (index < 0) return 0;
            if (index >= segmentCount) return segmentCount - 1;
            return index;
        }

        private static bool TryResolveSegmentAtIndex(
            IReadOnlyList<LocalTargetInfo> segments, int segmentIndex, out LocalTargetInfo target)
        {
            if (segments == null || segmentIndex < 0 || segmentIndex >= segments.Count)
            { target = LocalTargetInfo.Invalid; return false; }
            target = segments[segmentIndex];
            return target.IsValid;
        }

        private static bool TargetsEquivalent(LocalTargetInfo left, LocalTargetInfo right)
        {
            if (!left.IsValid || !right.IsValid) return !left.IsValid && !right.IsValid;
            if (left.HasThing && right.HasThing) return left.Thing == right.Thing;
            if (!left.HasThing && !right.HasThing) return left.Cell == right.Cell;
            return false;
        }
    }
}
