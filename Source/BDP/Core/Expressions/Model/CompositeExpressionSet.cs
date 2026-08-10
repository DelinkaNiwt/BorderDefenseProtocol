using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// Main / Sub 高层重算后形成的结果集合。
    /// 这里只收高层新结果，不收原始单侧结果。
    /// </summary>
    internal sealed class CompositeExpressionSet
    {
        /// <summary>
        /// 双武器类高层结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> DualWeaponResults { get; set; }

        /// <summary>
        /// 组合技类高层结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> ComboResults { get; set; }

        /// <summary>
        /// 非攻击联动类高层结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> NonCombatCompositeResults { get; set; }

        /// <summary>
        /// 当前全部高层结果的来源引用关系。
        /// 它只记录身份映射，不代替正式结果对象本身。
        /// </summary>
        public IReadOnlyList<CompositeExpressionReference> References { get; set; }
    }
}
