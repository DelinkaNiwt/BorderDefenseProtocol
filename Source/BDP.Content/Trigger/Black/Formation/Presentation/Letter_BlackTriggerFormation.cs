using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>黑触发器尸体形成待决信件，使用原版 LetterWithTimeout。</summary>
    public sealed class Letter_BlackTriggerFormation : LetterWithTimeout
    {
        /// <summary>关联的形成事件 ID。</summary>
        public string EventId;
        /// <summary>玩家可见信件正文。</summary>
        private string messageText;

        /// <summary>设置玩家可见信件内容。</summary>
        public void Configure()
        {
            BlackTriggerFormationEvent formationEvent = BlackTriggerFormationWorldService.Find(EventId);
            string maker = formationEvent?.Snapshot?.MakerLabel ?? "BDP_BlackTrigger_UnknownMaker".Translate().ToString();
            Label = "BDP_BlackTrigger_Formation_LetterLabel".Translate();
            if (formationEvent?.ForceDecision == true)
            {
                def = LetterDefOf.NegativeEvent;
                disappearAtTick = -1;
                messageText = "BDP_BlackTrigger_Formation_LetterUrgentText".Translate(maker).ToString();
            }
            else
            {
                string remaining = (formationEvent?.RemainingTicks ?? 0).ToStringTicksToPeriod();
                messageText = "BDP_BlackTrigger_Formation_LetterText".Translate(maker, remaining).ToString();
            }
            lookTargets = formationEvent?.Corpse == null ? null : new LookTargets(formationEvent.Corpse);
        }

        /// <summary>打开同一形成决策窗口。</summary>
        public override void OpenLetter()
        {
            Find.WindowStack.Add(new Window_BlackTriggerFormationDecision(EventId));
        }

        /// <summary>不允许把待决信件右键丢弃，避免玩家误以为形成已结束。</summary>
        public override bool CanDismissWithRightClick { get { return false; } }

        /// <summary>时限由世界事件统一处理，避免到期自动打开一个已终止窗口。</summary>
        public override bool ShouldAutomaticallyOpenLetter { get { return false; } }

        /// <summary>原版信件鼠标悬停提示。</summary>
        protected override string GetMouseoverText()
        {
            return messageText ?? string.Empty;
        }

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EventId, "eventId");
            Scribe_Values.Look(ref messageText, "messageText");
        }
    }
}
