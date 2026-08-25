using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 读取原版 ThingWithComps（带组件目标）的伤害前吸收结果。
    /// </summary>
    [HarmonyPatch(typeof(ThingWithComps), "PreApplyDamage")]
    public static class Patch_ThingWithComps_PreApplyDamage_BdpResolution
    {
        /// <summary>
        /// 只观察原版最终 absorbed（已吸收）值，不改变它。
        /// </summary>
        public static void Postfix(
            ThingWithComps __instance,
            ref bool absorbed)
        {
            DamageResolutionRuntime.ObservePreApplyDamage(__instance, absorbed);
        }
    }
}
