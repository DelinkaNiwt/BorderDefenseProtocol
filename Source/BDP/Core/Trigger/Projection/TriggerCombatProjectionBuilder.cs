using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using BDP.Core.VerbHosting;
using Verse;

namespace BDP.Core.Trigger.Projection
{
    /// <summary>
    /// Trigger 战斗投影构建器。
    /// 它只负责把 owner 内部 build input 装配成正式战斗投影，
    /// 不承担 dirty 裁定、发布时序或 formal host 同步。
    /// </summary>
    internal sealed class TriggerCombatProjectionBuilder
    {
        /// <summary>
        /// 从 owner 内部构建输入装配一份正式战斗投影。
        /// </summary>
        internal TriggerCombatProjectionState Build(TriggerProjectionBuildInput buildInput, Pawn ownerPawn, ExpressionService expressionService, int projectionVersion)
        {
            if (buildInput == null || expressionService == null)
            {
                return TriggerCombatProjectionState.CreateEmpty(projectionVersion);
            }

            ExpressionSnapshot snapshot = expressionService.BuildSelectedSnapshot(ownerPawn, buildInput);
            if (snapshot == null)
            {
                return TriggerCombatProjectionState.CreateEmpty(projectionVersion);
            }

            return new TriggerCombatProjectionState
            {
                ProjectionVersion = projectionVersion,
                Snapshot = snapshot,
                ChannelIndex = ExpressionChannelIndexBuilder.Build(snapshot),
                ResultIndex = BuildResultIndex(snapshot),
                CompositeReferenceIndex = BuildCompositeReferenceIndex(snapshot),
                ResultIdToFormalSlot = BuildFormalSlotIndex(snapshot)
            };
        }

        /// <summary>
        /// 为当前快照建立 ResultId 到正式结果的索引。
        /// </summary>
        private static IReadOnlyDictionary<string, FormalExpressionResult> BuildResultIndex(ExpressionSnapshot snapshot)
        {
            Dictionary<string, FormalExpressionResult> resultIndex =
                new Dictionary<string, FormalExpressionResult>();
            if (snapshot?.Results == null)
            {
                return resultIndex;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult result = snapshot.Results[i];
                if (result == null || string.IsNullOrWhiteSpace(result.Id) || resultIndex.ContainsKey(result.Id))
                {
                    continue;
                }

                resultIndex.Add(result.Id, result);
            }

            return resultIndex;
        }

        /// <summary>
        /// 为当前快照建立 CompositeId 到复合引用的索引。
        /// </summary>
        private static IReadOnlyDictionary<string, CompositeExpressionReference> BuildCompositeReferenceIndex(
            ExpressionSnapshot snapshot)
        {
            Dictionary<string, CompositeExpressionReference> compositeReferenceIndex =
                new Dictionary<string, CompositeExpressionReference>();
            if (snapshot?.CompositeReferences == null)
            {
                return compositeReferenceIndex;
            }

            for (int i = 0; i < snapshot.CompositeReferences.Count; i++)
            {
                CompositeExpressionReference reference = snapshot.CompositeReferences[i];
                if (reference == null
                    || string.IsNullOrWhiteSpace(reference.CompositeId)
                    || compositeReferenceIndex.ContainsKey(reference.CompositeId))
                {
                    continue;
                }

                compositeReferenceIndex.Add(reference.CompositeId, reference);
            }

            return compositeReferenceIndex;
        }

        /// <summary>
        /// 为当前快照建立 ResultId 到 formal host 固定槽位的索引。
        /// </summary>
        private static IReadOnlyDictionary<string, BdpFormalVerbHostSlot> BuildFormalSlotIndex(
            ExpressionSnapshot snapshot)
        {
            Dictionary<string, BdpFormalVerbHostSlot> resultIdToFormalSlot =
                new Dictionary<string, BdpFormalVerbHostSlot>();
            if (snapshot?.Results == null)
            {
                return resultIdToFormalSlot;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult result = snapshot.Results[i];
                BdpFormalVerbHostSlot slot;
                if (result == null
                    || string.IsNullOrWhiteSpace(result.Id)
                    || resultIdToFormalSlot.ContainsKey(result.Id)
                    || !TriggerBodyVerbHostManager.TryResolveFormalHostSlot(result, out slot))
                {
                    continue;
                }

                resultIdToFormalSlot.Add(result.Id, slot);
            }

            return resultIdToFormalSlot;
        }
    }
}
