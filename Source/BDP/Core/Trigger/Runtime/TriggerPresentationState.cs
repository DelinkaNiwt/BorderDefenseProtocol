using System.Collections.Generic;
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
        /// 当前已发布的说明投影。
        /// 常规 UI 读取默认直接消费它，不再自行重建说明结果。
        /// </summary>
        public ExpressionInfoProjection InfoProjection { get; set; }

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
                InfoProjection = new ExpressionInfoProjection
                {
                    Lines = new List<string>(),
                    Entries = new List<ExpressionInfoProjectionEntry>(),
                    ContractDiagnostics = new List<ExpressionContractDiagnosticEntry>(),
                    ChipDefinitionDiagnostics = new List<ChipDefinitionDiagnosticEntry>()
                },
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
