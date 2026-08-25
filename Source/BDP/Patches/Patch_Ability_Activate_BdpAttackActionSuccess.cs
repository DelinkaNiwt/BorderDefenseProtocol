using BDP.Core.AttackExecution;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 观察原版 Ability.Activate（能力激活）成功返回。
    /// 只有 hostile（敌对）或 violent（暴力）能力才被视为攻击动作。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Ability_Activate_BdpAttackActionSuccess
    {
        /// <summary>
        /// 同时覆盖地图目标和世界目标两条原版能力入口。
        /// </summary>
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(
                typeof(Ability),
                nameof(Ability.Activate),
                new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) });
            yield return AccessTools.Method(
                typeof(Ability),
                nameof(Ability.Activate),
                new[] { typeof(GlobalTargetInfo) });
        }

        /// <summary>
        /// 能力成功执行后发布中性攻击动作通知。
        /// </summary>
        public static void Postfix(Ability __instance, bool __result)
        {
            if (!__result || !IsOffensive(__instance))
            {
                return;
            }

            Verb verb = __instance.verb;
            AttackActionSuccessDispatcher.Publish(
                AttackActionSuccess.FromAbility(__instance, verb));
        }

        /// <summary>
        /// 判断能力 Def 是否属于攻击能力。
        /// Ability.Def 是原版私有字段，使用 Harmony 读取而不修改原版状态。
        /// </summary>
        private static bool IsOffensive(Ability ability)
        {
            if (ability == null)
            {
                return false;
            }

            AbilityDef def = Traverse.Create(ability).Field("def").GetValue<AbilityDef>();
            if (def == null)
            {
                return false;
            }

            return def.hostile
                || (def.verbProperties != null && def.verbProperties.violent);
        }
    }
}
