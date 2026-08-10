using System;
using System.Collections.Generic;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// Trion 条件解释器。
    /// 当前先只预留 Trion 条件分支接入口，不在没有正式词典前伪造解释结果。
    /// </summary>
    internal sealed class TrionExpressionConditionInterpreter : IExpressionConditionInterpreter
    {
        /// <summary>
        /// 当前条件是否属于 Trion 分支。
        /// </summary>
        public bool CanInterpret(ExpressionSourceConditionConfig condition)
        {
            return condition != null
                && !string.IsNullOrWhiteSpace(condition.ConditionKey)
                && condition.ConditionKey.StartsWith("trion.", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 解释一条 Trion 条件。
        /// </summary>
        public ExpressionConditionEvaluation Evaluate(Pawn pawn, ITriggerSlotState slot, ExpressionSourceConditionConfig condition)
        {
            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            List<string> notes = new List<string>();
            if (reader == null)
            {
                notes.Add("Trion 正式读取口不可用，当前条件不能被视为已成立。");
            }
            else
            {
                notes.Add("Trion 条件分支已预留，但具体条件词典尚未接入。");
            }

            return new ExpressionConditionEvaluation
            {
                IsSatisfied = false,
                HasUnknownConditions = true,
                Notes = notes
            };
        }
    }
}
