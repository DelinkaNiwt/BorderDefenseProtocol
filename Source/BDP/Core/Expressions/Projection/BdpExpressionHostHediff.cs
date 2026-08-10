using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// BDP 表达系统正式发布到原版 <see cref="Pawn_HealthTracker"/> 时使用的 Hediff 宿主基类。
    /// 它只负责把“这是表达宿主 Def”落成结构事实，不承担额外托管平台职责。
    /// </summary>
    public class BdpExpressionHostHediff : HediffWithComps
    {
        /// <summary>
        /// 当前 Hediff 宿主绑定的正式表达结果。
        /// 这些结果只作为运行时投影缓存，读档后由表达宿主同步链重建。
        /// </summary>
        private readonly List<FormalExpressionResult> expressionResults = new List<FormalExpressionResult>();

        /// <summary>
        /// 当前 Hediff 宿主绑定的正式表达结果只读视图。
        /// </summary>
        internal IReadOnlyList<FormalExpressionResult> ExpressionResults
        {
            get { return expressionResults; }
        }

        /// <summary>
        /// 同步当前 Hediff 宿主对应的正式表达结果集合。
        /// </summary>
        internal void SyncExpressionResults(IReadOnlyList<FormalExpressionResult> results)
        {
            expressionResults.Clear();
            if (results == null)
            {
                return;
            }

            for (int index = 0; index < results.Count; index++)
            {
                FormalExpressionResult result = results[index];
                if (result != null)
                {
                    expressionResults.Add(result);
                }
            }
        }
    }
}
