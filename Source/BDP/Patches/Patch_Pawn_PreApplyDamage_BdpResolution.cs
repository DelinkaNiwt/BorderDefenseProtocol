using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 读取 Pawn（人形单位）伤害前处理的最终吸收结果。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
    public static class Patch_Pawn_PreApplyDamage_BdpResolution
    {
        /// <summary>
        /// 只观察原版最终 absorbed（已吸收）值，不改变它。
        /// </summary>
        public static void Postfix(
            Pawn __instance,
            ref bool absorbed)
        {
            DamageResolutionRuntime.ObservePreApplyDamage(__instance, absorbed);
        }
    }
}
