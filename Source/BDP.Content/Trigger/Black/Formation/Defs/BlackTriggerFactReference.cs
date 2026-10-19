using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>形成条件引用的单项具体事实。</summary>
    public sealed class BlackTriggerFactReference
    {
        /// <summary>技能事实；与 Record 只能二选一。</summary>
        public SkillDef skill;

        /// <summary>生涯记录事实；与 Skill 只能二选一。</summary>
        public RecordDef record;

        /// <summary>该条件在玩家界面的补充说明语言键。</summary>
        public string explanationKey;

        /// <summary>返回引用事实的稳定键。</summary>
        public string Key
        {
            get { return skill != null ? "S:" + skill.defName : record != null ? "R:" + record.defName : string.Empty; }
        }

        /// <summary>判断配置是否恰好引用一种事实。</summary>
        public bool IsValid
        {
            get { return (skill != null) ^ (record != null); }
        }

        /// <summary>判断快照事实是否命中本引用。</summary>
        public bool Matches(BlackTriggerFactSnapshot fact)
        {
            if (fact == null)
            {
                return false;
            }
            return skill != null && fact.Kind == BlackTriggerFactKind.Skill
                && string.Equals(fact.DefName, skill.defName, System.StringComparison.Ordinal)
                || record != null && fact.Kind == BlackTriggerFactKind.Record
                && string.Equals(fact.DefName, record.defName, System.StringComparison.Ordinal);
        }
    }
}
