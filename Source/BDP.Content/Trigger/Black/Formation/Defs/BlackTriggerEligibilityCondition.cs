using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>首次装备适格条件的种类。</summary>
    public enum BlackTriggerEligibilityConditionKind
    {
        MinimumTrionPotential,
        SkillInTopThree,
        SkillLevelAtLeast,
        SkillDisabled,
        TraitDegree,
        ActiveGene,
        RecordNonZero,
        Custom
    }

    /// <summary>一条可解释的首次装备适格条件。</summary>
    public sealed class BlackTriggerEligibilityCondition
    {
        /// <summary>条件种类。</summary>
        public BlackTriggerEligibilityConditionKind kind;

        /// <summary>技能条件使用的技能 Def。</summary>
        public SkillDef skill;

        /// <summary>特质条件使用的特质 Def。</summary>
        public TraitDef trait;

        /// <summary>基因条件使用的基因 Def。</summary>
        public GeneDef gene;

        /// <summary>记录条件使用的 RecordDef。</summary>
        public RecordDef record;

        /// <summary>最低数值或 degree。</summary>
        public float minimumValue;

        /// <summary>玩家可见的通过说明语言键。</summary>
        public string explanationKey;

        /// <summary>自定义条件 Worker 的完整类型名。</summary>
        public string customWorkerType;
    }
}
