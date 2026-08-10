using System;
using System.Collections.Generic;
using BDP.Content.PathInput;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.AttackExecution.RangedProtocol.Aim;
using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.PathInput;
using BDP.Core.Projectiles.RangedFlightProtocol.Arrival;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.RoutePath
{
    /// <summary>
    /// 路线引导远程模块。
    /// </summary>
    /// <remarks>
    /// 参与六个阶段：
    ///
    /// 1. Targeting（目标选择） — 玩家 Shift+左键 追加锚点，左键确认最终目标，右键取消。
    /// 2. Preview（预览） — 绘制锚点折线与锚点计数提示。
    /// 3. Confirm（确认） — 冻结锚点链与最终目标，零手动锚点时尝试自动绕障。
    /// 4. Aim（瞄准） — 当前不参与瞄准阶段的具体贡献。
    /// 5. ProjectileInit（投射物初始化） — 按锚点链为每个 emit 设置首段目标与最终目标。
    /// 6. Arrival（到达段） — 逐段推进路径，到达一段末后切换到下一段。
    ///
    /// 核心原理：毒蛇式折线路径——子弹在发射时就拿到一套固定折线路径，
    /// 不再做持续追踪，而是按锚点顺序逐段飞行到最终目标。
    /// </remarks>
    public sealed class RoutePathModule :
        IRangedAttackModuleRuntime,
        ITargetingStageModule,
        IPreviewStageModule,
        IConfirmStageModule,
        IAimStageModule,
        IProjectileInitStageModule,
        IArrivalStageModule
    {
        /// <summary>
        /// 预览阶段用于表示"当前最后一段不通"的红线颜色。
        /// </summary>
        private static readonly Color LegacyBlockedSegmentPreviewColor = Color.red;

        /// <summary>当前运行时实例绑定的配置快照。</summary>
        private RoutePathConfig config;

        /// <summary>当前运行时实例绑定的正式结果标识。</summary>
        private string resultId;

        /// <summary>当前模块所属的单侧来源结果标识。</summary>
        private string sourceResultId;

        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            resultId = context != null ? context.ResultId : null;
            sourceResultId = context != null ? context.SourceResultId : null;
            config = ResolveConfigSnapshot(context);
        }

        void ITargetingStageModule.Contribute(TargetingRecord record)
        {
            if (record == null) return;

            RoutePathState state = record.GetOrCreatePrivateContext<RoutePathState>();
            if (state == null) return;

            EnsureState(state);
            EnableGroundTargeting(record);
            if (record.InputFrame != null && record.InputFrame.PressedButton == TargetingInputButton.Right)
            {
                HandleRightClickCancel(record, state);
                return;
            }

            ApplyCurrentTargetLegality(record, state);
            if (record.InputFrame == null || record.InputFrame.PressedButton != TargetingInputButton.Left) return;

            if (HasShiftModifier(record.InputFrame.Modifiers))
            { HandleAnchorAppend(record, state); return; }

            HandleFinalTargetConfirm(record, state);
        }

        void IPreviewStageModule.Contribute(PreviewRecord record)
        {
            if (record == null) return;

            RoutePathState state = record.GetOrCreatePrivateContext<RoutePathState>();
            if (state == null) return;

            EnsureState(state);
            AppendPreview(record, state);
        }

        void IConfirmStageModule.Contribute(ConfirmRecord record)
        {
            if (record == null) return;

            RoutePathState state = record.GetOrCreatePrivateContext<RoutePathState>();
            if (state == null) return;

            EnsureState(state);
            if (!state.InputState.HasFinalTarget || !state.InputState.FinalTarget.IsValid)
            {
                record.IsAllowed = false;
                record.RejectReason = "BDP_RoutePath_MustSelectFinalFirst".Translate();
                return;
            }

            state.ConfirmedSnapshot.Reset();
            List<PathAnchor> frozenAnchors = RouteSegmentResolver.NormalizeAnchors(state.InputState.Anchors);
            List<PathAnchor> autoLeftAnchors = new List<PathAnchor>();
            List<PathAnchor> autoRightAnchors = new List<PathAnchor>();
            RoutePathSource pathSource = RoutePathSource.Direct;
            if (HasManualAnchors(state))
            {
                pathSource = RoutePathSource.Manual;
            }
            else
            {
                RouteAutoResult autoRouteResult;
                if (!TryResolveAutoRouteForFinalTarget(record.Pawn, null, state, state.InputState.FinalTarget, out autoRouteResult)
                    || !autoRouteResult.Succeeded)
                {
                    record.IsAllowed = false;
                    record.RejectReason = autoRouteResult != null
                        ? autoRouteResult.RejectReason : "BDP_RoutePath_AutoRouteFailed".Translate().ToString();
                    return;
                }

                if (autoRouteResult.Anchors.Count > 0)
                {
                    frozenAnchors = BuildAnchorPoints(autoRouteResult.Anchors);
                    autoLeftAnchors = BuildAnchorPoints(autoRouteResult.LeftAnchors);
                    autoRightAnchors = BuildAnchorPoints(autoRouteResult.RightAnchors);
                    pathSource = RoutePathSource.Auto;
                }
            }

            state.ConfirmedSnapshot.Anchors = RouteSegmentResolver.NormalizeAnchors(frozenAnchors);
            state.ConfirmedSnapshot.AutoLeftAnchors = RouteSegmentResolver.NormalizeAnchors(autoLeftAnchors);
            state.ConfirmedSnapshot.AutoRightAnchors = RouteSegmentResolver.NormalizeAnchors(autoRightAnchors);
            state.ConfirmedSnapshot.HasFinalTarget = true;
            state.ConfirmedSnapshot.FinalTarget = state.InputState.FinalTarget;
            state.ConfirmedSnapshot.FinalIsThing = state.InputState.FinalIsThing;
            state.ConfirmedSnapshot.PathSource = pathSource;

            state.PathSnapshot.Reset();
            LocalTargetInfo firstTarget;
            record.Target = RouteSegmentResolver.TryResolveFirstLegTarget(state.ConfirmedSnapshot, out firstTarget)
                ? firstTarget : state.ConfirmedSnapshot.FinalTarget;
            record.SemanticTarget = state.InputState.FinalTarget;
        }

        void IAimStageModule.Contribute(in AimStageContext context, AimContribution contribution)
        {
            // 当前路线引导模块不参与瞄准阶段的贡献，保留接口以备后续扩展。
        }

        void IProjectileInitStageModule.Contribute(in ProjectileInitStageContext context, ProjectileInitContribution contribution)
        {
            if (contribution == null) return;

            RoutePathState state = context.GetPrivateContext<RoutePathState>();
            RouteConfirmedSnapshot confirmedSnapshot = state != null ? state.ConfirmedSnapshot : null;
            if (state == null || confirmedSnapshot == null || !confirmedSnapshot.HasFinalTarget) return;

            RouteSegmentResolver.PopulatePathSnapshot(
                state.PathSnapshot, confirmedSnapshot,
                ResolveArrivalTolerance(), ResolveIntermediateSpreadRadius(),
                ResolveFinalSpreadRadius(), ResolveHighAccuracySpreadScale(),
                ResolveSpreadSafetyShrinkSteps());
            state.PathSnapshot.AssignedEmitIndex = -1;

            for (int emitIndex = 0; emitIndex < context.EmitCount; emitIndex++)
            {
                if (!ShouldApplyToEmit(context, emitIndex)) continue;

                int roundEmitSequence = context.EmitSequenceBase + emitIndex;
                RouteConfirmedSnapshot emitSnapshot = BuildConfirmedSnapshotForEmit(confirmedSnapshot, roundEmitSequence);
                LocalTargetInfo firstTarget;
                if (!RouteSegmentResolver.TryResolveFirstLegTarget(emitSnapshot, out firstTarget)) continue;

                ProjectileInitPlanContribution planContribution = new ProjectileInitPlanContribution
                {
                    EmitIndex = emitIndex,
                    HasOverrideLaunchTarget = firstTarget.IsValid,
                    OverrideLaunchTarget = firstTarget,
                    HasOverrideAimTarget = confirmedSnapshot.FinalTarget.IsValid,
                    OverrideAimTarget = confirmedSnapshot.FinalTarget,
                    HasOverrideCurrentTarget = confirmedSnapshot.FinalTarget.IsValid,
                    OverrideCurrentTarget = confirmedSnapshot.FinalTarget
                };
                contribution.PlanContributions.Add(planContribution);
            }
        }

        void IArrivalStageModule.Contribute(in ArrivalStageContext context, ArrivalContribution contribution)
        {
            if (contribution == null) return;

            RoutePathState state = context.GetPrivateContext<RoutePathState>();
            EnsurePathSnapshotForEmit(state, context.EmitIndex);
            if (state == null
                || state.PathSnapshot == null
                || !state.PathSnapshot.HasFinalTarget
                || context.Projectile == null)
            {
                return;
            }

            if (!RouteSegmentResolver.TryResolveCurrentLegTarget(state.PathSnapshot, out _)) return;

            if (!RouteSegmentResolver.TryResolveContinuation(
                    state.PathSnapshot,
                    context.Map,
                    context.Projectile.ExactPosition,
                    context.AccuracySnapshot,
                    context.AttackInstanceId,
                    context.ResultId,
                    context.EmitIndex,
                    out bool advanceLeg,
                    out LocalTargetInfo nextLegTarget,
                    out ProjectileFlightPathSnapshot nextSnapshot))
            {
                return;
            }

            if (advanceLeg)
            {
                if (!RouteSegmentResolver.TryAdvanceLeg(state.PathSnapshot))
                {
                    return;
                }
            }

            contribution.HasOverrideContinueFlight = true;
            contribution.OverrideContinueFlight = true;
            contribution.HasNextDestination = true;
            contribution.NextDestination = nextSnapshot.End;
            contribution.HasNextTarget = true;
            contribution.NextTarget = nextLegTarget;
            contribution.HasNextFlightPathSnapshot = true;
            contribution.NextFlightPathSnapshot = nextSnapshot;
        }

        private bool ShouldApplyToEmit(in ProjectileInitStageContext context, int emitIndex)
        {
            if (string.IsNullOrWhiteSpace(sourceResultId)) return true;
            if (!context.TryGetEmitSourceResultId(emitIndex, out string emitSourceResultId)) return true;
            return string.Equals(sourceResultId, emitSourceResultId, StringComparison.Ordinal);
        }

        private void HandleAnchorAppend(TargetingRecord record, RoutePathState state)
        {
            if (record.InputFrame == null || !record.InputFrame.SelectedTarget.IsValid)
            { record.AdvanceDecision.Kind = TargetingAdvanceKind.Reject; record.AdvanceDecision.Reason = "BDP_RoutePath_NoAnchorsAvailable".Translate(); return; }

            if (state.InputState.Anchors.Count >= ResolveMaxAnchors())
            { record.AdvanceDecision.Kind = TargetingAdvanceKind.Reject; record.AdvanceDecision.Reason = "BDP_RoutePath_MaxAnchorsReached".Translate(); return; }

            string rejectReason;
            if (!TryValidateAnchorCandidate(record, state, record.InputFrame.SelectedTarget, out rejectReason))
            { record.AdvanceDecision.Kind = TargetingAdvanceKind.Reject; record.AdvanceDecision.Reason = rejectReason; return; }

            state.InputState.Anchors.Add(PathAnchor.FromCell(record.InputFrame.SelectedTarget.Cell));
            state.InputState.HasFinalTarget = false;
            state.InputState.FinalTarget = LocalTargetInfo.Invalid;
            state.InputState.FinalIsThing = false;
            record.InputState.StepIndex = state.InputState.Anchors.Count;
            record.AdvanceDecision.Kind = TargetingAdvanceKind.Continue;
            record.AdvanceDecision.Reason = null;
        }

        private void HandleFinalTargetConfirm(TargetingRecord record, RoutePathState state)
        {
            LocalTargetInfo selectedTarget = record.InputFrame != null
                ? record.InputFrame.SelectedTarget : LocalTargetInfo.Invalid;
            string rejectReason;
            if (!TryValidateFinalTargetCandidate(record, state, selectedTarget, out rejectReason))
            { record.AdvanceDecision.Kind = TargetingAdvanceKind.Reject; record.AdvanceDecision.Reason = rejectReason; return; }

            state.InputState.HasFinalTarget = true;
            state.InputState.FinalTarget = selectedTarget;
            state.InputState.FinalIsThing = selectedTarget.HasThing;
            record.InputState.StepIndex = state.InputState.Anchors.Count;
            record.AdvanceDecision.Kind = TargetingAdvanceKind.Complete;
            record.AdvanceDecision.Reason = null;
        }

        private static void HandleRightClickCancel(TargetingRecord record, RoutePathState state)
        {
            PathInputHandler.Cancel(state.InputState);
            record.AdvanceDecision.Kind = TargetingAdvanceKind.Cancel;
            record.AdvanceDecision.Reason = null;
        }

        private void AppendPreview(PreviewRecord record, RoutePathState state)
        {
            if (record == null || state?.InputState == null || record.Pawn == null) return;

            RouteInputState inputState = state.InputState;

            // 共享路径：通过 PathInputHandler 构建中性预览数据
            bool isLastBlocked = TryResolvePreviewTargetRejectReason(record, state, out _);
            PathPreviewData preview = PathInputHandler.BuildPreview(
                inputState, record.Pawn, record.Target, isLastBlocked);

            // 手动锚点预览 → 委托给共享渲染器（Deferred 模式：产出 DrawItems）
            if (HasManualAnchors(state))
            {
                PathPreviewRenderer.AppendToRecord(record, preview);

                // 有手动锚点时走共享路径，跳过自动绕障逻辑
            }
            else if (record.Target.IsValid)
            {
                // 无手动锚点 → 尝试自动绕障预览（毒蛇专属逻辑）
                Vector3 lastPoint = record.Pawn.DrawPos;
                RouteAutoResult autoRouteResult;
                if (TryResolveAutoRouteForFinalTarget(
                        record.Pawn, record.Verb, state, record.Target, out autoRouteResult)
                    && autoRouteResult.Succeeded && HasAnyAutoPathAnchors(autoRouteResult))
                {
                    AppendAutoRoutePreview(record, lastPoint, record.Target, autoRouteResult);
                }
                else
                {
                    // 自动绕障不可用 → 使用共享预览的末段线
                    PathPreviewRenderer.AppendToRecord(record, preview);
                }
            }

            // 锚点计数标签（毒蛇专属 UI）
            AppendAnchorCountLabel(record, inputState);
        }

        private static void AppendAutoRoutePreview(
            PreviewRecord record, Vector3 startPoint, LocalTargetInfo finalTarget, RouteAutoResult routeResult)
        {
            if (record == null || routeResult == null) return;

            bool drewAnySide = false;
            if (routeResult.LeftAnchors != null && routeResult.LeftAnchors.Count > 0)
            { AppendAutoRoutePreviewPath(record, startPoint, finalTarget, routeResult.LeftAnchors); drewAnySide = true; }
            if (routeResult.RightAnchors != null && routeResult.RightAnchors.Count > 0)
            { AppendAutoRoutePreviewPath(record, startPoint, finalTarget, routeResult.RightAnchors); drewAnySide = true; }
            if (!drewAnySide && routeResult.Anchors != null && routeResult.Anchors.Count > 0)
            { AppendAutoRoutePreviewPath(record, startPoint, finalTarget, routeResult.Anchors); }
        }

        private static void AppendAutoRoutePreviewPath(
            PreviewRecord record, Vector3 startPoint, LocalTargetInfo finalTarget, IReadOnlyList<IntVec3> anchors)
        {
            if (record == null || anchors == null || anchors.Count <= 0) return;
            Vector3 lastPoint = startPoint;
            for (int i = 0; i < anchors.Count; i++)
            { AddLine(record, lastPoint, anchors[i].ToVector3Shifted(), Color.white); lastPoint = anchors[i].ToVector3Shifted(); }
            if (finalTarget.IsValid) AddLine(record, lastPoint, finalTarget.CenterVector3, Color.white);
        }

        private Color ResolvePreviewLastSegmentColor(PreviewRecord record, RoutePathState state)
        {
            if (record == null || record.Pawn == null || !record.Target.IsValid) return Color.white;
            return TryResolvePreviewTargetRejectReason(record, state, out _) ? LegacyBlockedSegmentPreviewColor : Color.white;
        }

        private bool TryResolvePreviewTargetRejectReason(PreviewRecord record, RoutePathState state, out string rejectReason)
        {
            rejectReason = null;
            if (record == null || record.Pawn == null || !record.Target.IsValid) return false;
            if (IsPreviewingAnchorCandidate(state) && !TryValidateAnchorCell(record.Pawn.Map, record.Target, out rejectReason)) return true;
            return !TryValidatePreviewFinalRouteCandidate(record, state, record.Target, out rejectReason);
        }

        private bool TryValidatePreviewFinalRouteCandidate(
            PreviewRecord record, RoutePathState state, LocalTargetInfo target, out string rejectReason)
        {
            if (HasManualAnchors(state))
                return TryValidatePreviewSegmentCandidate(record, state, target, out rejectReason);

            RouteAutoResult autoRouteResult;
            if (TryResolveAutoRouteForFinalTarget(record != null ? record.Pawn : null, record != null ? record.Verb : null,
                    state, target, out autoRouteResult) && autoRouteResult.Succeeded)
            { rejectReason = null; return true; }
            rejectReason = autoRouteResult != null ? autoRouteResult.RejectReason : "BDP_RoutePath_AutoRouteFailed".Translate().ToString();
            return false;
        }

        private bool IsPreviewingAnchorCandidate(RoutePathState state)
        {
            TargetingInputRuntimeFacts runtimeFacts = TargetingInputRuntimeScope.Current;
            return runtimeFacts != null && HasShiftModifier(runtimeFacts.Modifiers)
                && state != null && state.InputState != null
                && state.InputState.Anchors.Count < ResolveMaxAnchors();
        }

        private static bool TryValidatePreviewSegmentCandidate(
            PreviewRecord record, RoutePathState state, LocalTargetInfo target, out string rejectReason)
        {
            return TryValidateCurrentSegmentLineOfSight(record != null ? record.Pawn : null,
                record != null ? record.Verb : null, state, target, out rejectReason);
        }

        private static bool TryValidateCurrentSegmentLineOfSight(
            Pawn pawn, Verb verb, RoutePathState state, LocalTargetInfo target, out string rejectReason)
        {
            rejectReason = null;
            if (pawn == null || !target.IsValid) { rejectReason = "BDP_RoutePath_CurrentLegTargetInvalid".Translate(); return false; }
            Map map = pawn.Map;
            if (map == null) { rejectReason = "BDP_RoutePath_MapInvalid".Translate(); return false; }

            IntVec3 originCell = ResolveCurrentSegmentOriginCell(pawn, verb, state);
            IntVec3 targetCell = target.Cell;
            if (!originCell.IsValid || !originCell.InBounds(map) || !targetCell.IsValid || !targetCell.InBounds(map))
            { rejectReason = "BDP_RoutePath_CurrentLegInvalid".Translate(); return false; }
            if (!GenSight.LineOfSight(originCell, targetCell, map))
            { rejectReason = "BDP_RoutePath_CurrentLegInvalid".Translate(); return false; }
            return true;
        }

        private void AppendAnchorCountLabel(PreviewRecord record, RouteInputState inputState)
        {
            if (record == null || inputState == null || inputState.Anchors == null || inputState.Anchors.Count <= 0) return;
            record.DrawItems.Add(new PreviewDrawItem
            { Kind = PreviewDrawItemKind.Label, Label = "BDP_RoutePath_AnchorCount".Translate(inputState.Anchors.Count, ResolveMaxAnchors()) });
        }

        private static void AddLine(PreviewRecord record, Vector3 start, Vector3 end, Color color)
            => record.DrawItems.Add(new PreviewDrawItem { Kind = PreviewDrawItemKind.Line, Start = start, End = end, Color = color });

        private void ApplyCurrentTargetLegality(TargetingRecord record, RoutePathState state)
        {
            if (record == null) return;
            LocalTargetInfo candidate = ResolveCurrentCandidate(record);
            if (!candidate.IsValid) { record.HasCurrentTargetLegalityOverride = false; record.CurrentTargetIsLegal = true; record.CurrentTargetRejectReason = null; return; }

            string rejectReason;
            bool isLegal = EvaluateCurrentCandidateLegality(record, state, candidate, out rejectReason);
            record.HasCurrentTargetLegalityOverride = true;
            record.CurrentTargetIsLegal = isLegal;
            record.CurrentTargetRejectReason = rejectReason;
        }

        private static LocalTargetInfo ResolveCurrentCandidate(TargetingRecord record)
        {
            if (record?.InputFrame == null) return LocalTargetInfo.Invalid;
            if (record.InputFrame.SelectedTarget.IsValid) return record.InputFrame.SelectedTarget;
            return record.InputFrame.HoveredTarget;
        }

        private bool EvaluateCurrentCandidateLegality(
            TargetingRecord record, RoutePathState state, LocalTargetInfo target, out string rejectReason)
        {
            bool isAnchorCandidate = record != null && record.InputFrame != null
                && HasShiftModifier(record.InputFrame.Modifiers)
                && state != null && state.InputState.Anchors.Count < ResolveMaxAnchors();
            return isAnchorCandidate
                ? TryValidateAnchorCandidate(record, state, target, out rejectReason)
                : TryValidateFinalTargetCandidate(record, state, target, out rejectReason);
        }

        private bool TryValidateFinalTargetCandidate(
            TargetingRecord record, RoutePathState state, LocalTargetInfo target, out string rejectReason)
        {
            if (!TryValidateFinalTarget(record, state, target, out rejectReason)) return false;
            if (HasManualAnchors(state)) return TryValidateSegmentCandidate(record, state, target, out rejectReason);

            RouteAutoResult autoRouteResult;
            if (TryResolveAutoRouteForFinalTarget(record != null ? record.Pawn : null, record != null ? record.Verb : null,
                    state, target, out autoRouteResult) && autoRouteResult.Succeeded)
            { rejectReason = null; return true; }
            rejectReason = autoRouteResult != null ? autoRouteResult.RejectReason : "BDP_RoutePath_AutoRouteFailed".Translate().ToString();
            return false;
        }

        private static bool TryValidateAnchorCandidate(
            TargetingRecord record, RoutePathState state, LocalTargetInfo target, out string rejectReason)
        { return TryValidateAnchorCell(record, target, out rejectReason) && TryValidateSegmentCandidate(record, state, target, out rejectReason); }

        private static bool TryValidateAnchorCell(TargetingRecord record, LocalTargetInfo target, out string rejectReason)
        {
            rejectReason = null;
            if (record == null || !target.IsValid) { rejectReason = "BDP_RoutePath_CurrentLegTargetInvalid".Translate(); return false; }
            Map map = record.Pawn != null ? record.Pawn.Map : record.Verb?.Caster?.Map;
            return TryValidateAnchorCell(map, target, out rejectReason);
        }

        private static bool TryValidateAnchorCell(Map map, LocalTargetInfo target, out string rejectReason)
        {
            // 委托给通用路径输入器的基础格校验
            rejectReason = null;
            if (map == null) { rejectReason = "BDP_RoutePath_MapInvalid".Translate(); return false; }
            if (!PathInputHandler.IsValidAnchorCell(map, target.Cell))
            { rejectReason = "BDP_RoutePath_AnchorNotWalkable".Translate(); return false; }
            return true;
        }

        private bool TryResolveAutoRouteForFinalTarget(
            Pawn pawn, Verb verb, RoutePathState state, LocalTargetInfo target, out RouteAutoResult routeResult)
        {
            routeResult = null;
            if (HasManualAnchors(state)) return false;
            if (pawn == null || pawn.Map == null || !target.IsValid)
            { routeResult = RouteAutoResult.Failure("BDP_RoutePath_AutoRouteOriginTargetInvalid".Translate()); return true; }

            IntVec3 originCell = ResolveCurrentSegmentOriginCell(pawn, verb, state);
            if (!originCell.IsValid || !originCell.InBounds(pawn.Map) || !target.Cell.InBounds(pawn.Map))
            { routeResult = RouteAutoResult.Failure("BDP_RoutePath_AutoRouteOriginTargetInvalid".Translate()); return true; }
            if (GenSight.LineOfSight(originCell, target.Cell, pawn.Map))
            { routeResult = RouteAutoResult.Success(null); return true; }

            routeResult = RouteAutoResolver.TryResolve(pawn.Map, originCell, target.Cell, config);
            return true;
        }

        private bool TryValidateFinalTarget(TargetingRecord record, RoutePathState state, LocalTargetInfo target, out string rejectReason)
        {
            if (!target.IsValid) { rejectReason = "BDP_RoutePath_FinalTargetInvalid".Translate(); return false; }
            if (target.HasThing && !config.AllowThingFinal) { rejectReason = "BDP_RoutePath_ThingTargetNotAllowed".Translate(); return false; }
            if (!target.HasThing && !config.AllowGroundFinal) { rejectReason = "BDP_RoutePath_GroundTargetNotAllowed".Translate(); return false; }
            if (!CanAcceptTargetByCurrentParameters(record, target, out rejectReason)) return false;
            if (!IsFinalTargetWithinShooterRange(record, target)) { rejectReason = "BDP_RoutePath_TargetOutOfRange".Translate(); return false; }
            rejectReason = null;
            return true;
        }

        private static bool TryValidateSegmentCandidate(
            TargetingRecord record, RoutePathState state, LocalTargetInfo target, out string rejectReason)
        {
            return TryValidateCurrentSegmentLineOfSight(
                record != null ? record.Pawn : null, record != null ? record.Verb : null, state, target, out rejectReason);
        }

        private static bool CanAcceptTargetByCurrentParameters(TargetingRecord record, LocalTargetInfo target, out string rejectReason)
        {
            rejectReason = null;
            if (record == null || !target.IsValid) { rejectReason = "BDP_RoutePath_FinalTargetInvalid".Translate(); return false; }
            Map map = record.Pawn != null ? record.Pawn.Map : record.Verb?.Caster?.Map;
            if (map == null) { rejectReason = "BDP_RoutePath_MapInvalid".Translate(); return false; }
            if (record.TargetingParameters != null && !record.TargetingParameters.CanTarget(target.ToTargetInfo(map), record.Verb))
            { rejectReason = "BDP_RoutePath_FinalTargetRejected".Translate(); return false; }
            return true;
        }

        private static bool IsFinalTargetWithinShooterRange(TargetingRecord record, LocalTargetInfo target)
        {
            if (record == null || !target.IsValid) return false;
            IntVec3 shooterCell = record.Pawn != null ? record.Pawn.Position
                : record.Verb?.Caster != null ? record.Verb.Caster.Position : IntVec3.Invalid;
            if (!shooterCell.IsValid) return false;
            float range = record.Verb?.verbProps != null ? record.Verb.verbProps.range : float.MaxValue;
            return shooterCell.DistanceTo(target.Cell) <= range;
        }

        private static bool HasManualAnchors(RoutePathState state)
            => state != null && state.InputState != null && state.InputState.Anchors != null && state.InputState.Anchors.Count > 0;

        private static bool HasAnyAutoPathAnchors(RouteAutoResult routeResult)
            => routeResult != null
                && ((routeResult.LeftAnchors != null && routeResult.LeftAnchors.Count > 0)
                    || (routeResult.RightAnchors != null && routeResult.RightAnchors.Count > 0)
                    || (routeResult.Anchors != null && routeResult.Anchors.Count > 0));

        private static void EnsurePathSnapshotForEmit(RoutePathState state, int emitIndex)
        {
            if (state == null || state.ConfirmedSnapshot == null || state.PathSnapshot == null) return;
            if (state.ConfirmedSnapshot.PathSource != RoutePathSource.Auto) return;
            if (state.PathSnapshot.AssignedEmitIndex == emitIndex) return;

            float arrivalTolerance = state.PathSnapshot.ArrivalTolerance > 0f ? state.PathSnapshot.ArrivalTolerance : 0.35f;
            float intermediateSpreadRadius = state.PathSnapshot.IntermediateSpreadRadius;
            float finalSpreadRadius = state.PathSnapshot.FinalSpreadRadius;
            float highAccuracySpreadScale = state.PathSnapshot.HighAccuracySpreadScale;
            int spreadSafetyShrinkSteps = state.PathSnapshot.SpreadSafetyShrinkSteps;
            bool hasFrozenFinalDestination = state.PathSnapshot.HasFrozenFinalDestination;
            Vector3 frozenFinalDestination = state.PathSnapshot.FrozenFinalDestination;
            RouteConfirmedSnapshot emitSnapshot = BuildConfirmedSnapshotForEmit(state.ConfirmedSnapshot, emitIndex);
            RouteSegmentResolver.PopulatePathSnapshot(
                state.PathSnapshot,
                emitSnapshot,
                arrivalTolerance,
                intermediateSpreadRadius,
                finalSpreadRadius,
                highAccuracySpreadScale,
                spreadSafetyShrinkSteps);
            // 自动路线这里只重选本发锚点，不得把发射时冻结的最终落点刷新为目标实时位置。
            state.PathSnapshot.HasFrozenFinalDestination = hasFrozenFinalDestination;
            state.PathSnapshot.FrozenFinalDestination = frozenFinalDestination;
            state.PathSnapshot.AssignedEmitIndex = emitIndex;
        }

        private static RouteConfirmedSnapshot BuildConfirmedSnapshotForEmit(RouteConfirmedSnapshot snapshot, int emitIndex)
        {
            if (snapshot == null) return null;
            RouteConfirmedSnapshot clone = snapshot.CloneTyped();
            if (snapshot.PathSource == RoutePathSource.Auto)
            {
                IReadOnlyList<PathAnchor> selected = SelectAutoPathAnchorsForEmit(snapshot, emitIndex);
                clone.Anchors = RouteSegmentResolver.NormalizeAnchors(CloneAnchorPoints(selected));
            }
            return clone;
        }

        private static IReadOnlyList<PathAnchor> SelectAutoPathAnchorsForEmit(RouteConfirmedSnapshot snapshot, int emitIndex)
        {
            if (snapshot == null) return new List<PathAnchor>();
            bool hasLeft = snapshot.AutoLeftAnchors != null && snapshot.AutoLeftAnchors.Count > 0;
            bool hasRight = snapshot.AutoRightAnchors != null && snapshot.AutoRightAnchors.Count > 0;
            if (hasLeft && hasRight) return emitIndex % 2 == 0 ? snapshot.AutoLeftAnchors : snapshot.AutoRightAnchors;
            if (hasLeft) return snapshot.AutoLeftAnchors;
            if (hasRight) return snapshot.AutoRightAnchors;
            return snapshot.Anchors ?? new List<PathAnchor>();
        }

        private static List<PathAnchor> BuildAnchorPoints(IReadOnlyList<IntVec3> anchorCells)
        {
            List<PathAnchor> anchors = new List<PathAnchor>();
            if (anchorCells == null) return anchors;
            for (int i = 0; i < anchorCells.Count; i++) anchors.Add(PathAnchor.FromCell(anchorCells[i]));
            return anchors;
        }

        private static List<PathAnchor> CloneAnchorPoints(IReadOnlyList<PathAnchor> anchorPoints)
        {
            List<PathAnchor> anchors = new List<PathAnchor>();
            if (anchorPoints == null) return anchors;
            for (int i = 0; i < anchorPoints.Count; i++)
                if (anchorPoints[i] != null) anchors.Add(anchorPoints[i].CloneTyped());
            return anchors;
        }

        private static IntVec3 ResolveCurrentSegmentOriginCell(Pawn pawn, Verb verb, RoutePathState state)
        {
            // 委托给通用路径输入器，再补充 Verb 回退
            IntVec3 baseOrigin = PathInputHandler.ResolveSegmentOriginCell(pawn, state?.InputState);
            if (baseOrigin.IsValid) return baseOrigin;
            return verb?.Caster != null ? verb.Caster.Position : IntVec3.Invalid;
        }

        private static void EnsureState(RoutePathState state)
        {
            if (state == null) return;
            if (state.InputState == null) state.InputState = new RouteInputState();
            if (state.ConfirmedSnapshot == null) state.ConfirmedSnapshot = new RouteConfirmedSnapshot();
            if (state.PathSnapshot == null) state.PathSnapshot = new RoutePathContext();
        }

        private static void EnableGroundTargeting(TargetingRecord record)
        {
            if (record == null) return;
            if (record.TargetingParameters == null) record.TargetingParameters = new TargetingParameters();
            record.TargetingParameters.canTargetLocations = true;
        }

        private static bool HasShiftModifier(TargetingInputModifiers modifiers)
            => PathInputHandler.HasShiftModifier(modifiers);

        private int ResolveMaxAnchors() => config != null && config.MaxAnchors > 0 ? config.MaxAnchors : 8;

        private float ResolveArrivalTolerance() => config != null && config.ArrivalTolerance > 0f ? config.ArrivalTolerance : 0.35f;

        /// <summary>读取中间续段最大散布半径。</summary>
        private float ResolveIntermediateSpreadRadius()
            => Mathf.Max(0f, config != null ? config.IntermediateSpreadRadius : 0.625f);

        /// <summary>读取最终续段最大散布半径。</summary>
        private float ResolveFinalSpreadRadius()
            => Mathf.Max(0f, config != null ? config.FinalSpreadRadius : 0.30f);

        /// <summary>读取高精度情况下仍保留的散布比例。</summary>
        private float ResolveHighAccuracySpreadScale()
            => Mathf.Clamp01(config != null ? config.HighAccuracySpreadScale : 0.25f);

        /// <summary>读取候选散布不安全时的折半收缩次数。</summary>
        private int ResolveSpreadSafetyShrinkSteps()
            => Mathf.Clamp(config != null ? config.SpreadSafetyShrinkSteps : 4, 0, 8);

        private static RoutePathConfig ResolveConfigSnapshot(RangedAttackModuleRuntimeContext context)
        {
            if (context != null && context.Config is RoutePathConfig typedConfig) return typedConfig.CloneTyped();
            return new RoutePathConfig();
        }
    }
}
