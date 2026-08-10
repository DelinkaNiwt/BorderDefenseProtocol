using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 基于原版近战权重语义为 BDP 多段近战预排每轮 step-tool 序列。
    /// BDP 仍掌控会话推进，只在“这一刀用哪把 Tool”上读取原版权重。
    /// </summary>
    internal static class VanillaCompatibleMeleeToolSelector
    {
        /// <summary>
        /// 为当前近战轮次准备完整的 step-tool 索引序列。
        /// 返回值长度始终与当前轮计划 step 数一致。
        /// </summary>
        public static IReadOnlyList<int> PrepareStepToolSequence(
            Pawn pawn,
            LocalTargetInfo target,
            FormalExpressionResult result,
            IReadOnlyList<MeleeToolSurface> candidateSurfaces,
            int plannedStepCount,
            string attackInstanceId,
            int roundOrdinal)
        {
            List<int> preparedStepToolIndices = new List<int>();
            int stepCount = plannedStepCount > 0 ? plannedStepCount : 1;
            if (candidateSurfaces == null || candidateSurfaces.Count == 0)
            {
                for (int i = 0; i < stepCount; i++)
                {
                    preparedStepToolIndices.Add(0);
                }

                return preparedStepToolIndices;
            }

            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                preparedStepToolIndices.Add(SelectToolIndex(
                    pawn,
                    target,
                    result,
                    candidateSurfaces,
                    attackInstanceId,
                    roundOrdinal,
                    stepIndex));
            }

            return preparedStepToolIndices;
        }

        /// <summary>
        /// 为当前这一刀选择最终要使用的 Tool 索引。
        /// 这里按原版近战权重语义计算，再用稳定种子完成本轮离散采样。
        /// </summary>
        private static int SelectToolIndex(
            Pawn pawn,
            LocalTargetInfo target,
            FormalExpressionResult result,
            IReadOnlyList<MeleeToolSurface> candidateSurfaces,
            string attackInstanceId,
            int roundOrdinal,
            int stepIndex)
        {
            float totalWeight = 0f;
            List<float> weights = new List<float>(candidateSurfaces.Count);
            for (int i = 0; i < candidateSurfaces.Count; i++)
            {
                float weight = ResolveSelectionWeight(candidateSurfaces[i], pawn, target);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0.001f)
            {
                return 0;
            }

            int seed = BuildStableSeed(pawn, target, result, attackInstanceId, roundOrdinal, stepIndex);
            Rand.PushState(seed);
            try
            {
                float threshold = Rand.Value * totalWeight;
                float cumulative = 0f;
                for (int i = 0; i < weights.Count; i++)
                {
                    cumulative += weights[i];
                    if (threshold <= cumulative)
                    {
                        return i;
                    }
                }
            }
            finally
            {
                Rand.PopState();
            }

            return weights.Count - 1;
        }

        /// <summary>
        /// 读取当前 surface 对应的原版近战选择权重。
        /// 这里直接复用 VerbProperties 的原版 helper，再补上一层目标是建筑时的倍率。
        /// </summary>
        private static float ResolveSelectionWeight(
            MeleeToolSurface surface,
            Pawn pawn,
            LocalTargetInfo target)
        {
            if (surface?.VerbProps == null)
            {
                return 0f;
            }

            float weight = surface.VerbProps.AdjustedMeleeSelectionWeight(
                surface.Tool,
                pawn,
                equipment: null,
                hediffCompSource: null,
                comesFromPawnNativeVerbs: true);
            if (target.HasThing && target.Thing.def != null && target.Thing.def.IsEdifice())
            {
                weight *= surface.VerbProps.commonalityVsEdificeFactor;
            }

            return weight > 0f ? weight : 0f;
        }

        /// <summary>
        /// 为当前轮次与当前步生成稳定随机种子。
        /// 这样同一轮的序列在重算前始终可复现，后续再由持久化状态保证跨档稳定。
        /// </summary>
        private static int BuildStableSeed(
            Pawn pawn,
            LocalTargetInfo target,
            FormalExpressionResult result,
            string attackInstanceId,
            int roundOrdinal,
            int stepIndex)
        {
            int seed = 17;
            seed = Gen.HashCombineInt(seed, pawn?.thingIDNumber ?? 0);
            seed = Gen.HashCombineInt(seed, target.HasThing ? target.Thing.thingIDNumber : target.Cell.GetHashCode());
            seed = Gen.HashCombineInt(seed, !string.IsNullOrWhiteSpace(result?.Id) ? GenText.StableStringHash(result.Id) : 0);
            seed = Gen.HashCombineInt(seed, !string.IsNullOrWhiteSpace(attackInstanceId) ? GenText.StableStringHash(attackInstanceId) : 0);
            seed = Gen.HashCombineInt(seed, roundOrdinal);
            seed = Gen.HashCombineInt(seed, stepIndex);
            return seed;
        }
    }
}
