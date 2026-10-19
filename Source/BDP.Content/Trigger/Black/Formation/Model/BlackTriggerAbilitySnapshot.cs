using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>人物死亡前冻结的一项实际拥有能力。</summary>
    public sealed class BlackTriggerAbilitySnapshot : IExposable
    {
        /// <summary>来源人物实际拥有的能力 DefName。</summary>
        public string AbilityDefName;

        /// <summary>能力来源模组包标识。</summary>
        public string SourcePackageId;

        /// <summary>死亡前冻结的玩家可读名称。</summary>
        public string Label;

        /// <summary>能力 DefName 与来源包组成的稳定键。</summary>
        public string StableKey
        {
            get { return (AbilityDefName ?? string.Empty) + "@" + (SourcePackageId ?? string.Empty); }
        }

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref AbilityDefName, "abilityDefName");
            Scribe_Values.Look(ref SourcePackageId, "sourcePackageId");
            Scribe_Values.Look(ref Label, "label");
        }
    }
}
