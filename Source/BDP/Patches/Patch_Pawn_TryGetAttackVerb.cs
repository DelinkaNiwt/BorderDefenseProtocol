using BDP.Core.AttackExecution;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 自动远程入口桥。
    /// 当前版本优先读取表达系统选出的默认远程主攻击，再把对应 formal host 壳交给原版持续攻击会话。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    /// <summary>
    /// 自动远程正式宿主壳注入补丁。
    /// </summary>
    public static class Patch_Pawn_TryGetAttackVerb
    {
        /// <summary>
        /// 只要表达系统给出了合法 PrimaryRanged，就优先让自动远程走 BDP。
        /// 没有合法表达结果时，再完整保留原版基线返回值。
        /// 捕获 target 参数用于单侧回退场景下的射程感知主攻选择。
        /// </summary>
        public static void Postfix(ref Verb __result, Pawn __instance, Thing target, bool allowManualCastWeapons)
        {
            if (__instance == null)
            {
                return;
            }

            if (!AttackExecutionSurfaceAccess.TryGetAutoRangedVerb(__instance, allowManualCastWeapons, target, out Verb rangedVerb))
            {
                return;
            }

            __result = rangedVerb;
        }
    }
}
