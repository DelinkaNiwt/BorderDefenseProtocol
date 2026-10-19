using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>只给拥有活动形成事件的尸体增加打开决策窗口的按钮。</summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetGizmos))]
    public static class Patch_Corpse_BlackTriggerFormation
    {
        /// <summary>追加 BDP 尸体决策按钮。</summary>
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Thing __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;
            Corpse corpse = __instance as Corpse;
            if (corpse == null) yield break;
            BlackTriggerFormationEvent formationEvent = BlackTriggerFormationWorldService.Resolve()?.PendingEvent;
            if (formationEvent?.Corpse != corpse) formationEvent = null;
            if (formationEvent == null) yield break;
            yield return new Command_Action
            {
                defaultLabel = "BDP_BlackTrigger_Formation_Open".Translate(),
                defaultDesc = "BDP_BlackTrigger_Formation_OpenDesc".Translate(),
                action = delegate { Find.WindowStack.Add(new Window_BlackTriggerFormationDecision(formationEvent.EventId)); }
            };
        }
    }

    /// <summary>只给当前事件尸体追加简短待决状态。</summary>
    [HarmonyPatch(typeof(Corpse), nameof(Corpse.GetInspectString))]
    public static class Patch_Corpse_BlackTriggerFormationInspect
    {
        /// <summary>显示未决形成与剩余时间，不泄露内部候选。</summary>
        public static void Postfix(Corpse __instance, ref string __result)
        {
            BlackTriggerFormationEvent formationEvent = BlackTriggerFormationWorldService.Resolve()?.PendingEvent;
            if (formationEvent?.Corpse != __instance) return;
            string remaining = formationEvent.ForceDecision
                ? "BDP_BlackTrigger_Formation_DeadlineReached".Translate().ToString()
                : formationEvent.RemainingTicks.ToStringTicksToPeriod();
            __result += (__result.NullOrEmpty() ? string.Empty : "\n")
                + "BDP_BlackTrigger_Formation_CorpseInspect".Translate(remaining);
        }
    }
}
