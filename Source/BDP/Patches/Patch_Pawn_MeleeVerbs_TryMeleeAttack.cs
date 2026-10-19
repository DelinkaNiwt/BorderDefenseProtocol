using BDP.Core.AttackExecution;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 自动近战入口桥：只提供合法默认攻击，由原版方法执行，原任务继续管理时序。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    public static class Patch_Pawn_MeleeVerbs_TryMeleeAttack
    {
        /// <summary>
        /// 只有原版还没明确给出 verbToUse 时，才准备默认近战并注入该参数。
        /// 另外，玩家手动点原版近战按钮时虽然 verbToUse 也可能为空，
        /// 但那属于原版基线手动命令，不应被自动近战桥误接管。
        /// 表达系统没有合法 PrimaryMelee 时，这里完全回退原版。
        /// </summary>
        public static void Prefix(Pawn_MeleeVerbs __instance, Thing target, ref Verb verbToUse)
        {
            if (verbToUse != null
                || __instance?.Pawn == null
                || __instance.Pawn.stances.FullBodyBusy
                || !__instance.Pawn.kindDef.canMeleeAttack
                || target == null
                || !target.Spawned
                || IsPlayerForcedVanillaMeleeOrder(__instance.Pawn, target))
            {
                return;
            }

            if (AttackExecutionSurfaceAccess.TryPrepareAutoMelee(__instance.Pawn, target, out Verb preparedVerb))
            {
                verbToUse = preparedVerb;
            }
        }

        /// <summary>
        /// 判断当前这次近战起手是否来自原版手动近战命令。
        /// 原版黄框近战按钮会下 `AttackMelee` 且 `playerForced=true` 的 job，
        /// 但不会把本体近战 verb 显式写进 `verbToUse`。
        /// 这类调用必须完整放回原版，不能被 BDP 自动近战桥误判成自动攻击。
        /// </summary>
        private static bool IsPlayerForcedVanillaMeleeOrder(Pawn pawn, Thing target)
        {
            if (pawn?.jobs?.curJob == null || target == null)
            {
                return false;
            }

            return pawn.jobs.curJob.def == JobDefOf.AttackMelee
                && pawn.jobs.curJob.playerForced
                && pawn.jobs.curJob.targetA.Thing == target;
        }
    }
}
