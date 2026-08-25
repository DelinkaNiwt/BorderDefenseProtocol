using BDP.Core.Projectiles;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 在原版投射物拦截器执行前读取 BDP 的冻结绕过策略。
    /// </summary>
    [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.CheckIntercept))]
    public static class Patch_CompProjectileInterceptor_BdpPolicy
    {
        /// <summary>
        /// 只对携带 BDP 策略的投射物跳过当前拦截器；普通投射物完全回退原版。
        /// </summary>
        public static bool Prefix(Projectile projectile, ref bool __result)
        {
            BdpProjectile bdpProjectile = projectile as BdpProjectile;
            if (bdpProjectile?.CurrentInteractionPolicy?.BypassProjectileInterceptors != true)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
