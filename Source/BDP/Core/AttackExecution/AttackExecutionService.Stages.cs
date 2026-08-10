using System.Collections.Generic;
using BDP.Core.CombatModel;
using BDP.Core.Expressions;
using BDP.Core.VerbHosting;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// AttackExecution 正式服务的内部编排步骤。
    /// 这些步骤只服务单一正式入口，不再以独立流程壳对外存在。
    /// </summary>
    internal sealed partial class AttackExecutionService
    {
        /// <summary>
        /// 尝试为当前已确认请求生成正式攻击编排。
        /// </summary>
        private bool TryBuildPlan(AttackExecutionPreparedContext request, out AttackExecutionPlan plan)
        {
            plan = null;
            if (request?.Request == null || request.Result == null)
            {
                return false;
            }

            List<AttackExecutionCast> casts = BuildCasts(request);
            if (casts == null || casts.Count == 0)
            {
                return false;
            }

            string attackInstanceId = request.AttackInstanceId;
            AttachInstanceId(casts, attackInstanceId);
            List<AttackExecutionGroup> groups = BuildGroups(casts, attackInstanceId);
            plan = new AttackExecutionPlan
            {
                Request = request.Request,
                AttackInstanceId = attackInstanceId,
                Groups = groups,
                Casts = casts,
                InvolvedResultIds = CollectInvolvedResultIds(casts),
                DriveMode = ResolveDriveMode(request, casts),
                GroupCount = groups.Count
            };
            return true;
        }

        /// <summary>
        /// 尝试把当前高层计划映射成正式运行时动作步。
        /// </summary>
        private bool TryBuildSteps(AttackExecutionPreparedContext request, out IReadOnlyList<AttackRuntimeStep> steps)
        {
            steps = null;
            if (request?.Plan?.Groups == null || request.Plan.Groups.Count == 0 || request.Result == null)
            {
                return false;
            }

            List<AttackRuntimeStep> builtSteps = new List<AttackRuntimeStep>();
            int stepIndex = 0;
            for (int i = 0; i < request.Plan.Groups.Count; i++)
            {
                AttackExecutionGroup group = request.Plan.Groups[i];
                if (group?.Casts == null || group.Casts.Count == 0)
                {
                    continue;
                }

                if (request.Result.WeaponMode == WeaponExpressionMode.Ranged)
                {
                    AppendRangedSteps(request, group, builtSteps, ref stepIndex);
                    continue;
                }

                AppendMeleeSteps(request, group, builtSteps, ref stepIndex);
            }

            if (builtSteps.Count == 0)
            {
                return false;
            }

            steps = builtSteps;
            return true;
        }

        /// <summary>
        /// 尝试执行当前已确认请求绑定的正式计划。
        /// </summary>
        private bool TryExecutePrepared(AttackExecutionPreparedContext request)
        {
            if (request?.Plan?.PrimaryGroup == null || request.Result == null)
            {
                return false;
            }

            request.Cursor = CreateInitialCursor(request.Plan);
            if (request.Result.ResultKind != ExpressionResultKind.Verb)
            {
                return false;
            }

            AttackExecutionGroup group = request.Plan.PrimaryGroup;
            if (ShouldEmitImmediateGroup(request, group))
            {
                return effectEmitter != null && effectEmitter.TryEmitGroup(request, group);
            }

            if (request.Result.WeaponMode == WeaponExpressionMode.Ranged)
            {
                return rangedAttackExecutor != null && rangedAttackExecutor.TryExecute(request);
            }

            if (request.Result.WeaponMode == WeaponExpressionMode.Melee)
            {
                return meleeAttackExecutor != null && meleeAttackExecutor.TryExecute(request);
            }

            return false;
        }

        /// <summary>
        /// 为当前请求构建正式施放动作集合。
        /// </summary>
        private static List<AttackExecutionCast> BuildCasts(AttackExecutionPreparedContext request)
        {
            FormalExpressionResult result = request.Result;
            if (result == null)
            {
                return null;
            }

            if (result.CompositeKind == CompositeExpressionKind.DualWeapon)
            {
                return BuildDualWeaponCasts(request, result);
            }

            return BuildSingleResultCasts(request, result, request.Request.Target, true, 0);
        }

        /// <summary>
        /// 为单条正式结果构建施放动作集合。
        /// </summary>
        private static List<AttackExecutionCast> BuildSingleResultCasts(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result,
            LocalTargetInfo target,
            bool isMainSide,
            int groupStartIndex)
        {
            List<AttackExecutionCast> casts = new List<AttackExecutionCast>();
            if (result == null)
            {
                return casts;
            }

            if (result.WeaponMode == WeaponExpressionMode.Ranged)
            {
                AppendRangedCasts(request, casts, result, target, isMainSide, groupStartIndex);
                return casts;
            }

            if (result.WeaponMode == WeaponExpressionMode.Melee)
            {
                AppendMeleeCasts(casts, result, target, isMainSide, groupStartIndex);
                return casts;
            }

            casts.Add(BuildCast(
                result,
                target,
                groupStartIndex,
                0,
                1,
                0,
                isMainSide,
                new[] { BuildSingleEmit(result, target, groupStartIndex, 0, 1, 0, Vector3.zero, false, 0f, 0f, 0f, 0f) }));
            return casts;
        }

        /// <summary>
        /// 为双持正式结果构建施放动作集合。
        /// </summary>
        private static List<AttackExecutionCast> BuildDualWeaponCasts(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result)
        {
            if (result.WeaponMode == WeaponExpressionMode.Melee)
            {
                return BuildDualMeleeWeaponCasts(request, result);
            }

            return BuildDualRangedWeaponCasts(request, result);
        }

        /// <summary>
        /// 为双持正式结果构建远程施放动作集合。
        /// </summary>
        private static List<AttackExecutionCast> BuildDualRangedWeaponCasts(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result)
        {
            LocalTargetInfo dualSemanticTarget = AttackExecutionSemanticTargetResolver.Resolve(request);
            CompositeExpressionReference reference = FindCompositeReference(request, result);
            if (reference == null)
            {
                AttackExecutionDiagnostics.LogDualRangedPlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    0,
                    null,
                    0,
                    "missing_composite_reference");
                return new List<AttackExecutionCast>();
            }

            string mainSourceResultId = !string.IsNullOrWhiteSpace(reference.MainSourceResultId)
                ? reference.MainSourceResultId
                : reference.SourceResultIds != null && reference.SourceResultIds.Count > 0
                    ? reference.SourceResultIds[0]
                    : null;
            string subSourceResultId = !string.IsNullOrWhiteSpace(reference.SubSourceResultId)
                ? reference.SubSourceResultId
                : reference.SourceResultIds != null && reference.SourceResultIds.Count > 1
                    ? reference.SourceResultIds[1]
                    : null;
            AttackExecutionDiagnostics.LogDualRangedPlanStart(
                request != null ? request.Pawn : null,
                result != null ? result.Id : null,
                mainSourceResultId,
                subSourceResultId,
                request != null ? request.Target : LocalTargetInfo.Invalid,
                dualSemanticTarget);
            if (string.IsNullOrWhiteSpace(mainSourceResultId) || string.IsNullOrWhiteSpace(subSourceResultId))
            {
                AttackExecutionDiagnostics.LogDualRangedPlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    0,
                    null,
                    0,
                    "missing_source_result_id");
                return new List<AttackExecutionCast>();
            }

            FormalExpressionResult mainResult = FindSourceResult(request, mainSourceResultId);
            FormalExpressionResult subResult = FindSourceResult(request, subSourceResultId);
            if (mainResult == null || subResult == null)
            {
                AttackExecutionDiagnostics.LogDualRangedPlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    0,
                    DescribeResultIds(mainResult, subResult),
                    0,
                    "missing_source_result");
                return new List<AttackExecutionCast>();
            }

            List<FormalExpressionResult> survivingResults = FilterDualRangedSidesByLegality(
                request,
                result,
                mainResult,
                subResult,
                dualSemanticTarget);
            if (survivingResults.Count == 0)
            {
                AttackExecutionDiagnostics.LogDualRangedPlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    0,
                    null,
                    0,
                    "no_surviving_side");
                return new List<AttackExecutionCast>();
            }

            if (survivingResults.Count == 1)
            {
                FormalExpressionResult survivingResult = survivingResults[0];
                bool isMainSide = survivingResult == mainResult;
                List<AttackExecutionCast> singleSideCasts = BuildSingleResultCasts(request, survivingResult, dualSemanticTarget, isMainSide, 0);
                AttackExecutionDiagnostics.LogDualRangedPlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    1,
                    DescribeResultIds(survivingResult),
                    singleSideCasts.Count,
                    "single_side_fallback");
                return singleSideCasts;
            }

            List<AttackExecutionCast> mainCasts = BuildSingleResultCasts(request, mainResult, dualSemanticTarget, true, 0);
            List<AttackExecutionCast> subCasts = BuildSingleResultCasts(request, subResult, dualSemanticTarget, false, 0);
            DualExecutionSchedule schedule = result.ExecutionStyle?.Dual != null
                ? result.ExecutionStyle.Dual.Schedule
                : DualExecutionSchedule.None;

            switch (schedule)
            {
                case DualExecutionSchedule.MainThenSub:
                    List<AttackExecutionCast> sequentialCasts = AppendGroupsSequentially(mainCasts, subCasts);
                    AttackExecutionDiagnostics.LogDualRangedPlanResult(
                        request != null ? request.Pawn : null,
                        result != null ? result.Id : null,
                        dualSemanticTarget,
                        2,
                        DescribeResultIds(mainResult, subResult),
                        sequentialCasts.Count,
                        "dual_schedule_main_then_sub");
                    return sequentialCasts;
                case DualExecutionSchedule.Simultaneous:
                    List<AttackExecutionCast> simultaneousCasts = MergeGroupsByIndex(mainCasts, subCasts, true);
                    AttackExecutionDiagnostics.LogDualRangedPlanResult(
                        request != null ? request.Pawn : null,
                        result != null ? result.Id : null,
                        dualSemanticTarget,
                        2,
                        DescribeResultIds(mainResult, subResult),
                        simultaneousCasts.Count,
                        "dual_schedule_simultaneous");
                    return simultaneousCasts;
                case DualExecutionSchedule.MixedRhythm:
                    List<AttackExecutionCast> mixedRhythmCasts = MergeGroupsByIndex(mainCasts, subCasts, false);
                    AttackExecutionDiagnostics.LogDualRangedPlanResult(
                        request != null ? request.Pawn : null,
                        result != null ? result.Id : null,
                        dualSemanticTarget,
                        2,
                        DescribeResultIds(mainResult, subResult),
                        mixedRhythmCasts.Count,
                        "dual_schedule_mixed_rhythm");
                    return mixedRhythmCasts;
                case DualExecutionSchedule.Alternating:
                default:
                    List<AttackExecutionCast> alternatingCasts = InterleaveGroups(mainCasts, subCasts);
                    AttackExecutionDiagnostics.LogDualRangedPlanResult(
                        request != null ? request.Pawn : null,
                        result != null ? result.Id : null,
                        dualSemanticTarget,
                        2,
                        DescribeResultIds(mainResult, subResult),
                        alternatingCasts.Count,
                        "dual_schedule_alternating");
                    return alternatingCasts;
            }
        }

        /// <summary>
        /// 为双持正式结果构建近战施放动作集合。
        /// 近战准入只判断“当前目标能否进入近战追击链”，不要求当前站位已经贴到可打。
        /// </summary>
        private static List<AttackExecutionCast> BuildDualMeleeWeaponCasts(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result)
        {
            LocalTargetInfo dualSemanticTarget = AttackExecutionSemanticTargetResolver.Resolve(request);
            CompositeExpressionReference reference = FindCompositeReference(request, result);
            if (reference == null)
            {
                AttackExecutionDiagnostics.LogDualMeleePlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    0,
                    null,
                    0,
                    "missing_composite_reference");
                return new List<AttackExecutionCast>();
            }

            string mainSourceResultId = !string.IsNullOrWhiteSpace(reference.MainSourceResultId)
                ? reference.MainSourceResultId
                : reference.SourceResultIds != null && reference.SourceResultIds.Count > 0
                    ? reference.SourceResultIds[0]
                    : null;
            string subSourceResultId = !string.IsNullOrWhiteSpace(reference.SubSourceResultId)
                ? reference.SubSourceResultId
                : reference.SourceResultIds != null && reference.SourceResultIds.Count > 1
                    ? reference.SourceResultIds[1]
                    : null;
            AttackExecutionDiagnostics.LogDualMeleePlanStart(
                request != null ? request.Pawn : null,
                result != null ? result.Id : null,
                mainSourceResultId,
                subSourceResultId,
                request != null ? request.Target : LocalTargetInfo.Invalid,
                dualSemanticTarget);
            if (string.IsNullOrWhiteSpace(mainSourceResultId) || string.IsNullOrWhiteSpace(subSourceResultId))
            {
                AttackExecutionDiagnostics.LogDualMeleePlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    0,
                    null,
                    0,
                    "missing_source_result_id");
                return new List<AttackExecutionCast>();
            }

            FormalExpressionResult mainResult = FindSourceResult(request, mainSourceResultId);
            FormalExpressionResult subResult = FindSourceResult(request, subSourceResultId);
            if (mainResult == null || subResult == null)
            {
                AttackExecutionDiagnostics.LogDualMeleePlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    0,
                    DescribeResultIds(mainResult, subResult),
                    0,
                    "missing_source_result");
                return new List<AttackExecutionCast>();
            }

            List<FormalExpressionResult> survivingResults = FilterDualMeleeSidesByLegality(
                request,
                result,
                mainResult,
                subResult,
                dualSemanticTarget);
            if (survivingResults.Count == 0)
            {
                AttackExecutionDiagnostics.LogDualMeleePlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    0,
                    null,
                    0,
                    "no_surviving_side");
                return new List<AttackExecutionCast>();
            }

            if (survivingResults.Count == 1)
            {
                FormalExpressionResult survivingResult = survivingResults[0];
                bool isMainSide = survivingResult == mainResult;
                List<AttackExecutionCast> singleSideCasts = BuildSingleResultCasts(request, survivingResult, dualSemanticTarget, isMainSide, 0);
                AttackExecutionDiagnostics.LogDualMeleePlanResult(
                    request != null ? request.Pawn : null,
                    result != null ? result.Id : null,
                    dualSemanticTarget,
                    1,
                    DescribeResultIds(survivingResult),
                    singleSideCasts.Count,
                    "single_side_fallback");
                return singleSideCasts;
            }

            List<AttackExecutionCast> mainCasts = BuildSingleResultCasts(request, mainResult, dualSemanticTarget, true, 0);
            List<AttackExecutionCast> subCasts = BuildSingleResultCasts(request, subResult, dualSemanticTarget, false, 0);
            DualExecutionSchedule schedule = result.ExecutionStyle?.Dual != null
                ? result.ExecutionStyle.Dual.Schedule
                : DualExecutionSchedule.None;

            switch (schedule)
            {
                case DualExecutionSchedule.MainThenSub:
                    List<AttackExecutionCast> sequentialCasts = AppendGroupsSequentially(mainCasts, subCasts);
                    AttackExecutionDiagnostics.LogDualMeleePlanResult(
                        request != null ? request.Pawn : null,
                        result != null ? result.Id : null,
                        dualSemanticTarget,
                        2,
                        DescribeResultIds(mainResult, subResult),
                        sequentialCasts.Count,
                        "dual_schedule_main_then_sub");
                    return sequentialCasts;
                case DualExecutionSchedule.Simultaneous:
                    List<AttackExecutionCast> simultaneousCasts = MergeGroupsByIndex(mainCasts, subCasts, true);
                    AttackExecutionDiagnostics.LogDualMeleePlanResult(
                        request != null ? request.Pawn : null,
                        result != null ? result.Id : null,
                        dualSemanticTarget,
                        2,
                        DescribeResultIds(mainResult, subResult),
                        simultaneousCasts.Count,
                        "dual_schedule_simultaneous");
                    return simultaneousCasts;
                case DualExecutionSchedule.MixedRhythm:
                    List<AttackExecutionCast> mixedRhythmCasts = MergeGroupsByIndex(mainCasts, subCasts, false);
                    AttackExecutionDiagnostics.LogDualMeleePlanResult(
                        request != null ? request.Pawn : null,
                        result != null ? result.Id : null,
                        dualSemanticTarget,
                        2,
                        DescribeResultIds(mainResult, subResult),
                        mixedRhythmCasts.Count,
                        "dual_schedule_mixed_rhythm");
                    return mixedRhythmCasts;
                case DualExecutionSchedule.Alternating:
                default:
                    List<AttackExecutionCast> alternatingCasts = InterleaveGroups(mainCasts, subCasts);
                    AttackExecutionDiagnostics.LogDualMeleePlanResult(
                        request != null ? request.Pawn : null,
                        result != null ? result.Id : null,
                        dualSemanticTarget,
                        2,
                        DescribeResultIds(mainResult, subResult),
                        alternatingCasts.Count,
                        "dual_schedule_alternating");
                    return alternatingCasts;
            }
        }

        /// <summary>
        /// 追加远程施放动作。
        /// </summary>
        private static void AppendRangedCasts(
            AttackExecutionPreparedContext request,
            List<AttackExecutionCast> casts,
            FormalExpressionResult result,
            LocalTargetInfo target,
            bool isMainSide,
            int groupStartIndex)
        {
            SingleAttackExecutionStyle single = result.ExecutionStyle?.Single;
            RangedExecutionRhythm rhythm = single != null ? single.RangedRhythm : RangedExecutionRhythm.None;
            ResolvedVerbSpec verbSpec = ResolveDeclaredVerbSpec(result);
            int declaredBurstCount = verbSpec != null && verbSpec.BurstShotCount > 0 ? verbSpec.BurstShotCount : 1;
            int declaredBurstInterval = verbSpec != null && verbSpec.TicksBetweenBurstShots > 0 ? verbSpec.TicksBetweenBurstShots : 0;
            bool hasDeclaredOriginSpreadRange = single != null && single.HasOriginSpreadRange;
            float declaredOriginSpreadLateralMin = hasDeclaredOriginSpreadRange
                ? single.OriginSpreadLateralMin
                : 0f;
            float declaredOriginSpreadLateralMax = hasDeclaredOriginSpreadRange
                ? single.OriginSpreadLateralMax
                : 0f;
            float declaredOriginSpreadForwardMin = hasDeclaredOriginSpreadRange
                ? single.OriginSpreadForwardMin
                : 0f;
            float declaredOriginSpreadForwardMax = hasDeclaredOriginSpreadRange
                ? single.OriginSpreadForwardMax
                : 0f;

            if (rhythm == RangedExecutionRhythm.Simultaneous)
            {
                List<AttackExecutionEmit> emits = new List<AttackExecutionEmit>();
                for (int i = 0; i < declaredBurstCount; i++)
                {
                    emits.Add(BuildSingleEmit(
                        result,
                        target,
                        groupStartIndex,
                        0,
                        1,
                        i,
                        Vector3.zero,
                        hasDeclaredOriginSpreadRange,
                        declaredOriginSpreadLateralMin,
                        declaredOriginSpreadLateralMax,
                        declaredOriginSpreadForwardMin,
                        declaredOriginSpreadForwardMax));
                }

                casts.Add(BuildCast(result, target, groupStartIndex, 0, 1, 0, isMainSide, emits));
                return;
            }

            for (int i = 0; i < declaredBurstCount; i++)
            {
                casts.Add(BuildCast(
                    result,
                    target,
                    groupStartIndex + i,
                    0,
                    i + 1,
                    declaredBurstInterval,
                    isMainSide,
                    new[]
                    {
                        BuildSingleEmit(
                            result,
                            target,
                            groupStartIndex + i,
                            0,
                            i + 1,
                            0,
                            Vector3.zero,
                            hasDeclaredOriginSpreadRange,
                            declaredOriginSpreadLateralMin,
                            declaredOriginSpreadLateralMax,
                            declaredOriginSpreadForwardMin,
                            declaredOriginSpreadForwardMax)
                    }));
            }
        }

        /// <summary>
        /// 追加近战施放动作。
        /// </summary>
        private static void AppendMeleeCasts(
            List<AttackExecutionCast> casts,
            FormalExpressionResult result,
            LocalTargetInfo target,
            bool isMainSide,
            int groupStartIndex)
        {
            SingleAttackExecutionStyle single = result.ExecutionStyle?.Single;
            int meleeHitCount = single != null && single.meleeHitCount > 0 ? single.meleeHitCount : 1;
            int interval = single != null && single.meleeHitIntervalTicks > 0 ? single.meleeHitIntervalTicks : 0;
            for (int i = 0; i < meleeHitCount; i++)
            {
                casts.Add(BuildCast(
                    result,
                    target,
                    groupStartIndex + i,
                    0,
                    i + 1,
                    interval,
                    isMainSide,
                    new[]
                    {
                        BuildSingleEmit(
                            result,
                            target,
                            groupStartIndex + i,
                            0,
                            i + 1,
                            0,
                            Vector3.zero,
                            false,
                            0f,
                            0f,
                            0f,
                            0f)
                    }));
            }
        }

        /// <summary>
        /// 构建单个施放动作对象。
        /// </summary>
        private static AttackExecutionCast BuildCast(
            FormalExpressionResult result,
            LocalTargetInfo target,
            int groupIndex,
            int castLocalIndex,
            int castOrdinal,
            int intervalTicksAfter,
            bool isMainSide,
            IReadOnlyList<AttackExecutionEmit> emits)
        {
            return new AttackExecutionCast
            {
                GroupIndex = groupIndex,
                CastLocalIndex = castLocalIndex,
                CastOrdinal = castOrdinal,
                ResultId = result.Id,
                Result = result,
                Target = target,
                SlotKey = result.ExecutionSlotKey,
                WeaponMode = result.WeaponMode,
                IntervalTicksAfter = intervalTicksAfter,
                IsSecondary = result.IsSecondaryAttack,
                IsPrimarySelection = !result.IsSecondaryAttack,
                IsMainSide = isMainSide,
                Emits = emits
            };
        }

        /// <summary>
        /// 构建单个效果实例对象。
        /// </summary>
        private static AttackExecutionEmit BuildSingleEmit(
            FormalExpressionResult result,
            LocalTargetInfo target,
            int groupIndex,
            int castLocalIndex,
            int castOrdinal,
            int emitLocalIndex,
            Vector3 originOffset,
            bool hasOriginSpreadRange,
            float originSpreadLateralMin,
            float originSpreadLateralMax,
            float originSpreadForwardMin,
            float originSpreadForwardMax)
        {
            return new AttackExecutionEmit
            {
                GroupIndex = groupIndex,
                CastLocalIndex = castLocalIndex,
                CastOrdinal = castOrdinal,
                EmitLocalIndex = emitLocalIndex,
                EmitOrdinal = emitLocalIndex + 1,
                ResultId = result.Id,
                Result = result,
                SourceResultId = result.Id,
                ProjectileDef = ResolveDeclaredProjectileDef(result),
                SemanticContext = result != null ? result.SemanticContext : null,
                OriginSide = result != null ? result.ExecutionSlotKey : null,
                Target = target,
                SemanticTarget = target,
                WeaponMode = result.WeaponMode,
                OriginOffset = originOffset,
                HasOriginSpreadRange = hasOriginSpreadRange,
                OriginSpreadLateralMin = originSpreadLateralMin,
                OriginSpreadLateralMax = originSpreadLateralMax,
                OriginSpreadForwardMin = originSpreadForwardMin,
                OriginSpreadForwardMax = originSpreadForwardMax
            };
        }

        /// <summary>
        /// 按组交替合并双侧施放动作。
        /// </summary>
        private static List<AttackExecutionCast> InterleaveGroups(
            List<AttackExecutionCast> mainCasts,
            List<AttackExecutionCast> subCasts)
        {
            List<AttackExecutionCast> merged = new List<AttackExecutionCast>();
            int mainIndex = 0;
            int subIndex = 0;
            int groupIndex = 0;
            while (mainIndex < mainCasts.Count || subIndex < subCasts.Count)
            {
                if (mainIndex < mainCasts.Count)
                {
                    merged.Add(CloneWithGroup(mainCasts[mainIndex], groupIndex++, 0));
                    mainIndex++;
                }

                if (subIndex < subCasts.Count)
                {
                    merged.Add(CloneWithGroup(subCasts[subIndex], groupIndex++, 0));
                    subIndex++;
                }
            }

            return merged;
        }

        /// <summary>
        /// 按顺序拼接双侧施放动作。
        /// </summary>
        private static List<AttackExecutionCast> AppendGroupsSequentially(
            List<AttackExecutionCast> mainCasts,
            List<AttackExecutionCast> subCasts)
        {
            List<AttackExecutionCast> merged = new List<AttackExecutionCast>();
            int groupIndex = 0;
            for (int i = 0; i < mainCasts.Count; i++)
            {
                merged.Add(CloneWithGroup(mainCasts[i], groupIndex++, 0));
            }

            for (int i = 0; i < subCasts.Count; i++)
            {
                merged.Add(CloneWithGroup(subCasts[i], groupIndex++, 0));
            }

            return merged;
        }

        /// <summary>
        /// 按组号并列合并双侧施放动作。
        /// </summary>
        private static List<AttackExecutionCast> MergeGroupsByIndex(
            List<AttackExecutionCast> mainCasts,
            List<AttackExecutionCast> subCasts,
            bool preferStrictPairing)
        {
            List<AttackExecutionCast> merged = new List<AttackExecutionCast>();
            int total = mainCasts.Count > subCasts.Count ? mainCasts.Count : subCasts.Count;
            int groupIndex = 0;
            for (int i = 0; i < total; i++)
            {
                bool hasMain = i < mainCasts.Count;
                bool hasSub = i < subCasts.Count;
                if (!hasMain && !hasSub)
                {
                    continue;
                }

                int castLocalIndex = 0;
                if (hasMain)
                {
                    merged.Add(CloneWithGroup(mainCasts[i], groupIndex, castLocalIndex++));
                }

                if (hasSub)
                {
                    merged.Add(CloneWithGroup(subCasts[i], groupIndex, castLocalIndex));
                }

                if (hasMain || hasSub || preferStrictPairing)
                {
                    groupIndex++;
                }
            }

            return merged;
        }

        /// <summary>
        /// 为合并后的编排克隆施放动作并重写组号。
        /// </summary>
        private static AttackExecutionCast CloneWithGroup(AttackExecutionCast cast, int groupIndex, int castLocalIndex)
        {
            List<AttackExecutionEmit> emits = new List<AttackExecutionEmit>();
            if (cast?.Emits != null)
            {
                for (int i = 0; i < cast.Emits.Count; i++)
                {
                    AttackExecutionEmit emit = cast.Emits[i];
                    if (emit == null)
                    {
                        continue;
                    }

                    emits.Add(new AttackExecutionEmit
                    {
                        AttackInstanceId = emit.AttackInstanceId,
                        GroupIndex = groupIndex,
                        CastLocalIndex = castLocalIndex,
                        CastOrdinal = cast.CastOrdinal,
                        EmitLocalIndex = emit.EmitLocalIndex,
                        EmitOrdinal = emit.EmitOrdinal,
                        ResultId = emit.ResultId,
                        Result = emit.Result,
                        SourceResultId = emit.SourceResultId,
                        ProjectileDef = emit.ProjectileDef,
                        SemanticContext = emit.SemanticContext,
                        OriginSide = emit.OriginSide,
                        Target = emit.Target,
                        SemanticTarget = emit.SemanticTarget,
                        WeaponMode = emit.WeaponMode,
                        OriginOffset = emit.OriginOffset,
                        HasOriginSpreadRange = emit.HasOriginSpreadRange,
                        OriginSpreadLateralMin = emit.OriginSpreadLateralMin,
                        OriginSpreadLateralMax = emit.OriginSpreadLateralMax,
                        OriginSpreadForwardMin = emit.OriginSpreadForwardMin,
                        OriginSpreadForwardMax = emit.OriginSpreadForwardMax
                    });
                }
            }

            return new AttackExecutionCast
            {
                AttackInstanceId = cast.AttackInstanceId,
                GroupIndex = groupIndex,
                CastLocalIndex = castLocalIndex,
                CastOrdinal = cast.CastOrdinal,
                ResultId = cast.ResultId,
                Result = cast.Result,
                Target = cast.Target,
                SlotKey = cast.SlotKey,
                WeaponMode = cast.WeaponMode,
                IntervalTicksAfter = cast.IntervalTicksAfter,
                IsSecondary = cast.IsSecondary,
                IsPrimarySelection = cast.IsPrimarySelection,
                IsMainSide = cast.IsMainSide,
                Emits = emits
            };
        }

        /// <summary>
        /// 把攻击实例标识写入每一个施放动作和效果实例。
        /// </summary>
        private static void AttachInstanceId(List<AttackExecutionCast> casts, string attackInstanceId)
        {
            if (casts == null)
            {
                return;
            }

            for (int i = 0; i < casts.Count; i++)
            {
                AttackExecutionCast cast = casts[i];
                if (cast == null)
                {
                    continue;
                }

                cast.AttackInstanceId = attackInstanceId;
                if (cast.Emits == null)
                {
                    continue;
                }

                for (int j = 0; j < cast.Emits.Count; j++)
                {
                    if (cast.Emits[j] != null)
                    {
                        cast.Emits[j].AttackInstanceId = attackInstanceId;
                    }
                }
            }
        }

        /// <summary>
        /// 把扁平施放动作归并成正式执行组。
        /// </summary>
        private static List<AttackExecutionGroup> BuildGroups(
            IReadOnlyList<AttackExecutionCast> casts,
            string attackInstanceId)
        {
            List<AttackExecutionGroup> groups = new List<AttackExecutionGroup>();
            if (casts == null || casts.Count == 0)
            {
                return groups;
            }

            int currentGroupIndex = -1;
            List<AttackExecutionCast> currentGroupCasts = null;
            for (int i = 0; i < casts.Count; i++)
            {
                AttackExecutionCast cast = casts[i];
                if (cast == null)
                {
                    continue;
                }

                if (currentGroupCasts == null || cast.GroupIndex != currentGroupIndex)
                {
                    if (currentGroupCasts != null)
                    {
                        groups.Add(BuildGroup(attackInstanceId, currentGroupIndex, currentGroupCasts));
                    }

                    currentGroupIndex = cast.GroupIndex;
                    currentGroupCasts = new List<AttackExecutionCast>();
                }

                currentGroupCasts.Add(cast);
            }

            if (currentGroupCasts != null)
            {
                groups.Add(BuildGroup(attackInstanceId, currentGroupIndex, currentGroupCasts));
            }

            return groups;
        }

        /// <summary>
        /// 构建单个正式执行组。
        /// </summary>
        private static AttackExecutionGroup BuildGroup(
            string attackInstanceId,
            int groupIndex,
            List<AttackExecutionCast> casts)
        {
            return new AttackExecutionGroup
            {
                AttackInstanceId = attackInstanceId,
                GroupIndex = groupIndex,
                TimingMode = casts != null && casts.Count > 1
                    ? AttackGroupTimingMode.ImmediateTogether
                    : AttackGroupTimingMode.SequenceInsideGroup,
                ExecutionKind = ResolveGroupExecutionKind(casts),
                Casts = casts,
                DelayAfterGroupTicks = ResolveDelayAfterGroup(casts)
            };
        }

        /// <summary>
        /// 解析当前执行组应通过哪条运行时边界落地。
        /// </summary>
        private static AttackGroupExecutionKind ResolveGroupExecutionKind(IReadOnlyList<AttackExecutionCast> casts)
        {
            if (casts == null || casts.Count == 0)
            {
                return AttackGroupExecutionKind.None;
            }

            for (int i = 0; i < casts.Count; i++)
            {
                AttackExecutionCast cast = casts[i];
                if (cast != null && cast.WeaponMode == WeaponExpressionMode.Ranged)
                {
                    return AttackGroupExecutionKind.VerbSession;
                }
            }

            return AttackGroupExecutionKind.DirectEffect;
        }

        /// <summary>
        /// 读取当前执行组完成后建议等待的 tick。
        /// </summary>
        private static int ResolveDelayAfterGroup(IReadOnlyList<AttackExecutionCast> casts)
        {
            if (casts == null || casts.Count == 0)
            {
                return 0;
            }

            int maxDelay = 0;
            for (int i = 0; i < casts.Count; i++)
            {
                if (casts[i] != null && casts[i].IntervalTicksAfter > maxDelay)
                {
                    maxDelay = casts[i].IntervalTicksAfter;
                }
            }

            return maxDelay;
        }

        /// <summary>
        /// 读取当前复合结果的来源引用。
        /// </summary>
        private static CompositeExpressionReference FindCompositeReference(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result)
        {
            if (request?.CompositeReferenceIndex == null || string.IsNullOrWhiteSpace(result?.Id))
            {
                return null;
            }

            request.CompositeReferenceIndex.TryGetValue(result.Id, out CompositeExpressionReference reference);
            return reference;
        }

        /// <summary>
        /// 在当前快照中按结果标识查找来源结果。
        /// </summary>
        private static FormalExpressionResult FindSourceResult(AttackExecutionPreparedContext request, string resultId)
        {
            if (request?.ResultIndex == null || string.IsNullOrWhiteSpace(resultId))
            {
                return null;
            }

            request.ResultIndex.TryGetValue(resultId, out FormalExpressionResult result);
            return result;
        }

        /// <summary>
        /// 收集当前计划涉及到的结果标识集合。
        /// </summary>
        private static IReadOnlyList<string> CollectInvolvedResultIds(IReadOnlyList<AttackExecutionCast> casts)
        {
            List<string> resultIds = new List<string>();
            if (casts == null)
            {
                return resultIds;
            }

            for (int i = 0; i < casts.Count; i++)
            {
                string resultId = casts[i]?.ResultId;
                if (string.IsNullOrWhiteSpace(resultId) || resultIds.Contains(resultId))
                {
                    continue;
                }

                resultIds.Add(resultId);
            }

            return resultIds;
        }

        /// <summary>
        /// 根据当前请求与完整计划推断推进方式。
        /// </summary>
        private static AttackDriveMode ResolveDriveMode(
            AttackExecutionPreparedContext request,
            IReadOnlyList<AttackExecutionCast> casts)
        {
            if (request != null
                && (request.DispatchIntent == AttackDispatchIntent.ForceTargetOrder
                    || request.DispatchIntent == AttackDispatchIntent.AutoAttackOrder))
            {
                return AttackDriveMode.Continuous;
            }

            return casts != null && casts.Count > 1
                ? AttackDriveMode.Continuous
                : AttackDriveMode.Immediate;
        }

        /// <summary>
        /// 读取当前结果声明时附带的正式 Verb 规格。
        /// </summary>
        private static ResolvedVerbSpec ResolveDeclaredVerbSpec(FormalExpressionResult result)
        {
            return result != null ? result.ResolvedVerbSpec : null;
        }

        /// <summary>
        /// 读取当前结果作者声明的投射物定义。
        /// </summary>
        private static ThingDef ResolveDeclaredProjectileDef(FormalExpressionResult result)
        {
            ResolvedVerbSpec verbSpec = ResolveDeclaredVerbSpec(result);
            return verbSpec != null ? verbSpec.ProjectileDef : null;
        }

        /// <summary>
        /// 远程规则：同组并列 cast 归并成一个运行时动作步。
        /// </summary>
        private static void AppendRangedSteps(
            AttackExecutionPreparedContext request,
            AttackExecutionGroup group,
            List<AttackRuntimeStep> builtSteps,
            ref int stepIndex)
        {
            List<AttackExecutionCast> casts = new List<AttackExecutionCast>();
            List<AttackExecutionEmit> emits = new List<AttackExecutionEmit>();
            int intervalTicksAfter = 0;
            bool isPrimarySelection = false;

            for (int i = 0; i < group.Casts.Count; i++)
            {
                AttackExecutionCast cast = group.Casts[i];
                if (cast == null)
                {
                    continue;
                }

                casts.Add(cast);
                isPrimarySelection |= cast.IsPrimarySelection;
                if (cast.IntervalTicksAfter > intervalTicksAfter)
                {
                    intervalTicksAfter = cast.IntervalTicksAfter;
                }

                if (cast.Emits == null || cast.Emits.Count == 0)
                {
                    emits.Add(CreateFallbackEmit(cast));
                    continue;
                }

                for (int j = 0; j < cast.Emits.Count; j++)
                {
                    AttackExecutionEmit emit = cast.Emits[j];
                    if (emit != null)
                    {
                        emits.Add(emit);
                    }
                }
            }

            if (casts.Count == 0)
            {
                return;
            }

            builtSteps.Add(new AttackRuntimeStep
            {
                AttackInstanceId = request.AttackInstanceId,
                GroupIndex = group.GroupIndex,
                StepIndex = stepIndex++,
                WeaponMode = WeaponExpressionMode.Ranged,
                ExecutionKind = group.ExecutionKind,
                HostResultId = ResolveRangedStepHostResultId(request),
                Target = ResolveStepTarget(request, casts),
                Casts = casts,
                Emits = emits,
                IntervalTicksAfter = intervalTicksAfter,
                IsPrimarySelection = isPrimarySelection
            });
        }

        /// <summary>
        /// 近战规则：每个计划层 cast 保持一条独立运行时动作步。
        /// </summary>
        private static void AppendMeleeSteps(
            AttackExecutionPreparedContext request,
            AttackExecutionGroup group,
            List<AttackRuntimeStep> builtSteps,
            ref int stepIndex)
        {
            for (int i = 0; i < group.Casts.Count; i++)
            {
                AttackExecutionCast cast = group.Casts[i];
                if (cast == null)
                {
                    continue;
                }

                builtSteps.Add(new AttackRuntimeStep
                {
                    AttackInstanceId = request.AttackInstanceId,
                    GroupIndex = group.GroupIndex,
                    StepIndex = stepIndex++,
                    WeaponMode = WeaponExpressionMode.Melee,
                    ExecutionKind = group.ExecutionKind,
                    HostResultId = request.Result.Id,
                    Target = cast.Target.IsValid ? cast.Target : request.Target,
                    Casts = new[] { cast },
                    Emits = cast.Emits,
                    IntervalTicksAfter = cast.IntervalTicksAfter,
                    IsPrimarySelection = cast.IsPrimarySelection
                });
            }
        }

        /// <summary>
        /// 读取当前运行时动作步最终应该绑定的目标。
        /// </summary>
        private static LocalTargetInfo ResolveStepTarget(
            AttackExecutionPreparedContext request,
            List<AttackExecutionCast> casts)
        {
            for (int i = 0; i < casts.Count; i++)
            {
                AttackExecutionCast cast = casts[i];
                if (cast != null && cast.Target.IsValid)
                {
                    return cast.Target;
                }
            }

            return request != null ? request.Target : LocalTargetInfo.Invalid;
        }

        /// <summary>
        /// 解析当前远程动作步应绑定的 formal host（正式宿主壳）身份。
        /// dual 的 formal host 身份始终是复合结果；实际发射侧由 SourceResultId / StepSourceResultIds 表达。
        /// </summary>
        private static string ResolveRangedStepHostResultId(AttackExecutionPreparedContext request)
        {
            if (request?.Result == null || string.IsNullOrWhiteSpace(request.Result.Id))
            {
                return null;
            }

            return request.Result.Id;
        }

        /// <summary>
        /// 为缺 emit 的远程 cast 提供最小兜底发射载荷。
        /// </summary>
        private static AttackExecutionEmit CreateFallbackEmit(AttackExecutionCast cast)
        {
            return new AttackExecutionEmit
            {
                AttackInstanceId = cast.AttackInstanceId,
                GroupIndex = cast.GroupIndex,
                CastLocalIndex = cast.CastLocalIndex,
                CastOrdinal = cast.CastOrdinal,
                EmitLocalIndex = 0,
                EmitOrdinal = 1,
                ResultId = cast.ResultId,
                Result = cast.Result,
                SourceResultId = cast.ResultId,
                ProjectileDef = ResolveDeclaredProjectileDef(cast.Result),
                SemanticContext = cast.Result?.SemanticContext,
                OriginSide = cast.SlotKey,
                Target = cast.Target,
                SemanticTarget = cast.Target,
                WeaponMode = cast.WeaponMode
            };
        }

        /// <summary>
        /// 按每侧正式结果自己的直射语义过滤 dual 远程可执行侧。
        /// 需要 LOS 的侧若对当前语义目标不合法，就在编排层直接裁掉。
        /// </summary>
        private static List<FormalExpressionResult> FilterDualRangedSidesByLegality(
            AttackExecutionPreparedContext request,
            FormalExpressionResult hostResult,
            FormalExpressionResult mainResult,
            FormalExpressionResult subResult,
            LocalTargetInfo semanticTarget)
        {
            List<FormalExpressionResult> survivingResults = new List<FormalExpressionResult>();
            if (CanExecuteDualRangedSide(request, hostResult, "main", mainResult, semanticTarget))
            {
                survivingResults.Add(mainResult);
            }

            if (CanExecuteDualRangedSide(request, hostResult, "sub", subResult, semanticTarget))
            {
                survivingResults.Add(subResult);
            }

            return survivingResults;
        }

        /// <summary>
        /// 按每侧正式结果自己的近战准入语义过滤 dual 近战可执行侧。
        /// 近战只要求能进入后续追击链，不把“当前站位已经贴到目标”当成硬门槛。
        /// </summary>
        private static List<FormalExpressionResult> FilterDualMeleeSidesByLegality(
            AttackExecutionPreparedContext request,
            FormalExpressionResult hostResult,
            FormalExpressionResult mainResult,
            FormalExpressionResult subResult,
            LocalTargetInfo semanticTarget)
        {
            List<FormalExpressionResult> survivingResults = new List<FormalExpressionResult>();
            if (CanExecuteDualMeleeSide(request, hostResult, "main", mainResult, semanticTarget))
            {
                survivingResults.Add(mainResult);
            }

            if (CanExecuteDualMeleeSide(request, hostResult, "sub", subResult, semanticTarget))
            {
                survivingResults.Add(subResult);
            }

            return survivingResults;
        }

        /// <summary>
        /// 判断单侧正式结果在当前语义目标上是否允许进入 dual 执行编排。
        /// 不要求“射手到语义目标必要直射”的侧直接放行；要求必要直射的侧必须能以自己宿主 Verb 命中目标。
        /// </summary>
        private static bool CanExecuteDualRangedSide(
            AttackExecutionPreparedContext request,
            FormalExpressionResult hostResult,
            string side,
            FormalExpressionResult result,
            LocalTargetInfo semanticTarget)
        {
            Pawn pawn = request != null ? request.Pawn : null;
            if (result == null)
            {
                AttackExecutionDiagnostics.LogDualRangedSideLegality(
                    pawn,
                    hostResult != null ? hostResult.Id : null,
                    side,
                    null,
                    semanticTarget,
                    false,
                    false,
                    false,
                    null,
                    false,
                    false,
                    "missing_result");
                return false;
            }

            ResolvedVerbSpec resolvedSpec = result.ResolvedVerbSpec;
            bool requiresDirectTargetLos = resolvedSpec != null && resolvedSpec.RequiresDirectTargetLineOfSight;
            bool requiresVerbLos = resolvedSpec != null
                ? resolvedSpec.RequireLineOfSight
                : result.VerbProps != null && result.VerbProps.requireLineOfSight;
            bool hasBinding = false;
            Verb verb = null;
            bool canHitDirectTarget = false;
            bool allowed = true;
            string reason = "direct_target_los_not_required";
            if (resolvedSpec != null && resolvedSpec.RequiresDirectTargetLineOfSight)
            {
                if (pawn == null || !semanticTarget.IsValid)
                {
                    allowed = false;
                    reason = pawn == null ? "missing_pawn" : "invalid_semantic_target";
                    AttackExecutionDiagnostics.LogDualRangedSideLegality(
                        pawn,
                        hostResult != null ? hostResult.Id : null,
                        side,
                        result,
                        semanticTarget,
                        requiresVerbLos,
                        requiresDirectTargetLos,
                        false,
                        null,
                        false,
                        allowed,
                        reason);
                    return false;
                }

                if (!VerbHostSurfaceAccess.TryGetByResultId(pawn, result.Id, out BdpFormalVerbBinding binding))
                {
                    allowed = false;
                    reason = "binding_missing";
                    AttackExecutionDiagnostics.LogDualRangedSideLegality(
                        pawn,
                        hostResult != null ? hostResult.Id : null,
                        side,
                        result,
                        semanticTarget,
                        requiresVerbLos,
                        requiresDirectTargetLos,
                        false,
                        null,
                        false,
                        allowed,
                        reason);
                    return false;
                }

                hasBinding = true;
                verb = binding.ResolveActiveVerb();
                canHitDirectTarget = verb != null && verb.CanHitTargetFrom(pawn.Position, semanticTarget);
                allowed = canHitDirectTarget;
                reason = verb == null ? "binding_has_no_active_verb" : canHitDirectTarget ? "required_direct_los_pass" : "required_direct_los_blocked";
                AttackExecutionDiagnostics.LogDualRangedSideLegality(
                    pawn,
                    hostResult != null ? hostResult.Id : null,
                    side,
                    result,
                    semanticTarget,
                    requiresVerbLos,
                    requiresDirectTargetLos,
                    hasBinding,
                    verb,
                    canHitDirectTarget,
                    allowed,
                    reason);
                return allowed;
            }

            AttackExecutionDiagnostics.LogDualRangedSideLegality(
                pawn,
                hostResult != null ? hostResult.Id : null,
                side,
                result,
                semanticTarget,
                requiresVerbLos,
                requiresDirectTargetLos,
                hasBinding,
                verb,
                canHitDirectTarget,
                allowed,
                reason);
            return true;
        }

        /// <summary>
        /// 判断单侧正式结果在当前语义目标上是否允许进入 dual 近战执行编排。
        /// 这里要求目标本身是可追击的 thing，并且该侧 formal host 宿主通过最小 ValidateTarget。
        /// </summary>
        private static bool CanExecuteDualMeleeSide(
            AttackExecutionPreparedContext request,
            FormalExpressionResult hostResult,
            string side,
            FormalExpressionResult result,
            LocalTargetInfo semanticTarget)
        {
            Pawn pawn = request != null ? request.Pawn : null;
            if (result == null)
            {
                AttackExecutionDiagnostics.LogDualMeleeSideLegality(
                    pawn,
                    hostResult != null ? hostResult.Id : null,
                    side,
                    null,
                    semanticTarget,
                    false,
                    false,
                    false,
                    "missing_result",
                    null);
                return false;
            }

            bool hasThingTarget = semanticTarget.IsValid && semanticTarget.HasThing;
            if (pawn == null || !hasThingTarget)
            {
                AttackExecutionDiagnostics.LogDualMeleeSideLegality(
                    pawn,
                    hostResult != null ? hostResult.Id : null,
                    side,
                    result,
                    semanticTarget,
                    false,
                    hasThingTarget,
                    false,
                    pawn == null ? "missing_pawn" : "melee_requires_thing_target",
                    null);
                return false;
            }

            if (!VerbHostSurfaceAccess.TryGetByResultId(pawn, result.Id, out BdpFormalVerbBinding binding))
            {
                AttackExecutionDiagnostics.LogDualMeleeSideLegality(
                    pawn,
                    hostResult != null ? hostResult.Id : null,
                    side,
                    result,
                    semanticTarget,
                    false,
                    hasThingTarget,
                    false,
                    "binding_missing",
                    null);
                return false;
            }

            Verb verb = binding.ResolveActiveVerb();
            bool allowed = verb != null
                && verb.Available()
                && verb.ValidateTarget(semanticTarget, false);
            string reason = verb == null
                ? "binding_has_no_active_verb"
                : !verb.Available()
                    ? "active_verb_unavailable"
                    : allowed
                        ? "melee_target_allowed"
                        : "melee_target_rejected";
            AttackExecutionDiagnostics.LogDualMeleeSideLegality(
                pawn,
                hostResult != null ? hostResult.Id : null,
                side,
                result,
                semanticTarget,
                true,
                hasThingTarget,
                allowed,
                reason,
                verb);
            return allowed;
        }

        /// <summary>
        /// 用于诊断输出的结果标识拼接。
        /// </summary>
        private static string DescribeResultIds(params FormalExpressionResult[] results)
        {
            if (results == null || results.Length == 0)
            {
                return null;
            }

            string text = string.Empty;
            for (int i = 0; i < results.Length; i++)
            {
                FormalExpressionResult result = results[i];
                if (result == null || string.IsNullOrWhiteSpace(result.Id))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    text += "|";
                }

                text += result.Id;
            }

            return text;
        }

        /// <summary>
        /// 为当前计划创建初始执行游标。
        /// </summary>
        private static AttackExecutionCursor CreateInitialCursor(AttackExecutionPlan plan)
        {
            AttackExecutionGroup primaryGroup = plan.PrimaryGroup;
            return new AttackExecutionCursor
            {
                AttackInstanceId = plan.AttackInstanceId,
                GroupIndex = primaryGroup != null ? primaryGroup.GroupIndex : 0,
                CastIndex = 0
            };
        }

        /// <summary>
        /// 判断当前执行组是否应直接在本次执行窗口内整组派发。
        /// </summary>
        private static bool ShouldEmitImmediateGroup(AttackExecutionPreparedContext request, AttackExecutionGroup group)
        {
            if (group == null)
            {
                return false;
            }

            if (group.ExecutionKind != AttackGroupExecutionKind.DirectEffect)
            {
                return false;
            }

            if (group.TimingMode == AttackGroupTimingMode.ImmediateTogether)
            {
                return true;
            }

            return request != null
                && request.Plan != null
                && request.Plan.DriveMode == AttackDriveMode.Immediate
                && group.Casts != null
                && group.Casts.Count == 1;
        }
    }
}
