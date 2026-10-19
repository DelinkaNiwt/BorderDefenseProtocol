using BDP.Content.Trigger.Black.Formation;
using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Trigger.BlackFormation
{
    /// <summary>Development 黑触发器调试入口。</summary>
    public static class BlackTriggerFormationDebugActions
    {
        /// <summary>打开系统总览。</summary>
        [DebugAction("Mods", "BDP：黑触发器：打开系统总览", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -221)]
        public static void OpenSystemCatalog() { Find.WindowStack.Add(new Window_BlackTriggerSystemCatalog()); }

        /// <summary>绕过危机、概率与冷却，但仍先走正式匹配准备。</summary>
        [DebugAction("Mods", "BDP：黑触发器：调试燃尽当前殖民者", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -220)]
        public static void DebugSacrificeSelectedPawn()
        {
            Pawn pawn = Find.Selector?.SingleSelectedThing as Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled || pawn.Dead)
            {
                Messages.Message("BDP_BlackTrigger_Debug_NoPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            BlackTriggerSacrificeService.ExecuteDebug(pawn);
        }

        /// <summary>输出当前角色的完整冻结快照、逐路线真值和侧重分配。</summary>
        [DebugAction("Mods", "BDP：黑触发器：输出形成匹配诊断", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -222)]
        public static void PrintFormationDiagnostic()
        {
            Pawn pawn = Find.Selector?.SingleSelectedThing as Pawn;
            if (pawn == null) { Messages.Message("BDP_BlackTrigger_Debug_NoPawn".Translate(), MessageTypeDefOf.RejectInput, false); return; }
            BlackTriggerMakerSnapshot snapshot = BlackTriggerMakerSnapshotFactory.Capture(pawn, BlackTriggerFormationSource.NaturalDeath, null, null);
            Log.Message(BlackTriggerFormationDiagnosticFormatter.Format(snapshot));
        }

        /// <summary>把当前待决事件推进到超时前一刻。</summary>
        [DebugAction("Mods", "BDP：黑触发器：推进到最后一小时", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -223)]
        public static void AdvancePendingEvent()
        {
            BlackTriggerFormationEvent formationEvent = BlackTriggerFormationWorldService.Resolve()?.PendingEvent;
            if (formationEvent != null) formationEvent.DeadlineTick = Find.TickManager.TicksGame + 2500;
        }

        /// <summary>立即触发到期检查。</summary>
        [DebugAction("Mods", "BDP：黑触发器：立即触发到期检查", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -224)]
        public static void TriggerExpiry()
        {
            BlackTriggerFormationEvent formationEvent = BlackTriggerFormationWorldService.Resolve()?.PendingEvent;
            if (formationEvent != null) formationEvent.DeadlineTick = Find.TickManager.TicksGame - 1;
            BlackTriggerFormationWorldService.ScanEvent(BlackTriggerFormationWorldService.Resolve());
        }

        /// <summary>终止当前待决事件并释放保底。</summary>
        [DebugAction("Mods", "BDP：黑触发器：终止当前待决事件", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -225)]
        public static void TerminatePendingEvent()
        {
            BlackTriggerFormationEvent formationEvent = BlackTriggerFormationWorldService.Resolve()?.PendingEvent;
            if (formationEvent != null) BlackTriggerFormationWorldService.Abandon(formationEvent.EventId);
        }

        /// <summary>重置当前选中黑触发器种类的开发唯一登记。</summary>
        [DebugAction("Mods", "BDP：黑触发器：重置选中种类唯一登记", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -226)]
        public static void ResetSelectedUniqueState()
        {
            Thing trigger = Find.Selector?.SingleSelectedThing;
            if (trigger?.TryGetComp<CompBlackTriggerBiocodable>() == null)
            {
                Messages.Message("BDP_BlackTrigger_Debug_NoTrigger".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            GameComponent_BlackTriggerWorld world = BlackTriggerFormationWorldService.Resolve();
            if (world != null) world.FinalizedThingDefNames.Remove(trigger.def.defName);
            Messages.Message("BDP_BlackTrigger_Debug_ResetDone".Translate(), MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
