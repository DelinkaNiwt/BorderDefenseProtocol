using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 公开表达读取面上的已发布投影快照。
    /// 它是主模组对外提供的稳定只读真值表面，不触发表达重建。
    /// </summary>
    public sealed class ExpressionPublishedProjectionSnapshot
    {
        /// <summary>
        /// 当前已发布投影版本号。
        /// </summary>
        public int ProjectionVersion { get; internal set; }

        /// <summary>
        /// 当前默认主远程结果标识。
        /// </summary>
        public string PrimaryRangedResultId { get; internal set; }

        /// <summary>
        /// 当前默认主近战结果标识。
        /// </summary>
        public string PrimaryMeleeResultId { get; internal set; }

        /// <summary>
        /// 当前执行结果标识。
        /// </summary>
        public string CurrentExecutingResultId { get; internal set; }

        /// <summary>
        /// 当前是否存在 Special 武器拦截。
        /// </summary>
        public bool HasSpecialWeaponOverride { get; internal set; }

        /// <summary>
        /// 当前已发布的全部公开结果。
        /// </summary>
        public IReadOnlyList<ExpressionPublishedResultSnapshot> Results { get; internal set; }

        /// <summary>
        /// 当前已发布的全部 Verb 结果。
        /// </summary>
        public IReadOnlyList<ExpressionPublishedResultSnapshot> VerbResults { get; internal set; }

        /// <summary>
        /// 当前已发布的全部 Ability 结果。
        /// </summary>
        public IReadOnlyList<ExpressionPublishedResultSnapshot> AbilityResults { get; internal set; }

        /// <summary>
        /// 当前已发布的全部 Hediff 结果。
        /// </summary>
        public IReadOnlyList<ExpressionPublishedResultSnapshot> HediffResults { get; internal set; }

        /// <summary>
        /// 当前已发布的全部 Passive 结果。
        /// </summary>
        public IReadOnlyList<ExpressionPublishedResultSnapshot> PassiveResults { get; internal set; }

        /// <summary>
        /// 按 ResultId 建好的公开结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, ExpressionPublishedResultSnapshot> ResultIndex { get; internal set; }

        /// <summary>
        /// 按执行槽位键建好的 Verb 结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>> VerbResultsBySlotKey { get; internal set; }

        /// <summary>
        /// 按 AbilityDefName 建好的 Ability 结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>> AbilityResultsByDefName { get; internal set; }

        /// <summary>
        /// 按 HediffDefName 建好的 Hediff 结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>> HediffResultsByDefName { get; internal set; }

        /// <summary>
        /// 按 PassiveKey 建好的 Passive 结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>> PassiveResultsByKey { get; internal set; }

        /// <summary>
        /// 按 CompositeId 建好的公开复合引用索引。
        /// </summary>
        public IReadOnlyDictionary<string, ExpressionPublishedCompositeReference> CompositeReferenceIndex { get; internal set; }

        /// <summary>
        /// 当前公开投影是否为空。
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return Results == null || Results.Count == 0;
            }
        }

        /// <summary>
        /// 按结果标识读取一条公开结果。
        /// </summary>
        public bool TryGetResult(string resultId, out ExpressionPublishedResultSnapshot result)
        {
            result = null;
            return ResultIndex != null
                && !string.IsNullOrWhiteSpace(resultId)
                && ResultIndex.TryGetValue(resultId, out result)
                && result != null;
        }

        /// <summary>
        /// 按复合结果标识读取一条公开复合引用。
        /// </summary>
        public bool TryGetCompositeReference(string compositeId, out ExpressionPublishedCompositeReference reference)
        {
            reference = null;
            return CompositeReferenceIndex != null
                && !string.IsNullOrWhiteSpace(compositeId)
                && CompositeReferenceIndex.TryGetValue(compositeId, out reference)
                && reference != null;
        }

        /// <summary>
        /// 构建一份稳定空快照。
        /// </summary>
        internal static ExpressionPublishedProjectionSnapshot Empty()
        {
            return new ExpressionPublishedProjectionSnapshot
            {
                Results = new List<ExpressionPublishedResultSnapshot>(),
                VerbResults = new List<ExpressionPublishedResultSnapshot>(),
                AbilityResults = new List<ExpressionPublishedResultSnapshot>(),
                HediffResults = new List<ExpressionPublishedResultSnapshot>(),
                PassiveResults = new List<ExpressionPublishedResultSnapshot>(),
                ResultIndex = new Dictionary<string, ExpressionPublishedResultSnapshot>(),
                VerbResultsBySlotKey = new Dictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>>(),
                AbilityResultsByDefName = new Dictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>>(),
                HediffResultsByDefName = new Dictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>>(),
                PassiveResultsByKey = new Dictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>>(),
                CompositeReferenceIndex = new Dictionary<string, ExpressionPublishedCompositeReference>()
            };
        }
    }
}
