using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>黑触发器形成系统的最小世界状态。</summary>
    public sealed class GameComponent_BlackTriggerWorld : GameComponent
    {
        /// <summary>全局唯一的当前待决事件。</summary>
        public BlackTriggerFormationEvent PendingEvent;

        /// <summary>永久占用的黑触发器世界唯一键。</summary>
        public List<string> FinalizedThingDefNames = new List<string>();

        /// <summary>普通形成流程下一次允许建立事件的 Tick。</summary>
        public int NextFormationAllowedTick;

        /// <summary>供 RimWorld 自动构造。</summary>
        public GameComponent_BlackTriggerWorld(Game game) { }

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public override void ExposeData()
        {
            Scribe_Deep.Look(ref PendingEvent, "blackTriggerPendingEvent");
            Scribe_Collections.Look(ref FinalizedThingDefNames, "blackTriggerFinalizedThingDefNames", LookMode.Value);
            Scribe_Values.Look(ref NextFormationAllowedTick, "blackTriggerNextFormationAllowedTick");
            if (FinalizedThingDefNames == null) FinalizedThingDefNames = new List<string>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                BlackTriggerBindingIndex.Rebuild();
                BlackTriggerInheritedTraitService.AuditAllPawns();
            }
        }

        /// <summary>低频检查尸体链和三小时截止状态。</summary>
        public override void GameComponentTick()
        {
            int interval = BlackTriggerFormationWorldService.ResolveSettings().eventScanIntervalTicks;
            if (interval <= 0 || Find.TickManager.TicksGame % interval != 0) return;
            BlackTriggerFormationWorldService.ScanEvent(this);
        }

        /// <summary>按事件标识返回当前待决事件。</summary>
        public BlackTriggerFormationEvent FindPending(string eventId)
        {
            return PendingEvent != null && PendingEvent.EventId == eventId ? PendingEvent : null;
        }
    }
}
