using System;
using System.Collections.Generic;

namespace BDP.Core.Expressions.Runtime
{
    /// <summary>
    /// 表达持续 Trion 费用表的统一规则。
    /// 它只负责结构校验和档位选择，不负责登记运行时扣费。
    /// </summary>
    internal static class ExpressionSustainCostPolicy
    {
        /// <summary>
        /// 校验费用表是否从 1 开始连续递增，且每档费用都是有限非负值。
        /// 空表表示当前表达没有持续费用，属于合法配置。
        /// </summary>
        internal static IReadOnlyList<string> Validate(
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> rows,
            string context)
        {
            List<string> errors = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                return errors;
            }

            string prefix = string.IsNullOrWhiteSpace(context) ? "表达持续费用表" : context + " 的持续费用表";
            for (int i = 0; i < rows.Count; i++)
            {
                ExpressionSustainCostBySourceCountConfig row = rows[i];
                int expectedSourceCount = i + 1;
                if (row == null)
                {
                    errors.Add(prefix + " 第 " + expectedSourceCount + " 档为空。");
                    continue;
                }

                if (row.SourceCount != expectedSourceCount)
                {
                    errors.Add(
                        prefix
                        + " 必须从 1 开始逐档连续递增；第 "
                        + expectedSourceCount
                        + " 档实际写为 "
                        + row.SourceCount
                        + "。");
                }

                if (row.TotalPerSecond < 0f
                    || float.IsNaN(row.TotalPerSecond)
                    || float.IsInfinity(row.TotalPerSecond))
                {
                    errors.Add(
                        prefix
                        + " 第 "
                        + expectedSourceCount
                        + " 档的 TotalPerSecond 必须是有限非负数。");
                }
            }

            return errors;
        }

        /// <summary>
        /// 按最终有效来源数读取整组效果的每秒总费用。
        /// 超出最高已配置档位时沿用最后一档；空表或无有效来源时返回零。
        /// </summary>
        internal static float ResolveTotalPerSecond(
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> rows,
            int effectiveSourceCount)
        {
            if (rows == null || rows.Count == 0 || effectiveSourceCount < 1)
            {
                return 0f;
            }

            int index = Math.Min(effectiveSourceCount, rows.Count) - 1;
            ExpressionSustainCostBySourceCountConfig row = rows[index];
            return row != null && row.TotalPerSecond > 0f ? row.TotalPerSecond : 0f;
        }
    }
}
