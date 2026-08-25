using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 在原版范围伤害工作器确认 Pawn 实际受伤后，消费减益模块的命中反馈颜色。
    /// </summary>
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyToPawn")]
    internal static class Patch_DamageWorker_AddInjury_BdpHitFeedbackColor
    {
        /// <summary>
        /// 只在原版已经确认 wounded（实际受伤）后登记颜色，不主动制造受击反馈。
        /// </summary>
        public static void Postfix(
            Pawn pawn,
            ref DamageWorker.DamageResult __result)
        {
            ExplosionImpactDispatchContext impactContext = ExplosionImpactRuntimeScope.Current;
            if (pawn == null
                || impactContext == null
                || !impactContext.HasHitFeedbackColor
                || !__result.wounded
                || !AppliesToPawn(impactContext))
            {
                return;
            }

            HitFeedbackColorRuntime.Register(pawn, impactContext.HitFeedbackColor);
        }

        /// <summary>
        /// 判断范围命中反馈颜色是否覆盖当前 Pawn 目标范围。
        /// </summary>
        private static bool AppliesToPawn(ExplosionImpactDispatchContext impactContext)
        {
            switch (impactContext.HitFeedbackTargetScope)
            {
                case ExtraEffectTargetScope.AttackTargetEvents:
                case ExtraEffectTargetScope.VanillaExplosionAffectedThings:
                case ExtraEffectTargetScope.VanillaExplosionAffectedPawns:
                    return true;
                default:
                    return false;
            }
        }
    }
}
