using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>把献祭入口挂到原版 Pawn 的 gizmo（操作按钮）列表。</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_BlackTriggerSacrifice
    {
        /// <summary>只对当前处于个人危机的玩家殖民者追加按钮。</summary>
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            string reasonKey;
            if (!BlackTriggerPersonalCrisisEvaluator.IsCrisis(__instance, out reasonKey))
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "BDP_BlackTrigger_Sacrifice_GizmoLabel".Translate(),
                defaultDesc = "BDP_BlackTrigger_Sacrifice_GizmoDesc".Translate(),
                action = delegate { Command_BlackTriggerSacrifice.TrySacrifice(__instance); }
            };
        }
    }
}
