using System.Collections.Generic;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 第一版默认条件评估器。
    /// 当前负责汇总单条条件解释器结果；遇到未接入词典的条件时，不伪装成已成立。
    /// </summary>
    internal sealed class DefaultExpressionConditionEvaluator
    {
        /// <summary>
        /// 单条条件解释器列表。
        /// </summary>
        private readonly IReadOnlyList<IExpressionConditionInterpreter> interpreters;

        /// <summary>
        /// 构造默认条件评估器。
        /// </summary>
        public DefaultExpressionConditionEvaluator()
        {
            interpreters = new List<IExpressionConditionInterpreter>
            {
                new CombatBodyExpressionConditionInterpreter(),
                new TrionExpressionConditionInterpreter(),
                new ModeExpressionConditionInterpreter()
            };
        }

        /// <summary>
        /// 评估当前来源声明的条件集合。
        /// </summary>
        public ExpressionConditionEvaluation Evaluate(Pawn pawn, ITriggerSlotState slot, ExpressionSourceDeclaration declaration)
        {
            List<string> notes = new List<string>();
            if (declaration == null || declaration.Conditions == null || declaration.Conditions.Count == 0)
            {
                return new ExpressionConditionEvaluation
                {
                    IsSatisfied = true,
                    HasUnknownConditions = false,
                    Notes = notes
                };
            }

            bool hasUnknownConditions = false;
            for (int i = 0; i < declaration.Conditions.Count; i++)
            {
                ExpressionSourceConditionConfig condition = declaration.Conditions[i];
                ExpressionConditionEvaluation conditionResult = EvaluateSingleCondition(pawn, slot, condition);
                if (conditionResult == null)
                {
                    hasUnknownConditions = true;
                    notes.Add("存在未被任何条件解释器接管的来源条件。");
                    continue;
                }

                if (conditionResult.Notes != null)
                {
                    for (int noteIndex = 0; noteIndex < conditionResult.Notes.Count; noteIndex++)
                    {
                        if (!string.IsNullOrWhiteSpace(conditionResult.Notes[noteIndex]))
                        {
                            notes.Add(conditionResult.Notes[noteIndex]);
                        }
                    }
                }

                if (!conditionResult.IsSatisfied || conditionResult.HasUnknownConditions)
                {
                    hasUnknownConditions = hasUnknownConditions || conditionResult.HasUnknownConditions;
                    return new ExpressionConditionEvaluation
                    {
                        IsSatisfied = false,
                        HasUnknownConditions = hasUnknownConditions || conditionResult.HasUnknownConditions,
                        Notes = notes
                    };
                }
            }

            return new ExpressionConditionEvaluation
            {
                IsSatisfied = true,
                HasUnknownConditions = false,
                Notes = notes
            };
        }

        /// <summary>
        /// 逐条评估单个条件。
        /// </summary>
        private ExpressionConditionEvaluation EvaluateSingleCondition(
            Pawn pawn,
            ITriggerSlotState slot,
            ExpressionSourceConditionConfig condition)
        {
            if (interpreters == null)
            {
                return null;
            }

            for (int i = 0; i < interpreters.Count; i++)
            {
                IExpressionConditionInterpreter interpreter = interpreters[i];
                if (interpreter == null || !interpreter.CanInterpret(condition))
                {
                    continue;
                }

                return interpreter.Evaluate(pawn, slot, condition);
            }

            return null;
        }
    }
}
