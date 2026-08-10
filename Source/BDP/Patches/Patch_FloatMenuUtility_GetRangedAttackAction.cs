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
    /// Postfix 清除 hint，避免污染后续调用。
    /// </summary>
    [HarmonyPatch(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetRangedAttackAction))]
    public static class Patch_FloatMenuUtility_GetRangedAttackAction
    {
        /// <summary>
        /// 在原版射程检查前为触发体设置本次攻击的 target hint。
        /// </summary>
        static void Prefix(Pawn pawn, LocalTargetInfo target)
        {
            if (pawn?.equipment?.Primary == null || !target.IsValid)
                return;

            CompTriggerBody triggerBody = pawn.equipment.Primary.TryGetComp<CompTriggerBody>();
            triggerBody?.PrepareRangedVerbForTarget(target.Thing);
        }

        /// <summary>
        /// 原版方法退出后清除 hint。
        /// GetRangedAttackAction 内部调用 PrimaryVerb 两次，
        /// hint 必须在整个方法执行期间保持有效。
        /// </summary>
        static void Postfix(Pawn pawn)
        {
            CompTriggerBody triggerBody = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            triggerBody?.ClearPendingRangedTargetHint();
        }
    }
}
