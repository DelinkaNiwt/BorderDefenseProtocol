using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Verbs;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 从原版忙碌姿态与 BDP 正式攻击会话读取武器视觉动作阶段。
    /// 该解析器无可变状态，不持久化，也不推进攻击时序。
    /// </summary>
    internal sealed class WeaponVisualStageResolver
    {
        /// <summary>
        /// 为一个常驻视觉条目解析当前动作阶段。
        /// 任何正式身份矛盾都会回退到空闲态，不猜测缺失的攻击真值。
        /// </summary>
        internal WeaponVisualStageSnapshot Resolve(
            Pawn pawn,
            VisualResidentEntry entry,
            TriggerCombatProjectionState combatProjection,
            TriggerVisualRuntimeState visualRuntimeState)
        {
            Stance currentStance = pawn != null && pawn.stances != null
                ? pawn.stances.curStance
                : null;
            Stance_Busy busyStance = currentStance as Stance_Busy;
            BdpVerb_FormalHostShoot hostVerb = busyStance != null
                ? busyStance.verb as BdpVerb_FormalHostShoot
                : null;
            AttackSessionToken token = hostVerb != null ? hostVerb.HostSessionToken : null;

            if (!HasValidFormalSession(pawn, hostVerb, token, combatProjection, visualRuntimeState))
            {
                return CreateIdleSnapshot(token, combatProjection);
            }

            string matchedSourceResultId = ResolveMatchedSourceResultId(
                entry,
                token,
                combatProjection,
                visualRuntimeState);
            if (string.IsNullOrWhiteSpace(matchedSourceResultId))
            {
                return CreateIdleSnapshot(token, combatProjection);
            }

            if (currentStance is Stance_Warmup)
            {
                return CreateSnapshot(
                    WeaponVisualActionStage.Warmup,
                    Mathf.Clamp01(hostVerb.WarmupProgress),
                    Math.Max(0, hostVerb.WarmupTicksLeft),
                    matchedSourceResultId,
                    token);
            }

            if (hostVerb.Bursting)
            {
                return CreateSnapshot(
                    WeaponVisualActionStage.Firing,
                    0f,
                    Math.Max(0, busyStance.ticksLeft),
                    matchedSourceResultId,
                    token);
            }

            if (currentStance is Stance_Cooldown)
            {
                int remainingTicks = Math.Max(0, busyStance.ticksLeft);
                int totalTicks = hostVerb.verbProps != null
                    ? hostVerb.verbProps.AdjustedCooldownTicks(hostVerb, pawn)
                    : 0;
                float progress01 = totalTicks > 0
                    ? Mathf.Clamp01(1f - (float)remainingTicks / totalTicks)
                    : 0f;
                return CreateSnapshot(
                    WeaponVisualActionStage.Cooldown,
                    progress01,
                    remainingTicks,
                    matchedSourceResultId,
                    token);
            }

            return CreateIdleSnapshot(token, combatProjection);
        }

        /// <summary>
        /// 校验姿态中的正式远程宿主、令牌、投影和可用视觉执行态是否属于同一会话。
        /// </summary>
        private static bool HasValidFormalSession(
            Pawn pawn,
            BdpVerb_FormalHostShoot hostVerb,
            AttackSessionToken token,
            TriggerCombatProjectionState combatProjection,
            TriggerVisualRuntimeState visualRuntimeState)
        {
            if (pawn == null
                || hostVerb == null
                || token == null
                || !token.IsValid
                || !token.BelongsTo(pawn)
                || combatProjection == null
                || token.ProjectionVersion != combatProjection.ProjectionVersion)
            {
                return false;
            }

            if (visualRuntimeState == null || !visualRuntimeState.HasExecutionState)
            {
                return true;
            }

            return visualRuntimeState.ProjectionVersion == combatProjection.ProjectionVersion
                && string.Equals(
                    visualRuntimeState.ActiveHostResultId,
                    token.ResultId,
                    StringComparison.Ordinal)
                && string.Equals(
                    visualRuntimeState.AttackInstanceId,
                    token.AttackInstanceId,
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// 从正常施放参与结果或读档恢复令牌中，寻找与视觉条目同芯片的实际来源结果。
        /// </summary>
        private static string ResolveMatchedSourceResultId(
            VisualResidentEntry entry,
            AttackSessionToken token,
            TriggerCombatProjectionState combatProjection,
            TriggerVisualRuntimeState visualRuntimeState)
        {
            if (entry?.SourceReference == null
                || token == null
                || combatProjection?.ResultIndex == null)
            {
                return null;
            }

            List<string> roots = new List<string>();
            if (visualRuntimeState != null
                && visualRuntimeState.HasExecutionState)
            {
                if (visualRuntimeState.ActiveAttackParticipantResultIds == null
                    || visualRuntimeState.ActiveAttackParticipantResultIds.Count == 0)
                {
                    return null;
                }

                for (int i = 0; i < visualRuntimeState.ActiveAttackParticipantResultIds.Count; i++)
                {
                    roots.Add(visualRuntimeState.ActiveAttackParticipantResultIds[i]);
                }
            }
            else
            {
                roots.Add(token.ResultId);
            }

            HashSet<string> visitedResultIds = new HashSet<string>(StringComparer.Ordinal);
            Stack<string> pendingResultIds = new Stack<string>();
            for (int i = roots.Count - 1; i >= 0; i--)
            {
                pendingResultIds.Push(roots[i]);
            }

            while (pendingResultIds.Count > 0)
            {
                string resultId = pendingResultIds.Pop();
                if (string.IsNullOrWhiteSpace(resultId) || !visitedResultIds.Add(resultId))
                {
                    continue;
                }

                if (TryExpandCompositeResult(resultId, combatProjection, pendingResultIds))
                {
                    continue;
                }

                if (combatProjection.ResultIndex.TryGetValue(resultId, out FormalExpressionResult sourceResult)
                    && sourceResult != null
                    && ExpressionSourceReferenceMatcher.AreSameChipInstance(
                        entry.SourceReference,
                        sourceResult.SourceReference))
                {
                    return resultId;
                }
            }

            return null;
        }

        /// <summary>
        /// 若结果是复合结果，则按作者声明顺序把下层来源压入待解析栈。
        /// </summary>
        private static bool TryExpandCompositeResult(
            string resultId,
            TriggerCombatProjectionState combatProjection,
            Stack<string> pendingResultIds)
        {
            if (combatProjection.CompositeReferenceIndex == null
                || !combatProjection.CompositeReferenceIndex.TryGetValue(
                    resultId,
                    out CompositeExpressionReference compositeReference)
                || compositeReference?.SourceResultIds == null
                || compositeReference.SourceResultIds.Count == 0)
            {
                return false;
            }

            for (int i = compositeReference.SourceResultIds.Count - 1; i >= 0; i--)
            {
                pendingResultIds.Push(compositeReference.SourceResultIds[i]);
            }

            return true;
        }

        /// <summary>
        /// 创建携带正式会话身份的非空闲阶段快照。
        /// </summary>
        private static WeaponVisualStageSnapshot CreateSnapshot(
            WeaponVisualActionStage stage,
            float progress01,
            int stageTicksRemaining,
            string matchedSourceResultId,
            AttackSessionToken token)
        {
            return new WeaponVisualStageSnapshot(
                stage,
                Mathf.Clamp01(progress01),
                Math.Max(0, stageTicksRemaining),
                matchedSourceResultId,
                token.ResultId,
                token.AttackInstanceId,
                token.ProjectionVersion);
        }

        /// <summary>
        /// 创建空闲阶段快照，并尽可能保留只读诊断所需的会话标识。
        /// </summary>
        private static WeaponVisualStageSnapshot CreateIdleSnapshot(
            AttackSessionToken token,
            TriggerCombatProjectionState combatProjection)
        {
            return new WeaponVisualStageSnapshot(
                WeaponVisualActionStage.Idle,
                0f,
                0,
                null,
                token != null ? token.ResultId : null,
                token != null ? token.AttackInstanceId : null,
                combatProjection != null ? combatProjection.ProjectionVersion : 0);
        }
    }
}
