using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDP.Content.Trigger.Black.Formation;

namespace BDP.Development.Trigger.BlackFormation
{
    /// <summary>把正式匹配结果格式化为可检索的 Development 诊断文本。</summary>
    public static class BlackTriggerFormationDiagnosticFormatter
    {
        /// <summary>输出完整快照、逐路线真值、候选去重与侧重分配。</summary>
        public static string Format(BlackTriggerMakerSnapshot snapshot)
        {
            BlackTriggerFormationEvaluation evaluation = BlackTriggerFormationMatcher.Evaluate(snapshot);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[BDP.BlackTrigger] 来源=" + (snapshot?.MakerLabel ?? "<null>")
                + " | LoadID=" + (snapshot?.MakerLoadId ?? "<null>")
                + " | 方式=" + snapshot?.Source);
            builder.AppendLine("冻结事实：");
            foreach (BlackTriggerFactSnapshot fact in snapshot?.Facts ?? new List<BlackTriggerFactSnapshot>())
            {
                builder.AppendLine("  " + fact.StableKey + " | " + BlackTriggerImprintFormatter.FormatFact(fact));
            }
            builder.AppendLine("逐路线判断：");
            foreach (BlackTriggerRouteEvaluation route in evaluation.RouteEvaluations)
            {
                builder.AppendLine("  " + (route.ThingDef?.LabelCap.ToString() ?? "无") + " [" + route.ThingDef?.defName + "] / " + route.RouteId
                    + " | 结果=" + (route.Passed ? "通过" : "未通过")
                    + " | 命中=" + Join(route.MatchedFactKeys)
                    + " | 失败=" + Join(route.FailureDetails)
                    + " | 侧重=" + (route.Emphasis?.LabelCap.ToString() ?? "无"));
            }
            builder.AppendLine("去重候选与侧重分配：");
            foreach (BlackTriggerCandidatePlan plan in evaluation.CandidatePlans)
            {
                builder.AppendLine("  " + plan.ThingDef.LabelCap.ToString() + " [" + plan.ThingDef.defName + "]"
                    + " | MatchedRoutes=" + Join(plan.MatchedRoutes.Select(value => value.RouteId))
                    + " | AssignedEmphasis=" + (plan.AssignedEmphasis?.LabelCap.ToString() ?? "无"));
            }
            return builder.ToString().TrimEnd();
        }

        /// <summary>统一处理空诊断集合。</summary>
        private static string Join(IEnumerable<string> values)
        {
            string[] items = values?.Where(value => !string.IsNullOrEmpty(value)).ToArray() ?? new string[0];
            return items.Length == 0 ? "无" : string.Join("、", items);
        }
    }
}
