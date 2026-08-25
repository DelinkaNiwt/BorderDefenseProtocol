using HarmonyLib;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>
    /// 在 RimWorld 原版 Pawn 受伤前处理完成后尝试应用正式能量护盾。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class Patch_Pawn_PreApplyDamage_EnergyShield
    {
        /// <summary>
        /// 只处理尚未被其它系统吸收的伤害，并把成功抵挡结果写回原版 absorbed 参数。
        /// </summary>
        public static void Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (absorbed
                || ProjectileInteractionPolicyScope.Current?.BypassRegisteredDamageShields == true
                || __instance?.health?.hediffSet?.hediffs == null)
            {
                return;
            }

            foreach (Hediff hediff in __instance.health.hediffSet.hediffs)
            {
                HediffComp_EnergyShield shield = hediff?.TryGetComp<HediffComp_EnergyShield>();
                if (shield == null || !shield.TryBlockDamage(ref dinfo))
                {
                    continue;
                }

                absorbed = true;
                // 向 Core（中性基础设施）登记“伤害前拦截”事实；Core 不知道这是哪一种具体护盾。
                DamageResolutionRuntime.MarkAbsorbed(__instance);
                return;
            }
        }
    }
}
