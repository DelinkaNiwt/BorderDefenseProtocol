using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>当前存档唯一的、以来源人物尸体为锚的限时待决事件。</summary>
    public sealed class BlackTriggerFormationEvent : IExposable
    {
        /// <summary>稳定事件标识。</summary>
        public string EventId;

        /// <summary>来源人物冻结快照。</summary>
        public BlackTriggerMakerSnapshot Snapshot;

        /// <summary>原身尸体引用。</summary>
        public Corpse Corpse;

        /// <summary>按 ThingDef 去重并完成侧重分配的候选计划。</summary>
        public List<BlackTriggerCandidatePlan> CandidatePlans = new List<BlackTriggerCandidatePlan>();

        /// <summary>玩家选择的战斗侧重 DefName。</summary>
        public string SelectedEmphasisDefName;

        /// <summary>玩家选择的传承人 LoadID；空值表示不指定。</summary>
        public string HeirLoadId;

        /// <summary>玩家选择的特性稳定键；特殊值 None 表示不继承。</summary>
        public string SelectedTraitOptionKey;

        /// <summary>事件建立 Tick。</summary>
        public int CreatedTick;

        /// <summary>进入强制决策的截止 Tick。</summary>
        public int DeadlineTick;

        /// <summary>到期后不能继续稍后决定。</summary>
        public bool ForceDecision;

        /// <summary>强制决策窗口是否已经自动打开过。</summary>
        public bool ForcedWindowOpened;

        /// <summary>最后一次可重试技术失败的语言键。</summary>
        public string LastTechnicalFailureKey;

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref EventId, "eventId");
            Scribe_Deep.Look(ref Snapshot, "snapshot");
            Scribe_References.Look(ref Corpse, "corpse", saveDestroyedThings: true);
            Scribe_Collections.Look(ref CandidatePlans, "candidatePlans", LookMode.Deep);
            Scribe_Values.Look(ref SelectedEmphasisDefName, "selectedEmphasisDefName");
            Scribe_Values.Look(ref HeirLoadId, "heirLoadId");
            Scribe_Values.Look(ref SelectedTraitOptionKey, "selectedTraitOptionKey");
            Scribe_Values.Look(ref CreatedTick, "createdTick");
            Scribe_Values.Look(ref DeadlineTick, "deadlineTick");
            Scribe_Values.Look(ref ForceDecision, "forceDecision");
            Scribe_Values.Look(ref LastTechnicalFailureKey, "lastTechnicalFailureKey");
            if (CandidatePlans == null) CandidatePlans = new List<BlackTriggerCandidatePlan>();
        }

        /// <summary>返回剩余 Tick；到期后恒为零。</summary>
        public int RemainingTicks
        {
            get { return System.Math.Max(0, DeadlineTick - (Find.TickManager?.TicksGame ?? DeadlineTick)); }
        }
    }
}
