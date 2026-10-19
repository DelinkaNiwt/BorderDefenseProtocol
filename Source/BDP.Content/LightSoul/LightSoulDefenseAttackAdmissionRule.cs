using BDP.Core.AttackExecution;
using Verse;

namespace BDP.Content.LightSoul
{
    /// <summary>防护业务从既有健康状态提供远程限制；破盾不会结束防护限制。</summary>
    internal sealed class LightSoulDefenseAttackAdmissionRule : IAttackAdmissionRule
    {
        /// <summary>保留现有远程分类，不把位移和非攻击能力归为远程武器。</summary>
        public AcceptanceReport Evaluate(Pawn pawn, Verb verb)
        {
            if (!LightSoulDefenseWatchUtility.IsRangedBlocked(verb)) return true;
            return "BDP_AttackBlocked_LightSoulDefense".Translate().ToString();
        }
    }
}
