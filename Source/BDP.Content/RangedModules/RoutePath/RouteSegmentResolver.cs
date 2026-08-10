using System.Collections.Generic;
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
        /// <summary>用于隔离中间段与最终段的稳定随机样本。</summary>
        private enum SampleKind
        {
            /// <summary>中间续段偏航样本。</summary>
            Intermediate = 1,

            /// <summary>最终续段收束样本。</summary>
            Final = 2
        }

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
            float arrivalTolerance, float intermediateSpreadRadius,
            float finalSpreadRadius, float highAccuracySpreadScale,
            int spreadSafetyShrinkSteps)
        {
            if (target == null) return;
            target.Reset();
            if (snapshot == null) return;

            target.Anchors = NormalizeAnchors(snapshot.Anchors);
            target.HasFinalTarget = snapshot.HasFinalTarget;
            target.FinalTarget = snapshot.FinalTarget;
            target.FinalIsThing = snapshot.FinalIsThing;
            target.PathSource = snapshot.PathSource;
            target.ArrivalTolerance = arrivalTolerance > 0f ? arrivalTolerance : 0.35f;
            target.IntermediateSpreadRadius = Mathf.Max(0f, intermediateSpreadRadius);
            target.FinalSpreadRadius = Mathf.Max(0f, finalSpreadRadius);
            target.HighAccuracySpreadScale = Mathf.Clamp01(highAccuracySpreadScale);
            target.SpreadSafetyShrinkSteps = Mathf.Clamp(spreadSafetyShrinkSteps, 0, 8);
            target.HasFrozenFinalDestination = snapshot.HasFinalTarget && snapshot.FinalTarget.IsValid;
            target.FrozenFinalDestination = target.HasFrozenFinalDestination
                ? ResolveFrozenFinalDestination(snapshot.FinalTarget) : Vector3.zero;
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
            RoutePathContext snapshot, Map map,
            ProjectileAccuracySnapshot accuracySnapshot, Vector3 start,
            string attackInstanceId, string resultId, int emitIndex)
        {
            List<LocalTargetInfo> segments = BuildSegments(snapshot);
            int legIndex = ResolveCurrentLegIndex(snapshot, segments.Count);
            return TryBuildLegFlightPath(
                snapshot,
                segments,
                legIndex,
                map,
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
        /// 解析当前到达点之后应提交的续段，并区分正常推进与回接当前名义锚点。
        /// </summary>
        public static bool TryResolveContinuation(
            RoutePathContext snapshot,
            Map map,
            Vector3 start,
            ProjectileAccuracySnapshot accuracySnapshot,
            string attackInstanceId,
            string resultId,
            int emitSequence,
            out bool advanceLeg,
            out LocalTargetInfo nextTarget,
            out ProjectileFlightPathSnapshot nextPath)
        {
            advanceLeg = false;
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
                    map,
                    accuracySnapshot,
                    start,
                    attackInstanceId,
                    resultId,
                    emitSequence,
                    out nextPath))
            {
                advanceLeg = true;
                nextTarget = nextNominalTarget;
                return true;
            }

            if (!TryResolveSegmentAtIndex(segments, currentIndex, out LocalTargetInfo currentNominalTarget))
            {
                return false;
            }

            Vector3 currentNominalDestination = ResolveSegmentBaseDestination(
                snapshot,
                currentNominalTarget,
                currentIndex,
                segments.Count);
            if (!IsWithinArrivalTolerance(
                    start,
                    currentNominalDestination,
                    snapshot != null ? snapshot.ArrivalTolerance : 0.35f))
            {
                nextTarget = currentNominalTarget;
                nextPath = ProjectileFlightPathUtility.CreateLinear(
                    NormalizePoint(start),
                    currentNominalDestination);
                return nextPath != null;
            }

            advanceLeg = true;
            nextTarget = nextNominalTarget;
            Vector3 nextNominalDestination = ResolveSegmentBaseDestination(
                snapshot,
                nextNominalTarget,
                nextIndex,
                segments.Count);
            nextPath = ProjectileFlightPathUtility.CreateLinear(
                NormalizePoint(start),
                nextNominalDestination);
            return nextPath != null;
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

        /// <summary>把冻结的原版瞄准概率归一化为路线散布质量。</summary>
        private static float ResolveAccuracyQuality(ProjectileAccuracySnapshot snapshot)
        {
            return snapshot != null && snapshot.IsAvailable
                ? Mathf.Clamp01(snapshot.StandardAimChance)
                : 0.5f;
        }

        /// <summary>按原版精度事实解析当前续段实际使用的散布半径。</summary>
        private static float ResolveSegmentSpreadRadius(
            RoutePathContext snapshot,
            ProjectileAccuracySnapshot accuracySnapshot,
            bool isFinal)
        {
            if (snapshot == null)
            {
                return 0f;
            }

            float quality = ResolveAccuracyQuality(accuracySnapshot);
            float highAccuracyScale = Mathf.Clamp01(snapshot.HighAccuracySpreadScale);
            float spreadScale = Mathf.Lerp(1f, highAccuracyScale, quality);
            float baseRadius = isFinal
                ? snapshot.FinalSpreadRadius
                : snapshot.IntermediateSpreadRadius;
            return Mathf.Max(0f, baseRadius * spreadScale);
        }

        /// <summary>为指定名义段生成通过安全约束的稳定偏航飞行快照。</summary>
        private static bool TryBuildLegFlightPath(
            RoutePathContext snapshot,
            IReadOnlyList<LocalTargetInfo> segments,
            int legIndex,
            Map map,
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
            LocalTargetInfo followingNominalTarget = LocalTargetInfo.Invalid;
            if (!isFinal)
            {
                TryResolveSegmentAtIndex(segments, legIndex + 1, out followingNominalTarget);
            }

            Vector3 nominalDestination = ResolveSegmentBaseDestination(
                snapshot,
                currentTarget,
                legIndex,
                segments.Count);
            float spreadRadius = ResolveSegmentSpreadRadius(snapshot, accuracySnapshot, isFinal);
            Vector3 sampledOffset = SampleSpreadOffset(
                spreadRadius,
                attackInstanceId,
                resultId,
                emitSequence,
                legIndex,
                isFinal ? SampleKind.Final : SampleKind.Intermediate);
            if (!TryResolveSafeSpreadDestination(
                    map,
                    start,
                    nominalDestination,
                    followingNominalTarget,
                    isFinal,
                    sampledOffset,
                    snapshot != null ? snapshot.SpreadSafetyShrinkSteps : 0,
                    out Vector3 destination))
            {
                return false;
            }

            path = ProjectileFlightPathUtility.CreateLinear(
                NormalizePoint(start),
                destination);
            return path != null;
        }

        /// <summary>判断实际到达点是否已经回到当前名义段目标的容差范围。</summary>
        private static bool IsWithinArrivalTolerance(
            Vector3 current,
            Vector3 nominal,
            float tolerance)
        {
            float safeTolerance = tolerance > 0f ? tolerance : 0.35f;
            return (current.Yto0() - nominal.Yto0()).sqrMagnitude
                <= safeTolerance * safeTolerance;
        }

        private static Vector3 ResolveSegmentBaseDestination(
            RoutePathContext snapshot, LocalTargetInfo segmentTarget,
            int segmentIndex, int segmentCount)
        {
            bool isFinal = segmentCount > 0 && segmentIndex == segmentCount - 1;
            if (isFinal && snapshot != null && snapshot.HasFrozenFinalDestination)
                return NormalizePoint(snapshot.FrozenFinalDestination);
            return segmentTarget.IsValid
                ? NormalizePoint(segmentTarget.Cell.ToVector3Shifted()) : Vector3.zero;
        }

        /// <summary>按攻击、发射序号、段序号和样本类型生成稳定的圆盘偏移。</summary>
        private static Vector3 SampleSpreadOffset(
            float spreadRadius,
            string attackInstanceId,
            string resultId,
            int emitIndex,
            int segmentIndex,
            SampleKind sampleKind)
        {
            float safeRadius = Mathf.Max(0f, spreadRadius);
            if (safeRadius <= 0f)
            {
                return Vector3.zero;
            }

            int seed = BuildSpreadSeed(
                attackInstanceId,
                resultId,
                emitIndex,
                segmentIndex,
                sampleKind);
            Rand.PushState(seed);
            try
            {
                float angle = Rand.Value * Mathf.PI * 2f;
                float radius = Mathf.Sqrt(Rand.Value) * safeRadius;
                return new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
            }
            finally
            {
                Rand.PopState();
            }
        }

        /// <summary>构造不依赖全局随机状态的稳定散布种子。</summary>
        private static int BuildSpreadSeed(
            string attackInstanceId,
            string resultId,
            int emitIndex,
            int segmentIndex,
            SampleKind sampleKind)
        {
            int seed = 143921;
            seed = Gen.HashCombineInt(seed, !string.IsNullOrWhiteSpace(attackInstanceId)
                ? GenText.StableStringHash(attackInstanceId) : 0);
            seed = Gen.HashCombineInt(seed, !string.IsNullOrWhiteSpace(resultId)
                ? GenText.StableStringHash(resultId) : 0);
            seed = Gen.HashCombineInt(seed, emitIndex);
            seed = Gen.HashCombineInt(seed, segmentIndex);
            seed = Gen.HashCombineInt(seed, (int)sampleKind);
            return seed;
        }

        /// <summary>折半收缩候选偏移，直到当前段和后继名义段都保持安全。</summary>
        private static bool TryResolveSafeSpreadDestination(
            Map map,
            Vector3 start,
            Vector3 nominalDestination,
            LocalTargetInfo followingNominalTarget,
            bool isFinal,
            Vector3 sampledOffset,
            int shrinkSteps,
            out Vector3 destination)
        {
            int safeShrinkSteps = Mathf.Clamp(shrinkSteps, 0, 8);
            Vector3 offset = sampledOffset.Yto0();
            for (int attempt = 0; attempt <= safeShrinkSteps; attempt++)
            {
                Vector3 candidate = (nominalDestination + offset).Yto0();
                if (IsSafeSpreadDestination(
                        map,
                        start,
                        candidate,
                        followingNominalTarget,
                        isFinal))
                {
                    destination = candidate;
                    return true;
                }

                offset *= 0.5f;
            }

            destination = nominalDestination.Yto0();
            return IsSafeSpreadDestination(
                map,
                start,
                destination,
                followingNominalTarget,
                isFinal);
        }

        /// <summary>检查候选落点不会主动破坏当前段或下一名义段的可通行视线。</summary>
        private static bool IsSafeSpreadDestination(
            Map map,
            Vector3 start,
            Vector3 candidate,
            LocalTargetInfo followingNominalTarget,
            bool isFinal)
        {
            if (map == null)
            {
                return false;
            }

            IntVec3 startCell = start.ToIntVec3();
            IntVec3 candidateCell = candidate.ToIntVec3();
            if (!startCell.IsValid
                || !startCell.InBounds(map)
                || !candidateCell.IsValid
                || !candidateCell.InBounds(map)
                || !candidateCell.CanBeSeenOverFast(map)
                || !GenSight.LineOfSight(startCell, candidateCell, map))
            {
                return false;
            }

            if (isFinal)
            {
                return true;
            }

            IntVec3 followingCell = followingNominalTarget.IsValid
                ? followingNominalTarget.Cell
                : IntVec3.Invalid;
            return followingCell.IsValid
                && followingCell.InBounds(map)
                && GenSight.LineOfSight(candidateCell, followingCell, map);
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
