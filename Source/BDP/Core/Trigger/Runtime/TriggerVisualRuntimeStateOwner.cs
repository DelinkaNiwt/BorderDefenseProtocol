using System;
using System.Collections.Generic;

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
            publishedState.ActiveCastResultIds = CloneList(activeCastResultIds);
            publishedState.ActiveEmitSourceResultIds = CloneList(activeEmitSourceResultIds);
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
