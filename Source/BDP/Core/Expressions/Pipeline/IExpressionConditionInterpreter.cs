using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 单条表达条件解释器。
    /// 它只负责解释自己所属条件分支，不处理整组条件的汇总。
    /// </summary>
    internal interface IExpressionConditionInterpreter
    {
        /// <summary>
        /// 当前解释器是否负责这条条件。
        /// </summary>
        bool CanInterpret(ExpressionSourceConditionConfig condition);

        /// <summary>
        /// 解释一条条件，并返回该条件自己的评估结果。
        /// </summary>
        ExpressionConditionEvaluation Evaluate(Pawn pawn, ITriggerSlotState slot, ExpressionSourceConditionConfig condition);
    }
}
