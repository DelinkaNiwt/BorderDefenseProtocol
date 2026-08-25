using HarmonyLib;
using Verse;

namespace BDP.Content.LightSoul.Patches
{
    /// <summary>
    /// 在原版人物朝向更新完成后，把正式注视警戒 Verb 保存的自动目标应用到人物。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
    public static class Patch_Pawn_RotationTracker_UpdateRotation
    {
        /// <summary>
        /// 仅在 Verb 仍持有当前可注视的自动目标时调用原版 FaceTarget（朝向目标）。
        /// </summary>
        public static void Postfix(Pawn ___pawn, Pawn_RotationTracker __instance)
        {
            Verb_LightSoulGuardWatch verb = LightSoulGuardWatchUtility.ResolveVerb(___pawn);
            if (verb == null
                || __instance == null
                || !verb.TryGetAutomaticWatchTarget(out LocalTargetInfo target))
            {
                return;
            }

            __instance.FaceTarget(target);
        }
    }
}
