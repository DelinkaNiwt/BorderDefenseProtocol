using System;
using System.Collections.Generic;
using UnityEngine;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// Trigger 视觉运行时状态 owner。
    /// 它是动态视觉状态的唯一写入面，避免静态投影被当成运行时真值使用。
    /// </summary>
    internal sealed class TriggerVisualRuntimeStateOwner
    {
        /// <summary>
        /// 当前已发布视觉运行时状态。
        /// </summary>
        private TriggerVisualRuntimeState publishedState;

        /// <summary>
        /// 当前投影版本内按正式表达结果保存的短暂视觉冲量。
        /// 新冲量覆盖同一结果的旧冲量，避免连续受击积累无界状态。
        /// </summary>
        private readonly Dictionary<string, ExpressionVisualImpulse> expressionVisualImpulses =
            new Dictionary<string, ExpressionVisualImpulse>(StringComparer.Ordinal);

        /// <summary>
        /// 构造一个空的视觉运行时状态 owner。
        /// </summary>
        public TriggerVisualRuntimeStateOwner()
        {
            publishedState = TriggerVisualRuntimeState.CreateEmpty(0);
        }

        /// <summary>
        /// 读取当前已发布视觉运行时状态。
        /// </summary>
        internal TriggerVisualRuntimeState PublishedState
        {
            get { return publishedState; }
        }

        /// <summary>
        /// 读取当前已采样装备姿态。
        /// </summary>
        internal EquipmentPoseSample PublishedEquipmentPoseSample
        {
            get { return publishedState != null ? publishedState.EquipmentPoseSample : null; }
        }

        /// <summary>
        /// 在新投影发布时重置视觉运行时状态。
        /// 这会切断旧攻击会话和旧装备姿态对新投影的污染。
        /// </summary>
        internal void ResetForPublishedProjection(int projectionVersion)
        {
            publishedState = TriggerVisualRuntimeState.CreateEmpty(projectionVersion);
            expressionVisualImpulses.Clear();
        }

        /// <summary>为指定正式表达结果发布一次短暂视觉冲量。</summary>
        internal void PublishExpressionVisualImpulse(
            string resultId,
            Vector3 direction,
            int startTick,
            int durationTicks,
            float distance)
        {
            direction.y = 0f;
            if (string.IsNullOrWhiteSpace(resultId)
                || direction == Vector3.zero
                || durationTicks <= 0
                || distance <= 0f)
            {
                return;
            }

            expressionVisualImpulses[resultId] = new ExpressionVisualImpulse
            {
                StartTick = startTick,
                Direction = direction.normalized,
                Distance = distance,
                DurationTicks = durationTicks
            };
        }

        /// <summary>
        /// 解析指定正式表达结果在当前 tick 的视觉冲量位移。
        /// 过期记录在读取时立即移除，不进入长期逐 tick 维护。
        /// </summary>
        internal Vector3 ResolveExpressionVisualImpulseOffset(string resultId, int currentTick)
        {
            ExpressionVisualImpulse impulse;
            if (string.IsNullOrWhiteSpace(resultId)
                || !expressionVisualImpulses.TryGetValue(resultId, out impulse)
                || impulse == null)
            {
                return Vector3.zero;
            }

            if (impulse.IsExpired(currentTick))
            {
                expressionVisualImpulses.Remove(resultId);
                return Vector3.zero;
            }

            return impulse.ResolveOffset(currentTick);
        }

        /// <summary>
        /// 发布当前帧原版装备姿态样本。
        /// 若样本版本不匹配当前投影版本，则拒绝写入。
        /// </summary>
        internal void PublishPoseSample(EquipmentPoseSample sample)
        {
            if (sample == null || sample.ProjectionVersion <= 0)
            {
                return;
            }

            EnsureVersion(sample.ProjectionVersion);
            if (publishedState == null || publishedState.ProjectionVersion != sample.ProjectionVersion)
            {
                return;
            }

            publishedState.EquipmentPoseSample = sample;
        }

        /// <summary>
        /// 发布攻击执行侧当前动态视觉状态。
        /// 它只更新执行标识，不覆盖装备姿态样本。
        /// </summary>
        internal void PublishExecutionState(
            int projectionVersion,
            string attackInstanceId,
            string activeHostResultId,
            IReadOnlyList<string> activeAttackParticipantResultIds,
            IReadOnlyList<string> activeCastResultIds,
            IReadOnlyList<string> activeEmitSourceResultIds)
        {
            if (projectionVersion <= 0)
            {
                return;
            }

            EnsureVersion(projectionVersion);
            if (publishedState == null)
            {
                publishedState = TriggerVisualRuntimeState.CreateEmpty(projectionVersion);
            }

            publishedState.AttackInstanceId = attackInstanceId;
            publishedState.ActiveHostResultId = activeHostResultId;
            publishedState.ActiveAttackParticipantResultIds = CloneList(activeAttackParticipantResultIds);
            publishedState.ActiveCastResultIds = CloneList(activeCastResultIds);
            publishedState.ActiveEmitSourceResultIds = CloneList(activeEmitSourceResultIds);
        }

        /// <summary>
        /// 在攻击实例身份一致时，发布正式发射计划最终保留的整轮参与来源。
        /// </summary>
        internal void PublishAttackParticipants(
            int projectionVersion,
            string attackInstanceId,
            IReadOnlyList<string> activeAttackParticipantResultIds)
        {
            if (publishedState == null
                || publishedState.ProjectionVersion != projectionVersion
                || !string.Equals(
                    publishedState.AttackInstanceId,
                    attackInstanceId,
                    StringComparison.Ordinal))
            {
                return;
            }

            publishedState.ActiveAttackParticipantResultIds = CloneList(activeAttackParticipantResultIds);
        }

        /// <summary>
        /// 清理指定攻击实例持有的动态执行态。
        /// 只有攻击实例和投影版本匹配时才会清理，避免旧 job 清掉新会话。
        /// </summary>
        internal void ClearExecutionState(string attackInstanceId, int projectionVersion)
        {
            if (publishedState == null || publishedState.ProjectionVersion != projectionVersion)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(publishedState.AttackInstanceId)
                && !string.IsNullOrWhiteSpace(attackInstanceId)
                && !string.Equals(publishedState.AttackInstanceId, attackInstanceId, StringComparison.Ordinal))
            {
                return;
            }

            publishedState.AttackInstanceId = null;
            publishedState.ActiveHostResultId = null;
            publishedState.ActiveAttackParticipantResultIds = new List<string>();
            publishedState.ActiveCastResultIds = new List<string>();
            publishedState.ActiveEmitSourceResultIds = new List<string>();
        }

        /// <summary>
        /// 确保当前状态对象绑定到指定投影版本。
        /// </summary>
        private void EnsureVersion(int projectionVersion)
        {
            if (publishedState == null || publishedState.ProjectionVersion != projectionVersion)
            {
                publishedState = TriggerVisualRuntimeState.CreateEmpty(projectionVersion);
            }
        }

        /// <summary>
        /// 复制一份结果标识列表。
        /// </summary>
        private static IReadOnlyList<string> CloneList(IReadOnlyList<string> source)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                {
                    result.Add(source[i]);
                }
            }

            return result;
        }
    }
}
