using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>玩家可选择的单条冻结事实。</summary>
    public sealed class BlackTriggerFactSnapshot : IExposable
    {
        /// <summary>事实来源种类。</summary>
        public BlackTriggerFactKind Kind;
        /// <summary>稳定 DefName。</summary>
        public string DefName;
        /// <summary>玩家可读名称。</summary>
        public string Label;
        /// <summary>技能等级或记录原始值。</summary>
        public float Value;
        /// <summary>技能兴趣等级。</summary>
        public Passion Passion;
        /// <summary>生涯记录竞赛名次。</summary>
        public int Rank;
        /// <summary>记录显示分组。</summary>
        public string GroupKey;

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref Kind, "kind");
            Scribe_Values.Look(ref DefName, "defName");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref Value, "value");
            Scribe_Values.Look(ref Passion, "passion");
            Scribe_Values.Look(ref Rank, "rank");
            Scribe_Values.Look(ref GroupKey, "groupKey");
        }

        /// <summary>事实稳定键。</summary>
        public string StableKey
        {
            get { return (Kind == BlackTriggerFactKind.Skill ? "S:" : "R:") + (DefName ?? string.Empty); }
        }

        /// <summary>数值显示文本。</summary>
        public string ValueText
        {
            get
            {
                if (Kind == BlackTriggerFactKind.Skill) return ((int)Value).ToString();
                RecordDef record = DefDatabase<RecordDef>.GetNamedSilentFail(DefName);
                return record?.type == RecordType.Time
                    ? ((int)Value).ToStringTicksToPeriod()
                    : Value.ToString("0.##");
            }
        }
    }
}
