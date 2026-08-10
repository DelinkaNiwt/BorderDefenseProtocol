using BDP.Core.Expressions;
using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离解析层。
    /// 只读取 Core 提供的公开表达投影，不接触内部表达对象或 Trigger 投影类型。
    /// </summary>
    internal sealed class CombatBodyEmergencyEscapeResolver
    {
        /// <summary>
        /// 紧急脱离正式被动键。
        /// 执行解析与展示状态解析必须共用同一语义键。
        /// </summary>
        internal const string EmergencyEscapePassiveKey = "EmergencyEscape";

        /// <summary>
        /// 解析当前 Pawn 是否具备紧急脱离附加阶段。
        /// </summary>
        public CombatBodyEmergencyEscapeResolution Resolve(Pawn pawn)
        {
            CombatBodyEmergencyEscapeResolution resolution = new CombatBodyEmergencyEscapeResolution();
            ExpressionPublishedProjectionSnapshot projection =
                ExpressionSurfaceAccess.ResolvePublishedProjection(pawn);
            ExpressionPublishedResultSnapshot result = FindAvailablePassive(projection);
            if (result == null)
            {
                return resolution;
            }

            resolution.IsAvailable = true;
            if (string.Equals(result.CompositeKindKey, "Combo", StringComparison.Ordinal)
                && projection.TryGetCompositeReference(result.ResultId, out ExpressionPublishedCompositeReference composite))
            {
                resolution.SourceReferences = ResolveCompositeSourceReferences(projection, composite);
            }

            if (resolution.SourceReferences == null && result.SourceReference != null)
            {
                resolution.SourceReferences = new List<ExpressionPublishedSourceReference>
                {
                    CloneSourceReference(result.SourceReference)
                };
            }

            return resolution;
        }

        /// <summary>
        /// 从公开投影中读取第一条可用的紧急脱离被动结果。
        /// </summary>
        private static ExpressionPublishedResultSnapshot FindAvailablePassive(
            ExpressionPublishedProjectionSnapshot projection)
        {
            if (projection?.PassiveResultsByKey == null
                || !projection.PassiveResultsByKey.TryGetValue(EmergencyEscapePassiveKey, out IReadOnlyList<ExpressionPublishedResultSnapshot> results)
                || results == null)
            {
                return null;
            }

            for (int i = 0; i < results.Count; i++)
            {
                ExpressionPublishedResultSnapshot result = results[i];
                if (result != null && result.IsAvailable && result.IsPublished)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 依据公开复合引用恢复全部来源槽位引用。
        /// </summary>
        private static List<ExpressionPublishedSourceReference> ResolveCompositeSourceReferences(
            ExpressionPublishedProjectionSnapshot projection,
            ExpressionPublishedCompositeReference composite)
        {
            List<ExpressionPublishedSourceReference> result =
                new List<ExpressionPublishedSourceReference>();
            if (projection == null || composite == null)
            {
                return result;
            }

            AppendSourceReference(result, projection, composite.MainSourceResultId);
            AppendSourceReference(result, projection, composite.SubSourceResultId);
            if (composite.SourceResultIds != null)
            {
                for (int i = 0; i < composite.SourceResultIds.Count; i++)
                {
                    AppendSourceReference(result, projection, composite.SourceResultIds[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 按公开结果标识追加来源槽位引用。
        /// </summary>
        private static void AppendSourceReference(
            List<ExpressionPublishedSourceReference> result,
            ExpressionPublishedProjectionSnapshot projection,
            string sourceResultId)
        {
            if (result == null
                || projection == null
                || string.IsNullOrWhiteSpace(sourceResultId)
                || !projection.TryGetResult(sourceResultId, out ExpressionPublishedResultSnapshot sourceResult)
                || sourceResult?.SourceReference == null)
            {
                return;
            }

            ExpressionPublishedSourceReference source = sourceResult.SourceReference;
            for (int i = 0; i < result.Count; i++)
            {
                ExpressionPublishedSourceReference existing = result[i];
                if (existing != null
                    && existing.ChipThingId == source.ChipThingId
                    && existing.Side == source.Side
                    && existing.SlotIndex == source.SlotIndex)
                {
                    return;
                }
            }

            result.Add(CloneSourceReference(source));
        }

        /// <summary>
        /// 克隆公开来源引用，避免缓存直接持有发布快照对象。
        /// </summary>
        private static ExpressionPublishedSourceReference CloneSourceReference(
            ExpressionPublishedSourceReference sourceReference)
        {
            if (sourceReference == null)
            {
                return null;
            }

            return new ExpressionPublishedSourceReference(
                sourceReference.ChipThingId,
                sourceReference.ChipDefName,
                sourceReference.Side,
                sourceReference.SlotIndex);
        }
    }
}
