using System;
using System.Collections.Generic;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 形态条件解释器。
    /// 当前先只预留形态条件分支；在 Trigger 正式形态读取口落地前，不伪造解释结果。
    /// </summary>
    internal sealed class ModeExpressionConditionInterpreter : IExpressionConditionInterpreter
    {
        /// <summary>
        /// 当前条件是否属于形态分支。
        /// </summary>
        public bool CanInterpret(ExpressionSourceConditionConfig condition)
        {
            return condition != null
                && !string.IsNullOrWhiteSpace(condition.ConditionKey)
                && condition.ConditionKey.StartsWith("mode.", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 解释一条形态条件。
        /// </summary>
        public ExpressionConditionEvaluation Evaluate(Pawn pawn, ITriggerSlotState slot, ExpressionSourceConditionConfig condition)
        {
            return new ExpressionConditionEvaluation
            {
                IsSatisfied = false,
                HasUnknownConditions = true,
                Notes = new List<string>
                {
                    "形态条件分支已预留，但 Trigger 正式形态读取口尚未接入。"
                }
            };
        }
    }
}
