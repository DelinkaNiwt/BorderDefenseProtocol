using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 正式表达结果的四类并联索引。
    /// 它只服务读取，不改变结果生成与发布行为。
    /// </summary>
    internal sealed class ExpressionChannelIndex
    {
        /// <summary>
        /// 当前已发布的全部正式结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> AllResults { get; set; }

        /// <summary>
        /// 当前已发布的全部 Verb 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> VerbResults { get; set; }

        /// <summary>
        /// 当前已发布的全部 Ability 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> AbilityResults { get; set; }

        /// <summary>
        /// 当前已发布的全部 Hediff 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> HediffResults { get; set; }

        /// <summary>
        /// 当前已发布的全部 Passive 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> PassiveResults { get; set; }

        /// <summary>
        /// 按 Ability DefName 建好的结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<FormalExpressionResult>> AbilityResultsByDefName { get; set; }

        /// <summary>
        /// 按 Hediff DefName 建好的结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<FormalExpressionResult>> HediffResultsByDefName { get; set; }

        /// <summary>
        /// 按 PassiveKey 建好的结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<FormalExpressionResult>> PassiveResultsByKey { get; set; }

        /// <summary>
        /// 构建一份空的四类并联索引。
        /// </summary>
        internal static ExpressionChannelIndex Empty()
        {
            return new ExpressionChannelIndex
            {
                AllResults = new List<FormalExpressionResult>(),
                VerbResults = new List<FormalExpressionResult>(),
                AbilityResults = new List<FormalExpressionResult>(),
                HediffResults = new List<FormalExpressionResult>(),
                PassiveResults = new List<FormalExpressionResult>(),
                AbilityResultsByDefName = new Dictionary<string, IReadOnlyList<FormalExpressionResult>>(),
                HediffResultsByDefName = new Dictionary<string, IReadOnlyList<FormalExpressionResult>>(),
                PassiveResultsByKey = new Dictionary<string, IReadOnlyList<FormalExpressionResult>>()
            };
        }
    }
}
