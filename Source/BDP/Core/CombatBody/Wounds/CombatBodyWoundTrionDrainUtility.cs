using System;
using System.Collections.Generic;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口 Trion 流失只读查询。
    /// 这里集中解释“当前伤口在当前策略下是否应产生流失”，避免账本发布和 UI 显示各算一套。
    /// </summary>
    internal static class CombatBodyWoundTrionDrainUtility
    {
        /// <summary>
        /// 尝试解析当前伤口的每秒 Trion 流失。
        /// 只有 Pawn 正处于伤口运行时适用阶段、策略启用且结果为正数时才返回 true。
        /// </summary>
        internal static bool TryResolveDrainPerSecond(Hediff hediff, out float drainPerSecond)
        {
            drainPerSecond = 0f;
            if (hediff?.pawn == null || !CombatBodyWoundPolicy.IsSupportedWound(hediff))
            {
                return false;
            }

            if (!CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(hediff.pawn))
            {
                return false;
            }

            CombatBodyWoundPolicyDef policy = CombatBodyWoundPolicy.Resolve();
            if (!HasPositiveDrainScale(policy))
            {
                return false;
            }

            if (hediff is Hediff_MissingPart && !policy.includeMissingPartBleedPotential)
            {
                return false;
            }

            drainPerSecond = ResolveDrainPerSecond(hediff, policy);
            return drainPerSecond > 0f;
        }

        /// <summary>
        /// 尝试从 Trion 账本读取当前伤口正在生效的每秒流失。
        /// 这是显示层使用的真值查询；账本条目到期注销后，这里会返回 false。
        /// </summary>
        internal static bool TryResolvePublishedDrainPerSecond(Hediff hediff, out float drainPerSecond)
        {
            drainPerSecond = 0f;
            if (hediff?.pawn == null || hediff.loadID <= 0 || !CombatBodyWoundPolicy.IsSupportedWound(hediff))
            {
                return false;
            }

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(hediff.pawn);
            IReadOnlyDictionary<TrionDrainKey, float> snapshot = reader?.GetDrainSnapshot();
            if (snapshot == null || !snapshot.TryGetValue(BuildDrainKey(hediff), out drainPerSecond))
            {
                drainPerSecond = 0f;
                return false;
            }

            return drainPerSecond > 0f;
        }

        /// <summary>
        /// 为伤口实例生成稳定 drain 键。
        /// 账本发布和显示查询必须共用这一处，避免到期注销后 UI 仍然读错来源。
        /// </summary>
        internal static TrionDrainKey BuildDrainKey(Hediff hediff)
        {
            return new TrionDrainKey("CombatBody", "Wound", -1, hediff.GetUniqueLoadID());
        }

        /// <summary>
        /// 判断当前策略是否启用并配置了正数流失倍率。
        /// </summary>
        private static bool HasPositiveDrainScale(CombatBodyWoundPolicyDef policy)
        {
            if (policy == null || !policy.trionDrainEnabled)
            {
                return false;
            }

            if (policy.trionDrainMetric == CombatBodyWoundTrionDrainMetric.Severity)
            {
                return policy.trionDrainPerSeverityPerSecond > 0f;
            }

            return policy.trionDrainPerRawBleedRatePerSecond > 0f;
        }

        /// <summary>
        /// 按策略口径计算要发布到 Trion 账本的每秒流失。
        /// </summary>
        private static float ResolveDrainPerSecond(Hediff hediff, CombatBodyWoundPolicyDef policy)
        {
            if (policy.trionDrainMetric == CombatBodyWoundTrionDrainMetric.Severity)
            {
                float severity = Math.Max(0f, hediff.Severity);
                return severity * Math.Max(0f, policy.trionDrainPerSeverityPerSecond);
            }

            float rawBleedRate = CombatBodyWoundRawMetrics.ReadRawBleedRate(hediff);
            return Math.Max(0f, rawBleedRate * policy.trionDrainPerRawBleedRatePerSecond);
        }
    }
}
