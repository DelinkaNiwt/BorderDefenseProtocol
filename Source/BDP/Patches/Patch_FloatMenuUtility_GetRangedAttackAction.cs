using System;
using BDP.Core.Trigger;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// GetRangedAttackAction 射程感知补丁。
    /// 原版在创建远程攻击命令时通过 PrimaryVerb 检查射程，
    /// 但 PrimaryVerb 是属性 getter 不接受 target 参数，无法按目标选最优 Verb。
    /// Prefix 设 target hint → 方法内多次 PrimaryVerb 都读取同一 hint →
    /// 正常与异常退出都恢复真正写入组件的旧提示，支持嵌套查询与查询期间换装备。
    /// </summary>
    [HarmonyPatch(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetRangedAttackAction))]
    public static class Patch_FloatMenuUtility_GetRangedAttackAction
    {
        /// <summary>
        /// 在原版射程检查前为触发体设置本次攻击的 target hint。
        /// </summary>
        static void Prefix(Pawn pawn, LocalTargetInfo target, out IDisposable __state)
        {
            __state = null;
            if (pawn?.equipment?.Primary == null || !target.IsValid)
                return;

            CompTriggerBody triggerBody = pawn.equipment.Primary.TryGetComp<CompTriggerBody>();
            __state = triggerBody?.BeginRangedTargetHint(target.Thing);
        }

        /// <summary>
        /// 原版方法正常退出后恢复旧提示。
        /// GetRangedAttackAction 内部调用 PrimaryVerb 两次，
        /// hint 必须在整个方法执行期间保持有效。
        /// </summary>
        static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        /// <summary>异常退出也恢复同一组件；作用域幂等，正常退出后的重复清理安全。</summary>
        static Exception Finalizer(IDisposable __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }
}
