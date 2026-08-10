using System.Collections.Generic;
using BDP.Core.Trigger;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 某一侧当前正式成立的结果集合。
    /// 它只回答“这一侧成立了什么”，不处理双侧高层关系。
    /// </summary>
    internal sealed class SingleSideExpressionSet
    {
        /// <summary>
        /// 当前结果集合对应的侧别。
        /// </summary>
        public TriggerSide Side { get; set; }

        /// <summary>
        /// 这一侧全部正式结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> Results { get; set; }

        /// <summary>
        /// 这一侧的武器类结果。
        /// 这里只是当前侧自己的结果切片，不代表全局唯一化裁定。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> WeaponResults { get; set; }
    }
}
