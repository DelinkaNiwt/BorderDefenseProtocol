using System;
using BDP.Core.AttackExecution;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 观察原版所有普通 Verb 的统一单次发射执行入口。
    /// 这样标准武器和 BDP 自定义武器都不需要各自复制攻击通知补丁。
    /// </summary>
    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess
    {
        /// <summary>
        /// 记录本次入口调用前剩余的 burst（连发）次数。
        /// </summary>
        public static void Prefix(Verb __instance, ref int __state)
        {
            __state = ReadBurstShotsLeft(__instance);
        }

        /// <summary>
        /// 只在内部 TryCastShot（尝试施放）确实成功时发布普通武器攻击通知。
        /// Ability Verb（能力动词）由 Ability.Activate 专用补丁负责，避免重复通知。
        /// </summary>
        public static void Postfix(Verb __instance, int __state)
        {
            if (__instance == null
                || __instance is Verb_CastAbility
                || (!(__instance is Verb_LaunchProjectile) && !(__instance is Verb_MeleeAttack))
                || ReadBurstShotsLeft(__instance) >= __state)
            {
                return;
            }

            AttackActionSuccessDispatcher.Publish(
                AttackActionSuccess.FromWeapon(__instance, true));
        }

        /// <summary>
        /// 读取原版 Verb 私有的连发剩余次数。
        /// </summary>
        private static int ReadBurstShotsLeft(Verb verb)
        {
            return verb == null
                ? 0
                : Traverse.Create(verb).Field("burstShotsLeft").GetValue<int>();
        }
    }
}
