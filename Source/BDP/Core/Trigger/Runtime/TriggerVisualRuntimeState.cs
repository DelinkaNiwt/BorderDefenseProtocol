using System.Collections.Generic;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// Trigger 视觉运行时动态状态。
    /// 它保存当轮攻击与装备姿态的动态真值，静态视觉政策仍由 VisualExpressionProjection 提供。
    /// </summary>
    internal sealed class TriggerVisualRuntimeState
    {
        /// <summary>
        /// 当前视觉运行时状态绑定的投影版本号。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 当前攻击实例标识。
        /// 没有正在执行的攻击时为空。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前执行宿主结果标识。
        /// 对 dual 入口通常是复合宿主结果。
        /// </summary>
        public string ActiveHostResultId { get; set; }

        /// <summary>
        /// 当前整轮攻击实际参与的结果标识集合。
        /// 它服务跨运行步骤共享的动作阶段，不等于当前 cast 或 emit 焦点。
        /// </summary>
        public IReadOnlyList<string> ActiveAttackParticipantResultIds { get; set; }

        /// <summary>
        /// 当前 cast（施放动作）正在涉及的结果标识集合。
        /// </summary>
        public IReadOnlyList<string> ActiveCastResultIds { get; set; }

        /// <summary>
        /// 当前 emit（效果实例）正在涉及的源结果标识集合。
        /// </summary>
        public IReadOnlyList<string> ActiveEmitSourceResultIds { get; set; }

        /// <summary>
        /// 当前已采样的原版装备姿态。
        /// 绘制和枪口发射共用它作为宿主基准。
        /// </summary>
        public EquipmentPoseSample EquipmentPoseSample { get; set; }

        /// <summary>
        /// 当前状态是否携带有效攻击执行态。
        /// </summary>
        public bool HasExecutionState
        {
            get
            {
                return !string.IsNullOrWhiteSpace(AttackInstanceId)
                    || !string.IsNullOrWhiteSpace(ActiveHostResultId)
                    || (ActiveAttackParticipantResultIds != null && ActiveAttackParticipantResultIds.Count > 0)
                    || (ActiveCastResultIds != null && ActiveCastResultIds.Count > 0)
                    || (ActiveEmitSourceResultIds != null && ActiveEmitSourceResultIds.Count > 0);
            }
        }

        /// <summary>
        /// 构建指定投影版本下的空视觉运行时状态。
        /// </summary>
        public static TriggerVisualRuntimeState CreateEmpty(int projectionVersion)
        {
            return new TriggerVisualRuntimeState
            {
                ProjectionVersion = projectionVersion,
                AttackInstanceId = null,
                ActiveHostResultId = null,
                ActiveAttackParticipantResultIds = new List<string>(),
                ActiveCastResultIds = new List<string>(),
                ActiveEmitSourceResultIds = new List<string>(),
                EquipmentPoseSample = null
            };
        }

        /// <summary>
        /// 判断指定结果是否处于当前执行焦点中。
        /// </summary>
        public bool ContainsActiveCastResult(string resultId)
        {
            return Contains(ActiveCastResultIds, resultId) || Same(ActiveHostResultId, resultId);
        }

        /// <summary>
        /// 判断指定结果是否处于当前 emit 源焦点中。
        /// </summary>
        public bool ContainsActiveEmitSourceResult(string resultId)
        {
            return Contains(ActiveEmitSourceResultIds, resultId);
        }

        /// <summary>
        /// 判断列表中是否包含指定结果标识。
        /// </summary>
        private static bool Contains(IReadOnlyList<string> values, string resultId)
        {
            if (values == null || string.IsNullOrWhiteSpace(resultId))
            {
                return false;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (Same(values[i], resultId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 比较两个正式结果标识是否相同。
        /// </summary>
        private static bool Same(string left, string right)
        {
            return string.Equals(left, right, System.StringComparison.Ordinal);
        }
    }
}
