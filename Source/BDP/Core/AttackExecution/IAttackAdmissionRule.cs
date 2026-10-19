using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>报告已有业务状态是否允许当前攻击，不持有另一份人物限制状态。</summary>
    public interface IAttackAdmissionRule
    {
        /// <summary>只读查询；允许时通过，拒绝时携带本地化原因。</summary>
        AcceptanceReport Evaluate(Pawn pawn, Verb verb);
    }
}
