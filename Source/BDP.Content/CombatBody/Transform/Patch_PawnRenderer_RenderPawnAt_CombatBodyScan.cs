using HarmonyLib;
using Verse;

namespace BDP.Content.CombatBody.Transform
{
    /// <summary>
    /// 扫描快照接管画面期间，短时跳过原版完整人物绘制。
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class Patch_PawnRenderer_RenderPawnAt_CombatBodyScan
    {
        /// <summary>
        /// 直接使用 Harmony 的私有字段注入取得所属 Pawn，不引入反射访问。
        /// </summary>
        public static bool Prefix(Pawn ___pawn)
        {
            return !CombatBodyPawnRenderSuppression.ShouldSuppress(___pawn);
        }
    }
}
