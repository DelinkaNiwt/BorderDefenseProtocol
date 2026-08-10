using BDP.Core.AttackExecution;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 自动近战入口桥。
    /// 当前版本只在表达系统给出合法默认近战主攻击时接管自动近战，否则完整放行原版近战池。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    /// <summary>
    /// 自动近战起手转发补丁。
    /// </summary>
    public static class Patch_Pawn_MeleeVerbs_TryMeleeAttack
    {
        /// <summary>
        /// 只有原版还没明确给出 verbToUse 时，才尝试把自动近战起手翻译成正式攻击请求。
        /// 另外，玩家手动点原版近战按钮时虽然 verbToUse 也可能为空，
        /// 但那属于原版基线手动命令，不应被自动近战桥误接管。
        /// 表达系统没有合法 PrimaryMelee 时，这里完全回退原版。
        /// </summary>
        public static bool Prefix(Pawn_MeleeVerbs __instance, Thing target, Verb verbToUse, ref bool __result)
        {
            if (verbToUse != null
                || __instance?.Pawn == null
                || target == null
                || !target.Spawned
                || IsPlayerForcedVanillaMeleeOrder(__instance.Pawn, target))
            {
                return true;
            }

            if (!AttackExecutionSurfaceAccess.TryExecuteAutoMelee(__instance.Pawn, target))
            {
                return true;
            }

            __result = true;
            return false;
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
