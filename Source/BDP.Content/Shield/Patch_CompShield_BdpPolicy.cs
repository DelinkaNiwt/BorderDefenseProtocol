using BDP.Core.Projectiles.Interaction;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>
    /// 让携带 BDP 绕过策略的投射物跳过原版伤害吸收护盾。
    /// </summary>
    [HarmonyPatch(typeof(CompShield), nameof(CompShield.PostPreApplyDamage))]
    public static class Patch_CompShield_BdpPolicy
    {
        /// <summary>
        /// 仅对当前 BDP 投射物作用域短路原版 CompShield；其它伤害保持原版。
        /// </summary>
        public static bool Prefix(ref bool absorbed)
        {
            if (ProjectileInteractionPolicyScope.Current?.BypassRegisteredDamageShields != true)
            {
                return true;
            }

            absorbed = false;
            return false;
        }
    }
}
