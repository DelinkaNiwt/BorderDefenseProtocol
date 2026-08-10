using System;
using System.Collections.Generic;
using BDP.Core.Trion;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Expressions.Runtime
{
    /// <summary>
    /// 把最终可用表达的持续 Trion 费用对账到中央账本。
    /// 它不保存第二份运行真值，每次发布都以正式表达快照和账本快照重新核对。
    /// </summary>
    internal sealed class ExpressionSustainDrainService
    {
        /// <summary>
        /// 对账指定 Pawn 当前最终表达应有的持续消耗。
        /// 空结果会注销全部 Expression 领域旧账，读档后的首次发布会自动重建。
        /// </summary>
        internal void Reconcile(Pawn pawn, ExpressionSnapshot snapshot)
        {
            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (reader == null || commands == null)
            {
                return;
            }

            IReadOnlyDictionary<TrionDrainKey, float> current = reader.GetDrainSnapshot();
            Dictionary<TrionDrainKey, float> desired = BuildDesiredDrains(pawn, snapshot);

            if (current != null)
            {
                foreach (KeyValuePair<TrionDrainKey, float> pair in current)
                {
                    if (string.Equals(pair.Key.Domain, "Expression", StringComparison.Ordinal)
                        && !desired.ContainsKey(pair.Key))
                    {
                        commands.UnregisterDrain(pair.Key);
                    }
                }
            }

            foreach (KeyValuePair<TrionDrainKey, float> pair in desired)
            {
                float existing;
                if (current != null
                    && current.TryGetValue(pair.Key, out existing)
                    && Math.Abs(existing - pair.Value) < 0.0001f)
                {
                    continue;
                }

                commands.RegisterDrain(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// 按相同最终效果身份分组，并计算各组应登记的每秒总费用。
        /// 只有最终可用且显式配置费用表的表达才参与。
        /// </summary>
        private static Dictionary<TrionDrainKey, float> BuildDesiredDrains(
            Pawn pawn,
            ExpressionSnapshot snapshot)
        {
            Dictionary<TrionDrainKey, List<FormalExpressionResult>> groups =
                new Dictionary<TrionDrainKey, List<FormalExpressionResult>>();
            IReadOnlyList<FormalExpressionResult> results = snapshot != null ? snapshot.Results : null;
            if (results == null)
            {
                return new Dictionary<TrionDrainKey, float>();
            }

            for (int i = 0; i < results.Count; i++)
            {
                FormalExpressionResult result = results[i];
                if (result == null
                    || !result.IsAvailable
                    || (result.UseRequirementCheck != null
                        && !result.UseRequirementCheck.Satisfied)
                    || result.Trion == null
                    || result.Trion.SustainCostBySourceCount == null
                    || result.Trion.SustainCostBySourceCount.Count == 0)
                {
                    continue;
                }

                TrionDrainKey key = ExpressionSustainDrainKeyFactory.Create(result);
                List<FormalExpressionResult> group;
                if (!groups.TryGetValue(key, out group))
                {
                    group = new List<FormalExpressionResult>();
                    groups.Add(key, group);
                }

                group.Add(result);
            }

            Dictionary<TrionDrainKey, float> desired = new Dictionary<TrionDrainKey, float>();
            foreach (KeyValuePair<TrionDrainKey, List<FormalExpressionResult>> pair in groups)
            {
                List<FormalExpressionResult> group = pair.Value;
                group.Sort(CompareResultIdentity);
                IReadOnlyList<ExpressionSustainCostBySourceCountConfig> selectedTable =
                    group[0].Trion.SustainCostBySourceCount;
                WarnIfTablesDiffer(pawn, pair.Key, group, selectedTable);

                float totalPerSecond = ExpressionSustainCostPolicy.ResolveTotalPerSecond(
                    selectedTable,
                    group.Count);
                if (totalPerSecond > 0f)
                {
                    desired[pair.Key] = totalPerSecond;
                }
            }

            return desired;
        }

        /// <summary>
        /// 按正式结果标识提供稳定排序，使冲突配置的选择不受槽位遍历顺序影响。
        /// </summary>
        private static int CompareResultIdentity(FormalExpressionResult left, FormalExpressionResult right)
        {
            return StringComparer.Ordinal.Compare(
                left != null ? left.Id ?? string.Empty : string.Empty,
                right != null ? right.Id ?? string.Empty : string.Empty);
        }

        /// <summary>
        /// 同一最终效果出现不同费用表时记录一次诊断，并继续采用稳定排序后的第一张表。
        /// 合法定义应让同组来源保持完全一致。
        /// </summary>
        private static void WarnIfTablesDiffer(
            Pawn pawn,
            TrionDrainKey key,
            IReadOnlyList<FormalExpressionResult> group,
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> selectedTable)
        {
            for (int i = 1; i < group.Count; i++)
            {
                IReadOnlyList<ExpressionSustainCostBySourceCountConfig> candidate =
                    group[i].Trion.SustainCostBySourceCount;
                if (AreSameTable(selectedTable, candidate))
                {
                    continue;
                }

                string pawnId = pawn != null ? pawn.ThingID : "null";
                BdpDiagnostics.Once(
                    "expression.sustain_table_conflict." + pawnId + "." + key,
                    "同一最终表达效果收到不同的持续 Trion 费用表；已按稳定结果顺序采用第一张。pawn="
                    + pawnId
                    + ", key="
                    + key);
                return;
            }
        }

        /// <summary>
        /// 判断两张持续费用表的档位和值是否完全一致。
        /// </summary>
        private static bool AreSameTable(
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> left,
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> right)
        {
            int leftCount = left != null ? left.Count : 0;
            int rightCount = right != null ? right.Count : 0;
            if (leftCount != rightCount)
            {
                return false;
            }

            for (int i = 0; i < leftCount; i++)
            {
                ExpressionSustainCostBySourceCountConfig leftRow = left[i];
                ExpressionSustainCostBySourceCountConfig rightRow = right[i];
                if (leftRow == null
                    || rightRow == null
                    || leftRow.SourceCount != rightRow.SourceCount
                    || Math.Abs(leftRow.TotalPerSecond - rightRow.TotalPerSecond) >= 0.0001f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
