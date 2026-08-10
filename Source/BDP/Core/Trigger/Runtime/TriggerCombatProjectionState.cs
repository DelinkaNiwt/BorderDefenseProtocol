using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.VerbHosting;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// Trigger 当前已发布的战斗投影状态。
    /// 它是运行时消费者统一读取的正式战斗真值表面。
    /// </summary>
    internal sealed class TriggerCombatProjectionState
    {
        /// <summary>
        /// 当前已发布投影的版本号。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 当前投影共享的表达快照。
        /// </summary>
        public ExpressionSnapshot Snapshot { get; set; }

        /// <summary>
        /// 当前投影共享的四类表达并联索引。
        /// </summary>
        public ExpressionChannelIndex ChannelIndex { get; set; }

        /// <summary>
        /// 按 ResultId 建好的正式结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, FormalExpressionResult> ResultIndex { get; set; }

        /// <summary>
        /// 按 CompositeId 建好的复合引用索引。
        /// </summary>
        public IReadOnlyDictionary<string, CompositeExpressionReference> CompositeReferenceIndex { get; set; }

        /// <summary>
        /// 按 ResultId 建好的 formal host 固定槽位索引。
        /// </summary>
        public IReadOnlyDictionary<string, BdpFormalVerbHostSlot> ResultIdToFormalSlot { get; set; }

        /// <summary>
        /// 当前已发布投影是否为空。
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return Snapshot == null
                    || Snapshot.Results == null
                    || Snapshot.Results.Count == 0;
            }
        }

        /// <summary>
        /// 构建一份空的战斗投影状态。
        /// </summary>
        internal static TriggerCombatProjectionState CreateEmpty(int projectionVersion)
        {
            return new TriggerCombatProjectionState
            {
                ProjectionVersion = projectionVersion,
                Snapshot = new ExpressionSnapshot(),
                ChannelIndex = ExpressionChannelIndex.Empty(),
                ResultIndex = new Dictionary<string, FormalExpressionResult>(),
                CompositeReferenceIndex = new Dictionary<string, CompositeExpressionReference>(),
                ResultIdToFormalSlot = new Dictionary<string, BdpFormalVerbHostSlot>()
            };
        }
    }
}
