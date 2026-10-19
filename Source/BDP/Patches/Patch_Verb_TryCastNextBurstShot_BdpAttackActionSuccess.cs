using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BDP.Core.AttackExecution;
using BDP.Core.Abilities;
using BDP.Core.Verbs;
using BDP.Support.Diagnostics;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>观察原版单次发射的真实返回事实，不用连射次数清零推测发射成功。</summary>
    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess
    {
        /// <summary>当前线程本轮单次发射观察；嵌套发射各有自己的状态。</summary>
        [ThreadStatic]
        private static ShotObservation current;

        /// <summary>记录原版真实 TryCastShot 返回值的中性观察方法。</summary>
        private static readonly MethodInfo ObserveMethod =
            AccessTools.Method(typeof(Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess), nameof(ObserveShotResult));

        /// <summary>武器及自有射击能力共用原版成功观察；其余能力继续由激活入口通知，避免双重发布。</summary>
        public static void Prefix(Verb __instance, out ShotObservation __state)
        {
            __state = null;
            if (__instance is BdpVerb_AbilityShoot abilityShoot)
            {
                // 此共用宿主自行发射并启动冷却，不调用 Ability.Activate；通知要等原版收尾，不能塞进发射函数。
                if (!AttackActionSuccess.IsOffensiveAbility(abilityShoot.Ability)) return;
            }
            else if (__instance is Verb_CastAbility
                || (!(__instance is Verb_LaunchProjectile) && !(__instance is Verb_MeleeAttack))) return;
            if (__instance is BdpVerb_MeleeAttackDamage bdpMelee)
                bdpMelee.ClearLastMeleeBurstShotResult();
            __state = new ShotObservation(__instance);
        }

        /// <summary>在唯一的原版 TryCastShot 虚调用后复制返回值作观察，原分支仍消费原 bool。</summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            MethodInfo cast = AccessTools.Method(typeof(Verb), "TryCastShot");
            int callIndex = -1;
            int callCount = 0;
            for (int index = 0; index < code.Count; index++)
            {
                if (!code[index].Calls(cast)) continue;
                callIndex = index;
                callCount++;
            }
            if (callCount != 1)
            {
                // 其他补丁改写了调用点时不猜测结果，也不妨碍原版方法安装与执行。
                if (BdpDiagnostics.Enabled)
                    BdpDiagnostics.Once("attack.success.cast_anchor_missing", "BDP_AttackSuccessPatchUnavailable".Translate());
                return code;
            }
            code.InsertRange(callIndex + 1, new[]
            {
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, ObserveMethod)
            });
            return code;
        }

        /// <summary>只登记本轮同一 Verb 的真实成功事实；不改变返回值、命中判断或原版流程。</summary>
        public static void ObserveShotResult(bool succeeded, Verb verb)
        {
            ShotObservation state = current;
            if (!succeeded || state == null || !ReferenceEquals(state.Verb, verb)) return;
            // BDP 近战用 true 表示尝试完成；miss/dodge 的通知仍由近战专用补丁发布。
            if (verb is BdpVerb_MeleeAttackDamage melee
                && (!melee.LastMeleeBurstShotCompleted || !melee.LastMeleeBurstShotHit)) return;
            state.Succeeded = true;
        }

        /// <summary>正常原版收尾后退出观察，再按真实来源发布一次成功动作；投射物是否造成伤害不参与裁定。</summary>
        public static void Postfix(Verb __instance, ShotObservation __state)
        {
            __state?.Dispose();
            if (__state?.Succeeded != true) return;
            AttackActionSuccessDispatcher.Publish(__instance is BdpVerb_AbilityShoot abilityShoot
                ? AttackActionSuccess.FromAbility(abilityShoot.Ability, abilityShoot)
                : AttackActionSuccess.FromWeapon(__instance, true));
        }

        /// <summary>异常也退出观察，不能把已登记事实泄漏到下一轮。</summary>
        public static Exception Finalizer(ShotObservation __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }

        /// <summary>本轮调用的短期观察令牌；不保存历史动作。</summary>
        public sealed class ShotObservation : IDisposable
        {
            /// <summary>进入本轮前的外层观察。</summary>
            private readonly ShotObservation previous;
            /// <summary>压住正常与异常出口重复清理。</summary>
            private bool disposed;
            /// <summary>本轮唯一的攻击动作实例。</summary>
            internal Verb Verb { get; }
            /// <summary>原版 TryCastShot 是否真正完成成功动作。</summary>
            internal bool Succeeded { get; set; }

            /// <summary>保存外层观察，开始本轮观察。</summary>
            internal ShotObservation(Verb verb)
            {
                Verb = verb;
                previous = current;
                current = this;
            }

            /// <summary>对称恢复外层观察，避免异常/嵌套发射污染后续动作。</summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                current = previous;
            }
        }
    }
}
