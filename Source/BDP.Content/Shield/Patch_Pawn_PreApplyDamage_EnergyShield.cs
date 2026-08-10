using HarmonyLib;
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
            if (absorbed || __instance?.health?.hediffSet?.hediffs == null)
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
                return;
            }
        }
    }
}
