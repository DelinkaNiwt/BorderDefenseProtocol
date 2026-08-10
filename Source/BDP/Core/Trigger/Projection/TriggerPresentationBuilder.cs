using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;

namespace BDP.Core.Trigger.Projection
{
    /// <summary>
    /// Trigger 表现投影构建器。
    /// 它只负责把已经选好的表达快照装配成说明、手动入口和视觉投影。
    /// </summary>
    internal sealed class TriggerPresentationBuilder
    {
        /// <summary>
        /// 从一份已经成立的表达快照构建正式表现投影。
        /// </summary>
        internal TriggerPresentationState Build(ExpressionService expressionService, ExpressionSnapshot snapshot, int projectionVersion)
        {
            if (expressionService == null || snapshot == null)
            {
                return TriggerPresentationState.CreateEmpty(projectionVersion);
            }

            return new TriggerPresentationState
            {
                ProjectionVersion = projectionVersion,
                InfoProjection = expressionService.BuildPublishedInfoProjection(snapshot),
                ManualProjection = expressionService.BuildPublishedManualProjection(snapshot),
                VisualProjection = expressionService.BuildPublishedVisualProjection(snapshot)
            };
        }
    }
}
