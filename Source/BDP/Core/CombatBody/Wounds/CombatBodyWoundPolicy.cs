using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口策略读取层。
    /// 它只解释当前伤口在 CombatBody 伤口运行时下的有效规则，不改写原版伤口事实。
    /// </summary>
    internal static class CombatBodyWoundPolicy
    {
        /// <summary>
        /// 默认策略 Def 名称。
        /// </summary>
        private const string DefaultPolicyDefName = "BDP_DefaultCombatBodyWoundPolicy";

        /// <summary>
        /// Def 未加载时使用的安全回退策略。
        /// </summary>
        private static readonly CombatBodyWoundPolicyDef fallbackPolicy = new CombatBodyWoundPolicyDef();

        /// <summary>
        /// 读取当前默认伤口策略。
        /// </summary>
        internal static CombatBodyWoundPolicyDef Resolve()
        {
            CombatBodyWoundPolicyDef policy =
                DefDatabase<CombatBodyWoundPolicyDef>.GetNamedSilentFail(DefaultPolicyDefName);

            return policy ?? fallbackPolicy;
        }

        /// <summary>
        /// 判断当前 Pawn 是否处于战斗体激活态。
        /// </summary>
        internal static bool IsCombatBodyActive(Pawn pawn)
        {
            ICombatBodyReader reader = CombatBodySurfaceAccess.ResolveReader(pawn);
            return reader != null && reader.Phase == CombatBodyPhase.Active;
        }

        /// <summary>
        /// 判断当前 Pawn 是否处于战斗体伤口运行时适用阶段。
        /// Active 与 Collapsing 都保留伤口喷射生命周期，最终退出时再统一清理。
        /// </summary>
        internal static bool IsCombatBodyWoundRuntimeApplicable(Pawn pawn)
        {
            ICombatBodyReader reader = CombatBodySurfaceAccess.ResolveReader(pawn);
            return reader != null
                && (reader.Phase == CombatBodyPhase.Active || reader.Phase == CombatBodyPhase.Collapsing);
        }

        /// <summary>
        /// 判断当前伤口是否应压制单伤口流血表现。
        /// </summary>
        internal static bool ShouldSuppressIndividualBleeding(Hediff hediff)
        {
            if (hediff?.pawn == null || !IsSupportedWound(hediff))
            {
                return false;
            }

            CombatBodyWoundPolicyDef policy = Resolve();
            return policy.suppressIndividualBleeding && IsCombatBodyActive(hediff.pawn);
        }

        /// <summary>
        /// 判断当前 Hediff 是否属于第一版支持的原版伤口类型。
        /// </summary>
        internal static bool IsSupportedWound(Hediff hediff)
        {
            return hediff is Hediff_Injury || hediff is Hediff_MissingPart;
        }
    }
}
