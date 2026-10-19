using System.Collections.Generic;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>无人物缓存的中性攻击准入入口；业务自行读取其已有真值。</summary>
    public static class AttackAdmissionAccess
    {
        /// <summary>启动期注册的规则；不保存人物、动作或跨存档状态。</summary>
        private static readonly List<IAttackAdmissionRule> Rules = new List<IAttackAdmissionRule>();

        /// <summary>启动期注册规则，同一实例只注册一次。</summary>
        public static void Register(IAttackAdmissionRule rule)
        {
            if (rule != null && !Rules.Contains(rule)) Rules.Add(rule);
        }

        /// <summary>默认允许；任何生效业务拒绝时返回首个拒绝，原版有效性仍由调用入口处理。</summary>
        public static AcceptanceReport Evaluate(Pawn pawn, Verb verb)
        {
            for (int i = 0; i < Rules.Count; i++)
            {
                AcceptanceReport report = Rules[i].Evaluate(pawn, verb);
                if (!report.Accepted) return report;
            }
            return true;
        }
    }
}
