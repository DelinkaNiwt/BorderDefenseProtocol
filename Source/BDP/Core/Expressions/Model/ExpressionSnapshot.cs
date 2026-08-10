using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 当前这一刻完整的表达结果总表。
    /// 它是内部运算链的统一收口对象，不承担重新计算职责。
    /// </summary>
    internal sealed class ExpressionSnapshot
    {
        /// <summary>
        /// 当前全部正式结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> Results { get; set; }

        /// <summary>
        /// 当前默认主远程结果。
        /// </summary>
        public FormalExpressionResult PrimaryRanged { get; set; }

        /// <summary>
        /// 当前默认主近战结果。
        /// </summary>
        public FormalExpressionResult PrimaryMelee { get; set; }

        /// <summary>
        /// 当前高层复合结果的来源引用集合。
        /// 它只记录复合结果由哪些单侧结果组成，不承担重新计算职责。
        /// </summary>
        public IReadOnlyList<CompositeExpressionReference> CompositeReferences { get; set; }

        /// <summary>
        /// 当前执行表达。
        /// </summary>
        public FormalExpressionResult CurrentExecuting { get; set; }

        /// <summary>
        /// 当前结果表对应的发布观察快照。
        /// 它只服务说明和排查，不作为运行时真值。
        /// </summary>
        public ExpressionPublicationSnapshot PublicationSnapshot { get; set; }

        /// <summary>
        /// 当前是否存在 Special 武器类拦截。
        /// </summary>
        public bool HasSpecialWeaponOverride { get; set; }
    }
}
