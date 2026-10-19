using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>死亡前冻结的传承人候选资料。</summary>
    public sealed class BlackTriggerHeirCandidateSnapshot : IExposable
    {
        /// <summary>形成前可用的 Pawn 引用。</summary>
        public Pawn Pawn;
        /// <summary>稳定 Pawn LoadID。</summary>
        public string LoadId;
        /// <summary>显示名快照。</summary>
        public string Label;
        /// <summary>关系中文显示值。</summary>
        public string RelationLabel;
        /// <summary>死亡前好感。</summary>
        public float Opinion;
        /// <summary>死亡前冻结的地图或世界旅行位置。</summary>
        public string LocationLabel;

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "pawn", saveDestroyedThings: true);
            Scribe_Values.Look(ref LoadId, "loadId");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref RelationLabel, "relationLabel");
            Scribe_Values.Look(ref Opinion, "opinion");
            Scribe_Values.Look(ref LocationLabel, "locationLabel");
        }
    }
}
