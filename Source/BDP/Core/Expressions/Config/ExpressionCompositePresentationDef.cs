using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 复合表达手动入口的表现定义。
    /// 它只负责给 Dual / Combo 这类没有单一芯片物品归属的按钮提供显式贴图路径。
    /// </summary>
    public sealed class ExpressionCompositePresentationDef : Def
    {
        /// <summary>
        /// 当前表现定义对应的复合表达种类。
        /// </summary>
        public string CompositeKind;

        /// <summary>
        /// 当前表现定义对应的武器模式。
        /// 允许保留 None 作为通配。
        /// </summary>
        public string WeaponMode;

        /// <summary>
        /// 当前表现定义若只针对某个组合技，则这里填写 ComboDefName。
        /// 留空表示不限制具体组合技。
        /// </summary>
        public string ComboDefName;

        /// <summary>
        /// 当前手动攻击入口按钮要使用的贴图路径。
        /// </summary>
        public string ManualEntryIconTexPath;
    }
}
