using System.Collections.Generic;
using BDP.Core.Information;
using BDP.Core.Expressions;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// Trigger 当前已发布的表现投影状态。
    /// 它是 UI、说明和视觉读取统一消费的正式表现真值表面。
    /// </summary>
    internal sealed class TriggerPresentationState
    {
        /// <summary>
        /// 当前已发布表现投影的版本号。
        /// 它必须与同轮发布的战斗投影版本号保持一致。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 当前已发布的激活芯片统一信息模型集合。
        /// 每枚芯片各自保留身份和来源链，避免多枚芯片的同名字段相互覆盖。
        /// </summary>
        public IReadOnlyList<ChipInformationModel> ChipInformations { get; set; }

        /// <summary>已发布组合结果按结果 Id 索引的中性功能信息。</summary>
        public IReadOnlyDictionary<string, ChipFeatureResolutionResult> ComboFeatureInformations { get; set; }

        /// <summary>
        /// 当前已发布的手动入口投影。
        /// 手动入口按钮和相关 UI 读取都应直接消费它。
        /// </summary>
        public ManualEntryProjection ManualProjection { get; set; }

        /// <summary>
        /// 当前已发布的视觉投影。
        /// 视觉层只读取这份结果，不再回头重建表达快照。
        /// </summary>
        public VisualExpressionProjection VisualProjection { get; set; }

        /// <summary>
        /// 构建一份空的表现投影状态。
        /// 它用于未装备、读档恢复前或显式清空后的稳定兜底读取。
        /// </summary>
        internal static TriggerPresentationState CreateEmpty(int projectionVersion)
        {
            return new TriggerPresentationState
            {
                ProjectionVersion = projectionVersion,
                ChipInformations = new List<ChipInformationModel>(),
                ComboFeatureInformations = new Dictionary<string, ChipFeatureResolutionResult>(),
                ManualProjection = new ManualEntryProjection
                {
                    Groups = new List<ManualEntryProjectionGroup>()
                },
                VisualProjection = new VisualExpressionProjection
                {
                    RelationKind = VisualExpressionRelationKind.None,
                    ResidentEntries = new List<VisualResidentEntry>(),
                    ActiveWeaponChipInstanceCount = 0,
                    HostEquipmentRenderMode = HostEquipmentRenderMode.Keep,
                    ExecutionFocusPolicy = VisualExecutionFocusPolicy.None,
                    MuzzleFollowPolicy = VisualMuzzleFollowPolicy.None
                }
            };
        }
    }
}
