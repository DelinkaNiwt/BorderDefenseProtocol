namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技单个字段的求值结果。
    /// 它同时保留显式值、求值模式和最终取到的值，方便后续排查。
    /// </summary>
    internal sealed class ComboResolvedFieldValue<TValue>
    {
        /// <summary>
        /// 当前字段是否存在显式值。
        /// </summary>
        public bool HasExplicitValue;

        /// <summary>
        /// 当前字段的显式值。
        /// </summary>
        public TValue ExplicitValue;

        /// <summary>
        /// 当前字段实际采用的求值模式。
        /// </summary>
        public ComboValueResolveMode? ResolveMode;

        /// <summary>
        /// 当前字段是否已成功得出正式值。
        /// </summary>
        public bool HasResolvedValue;

        /// <summary>
        /// 当前字段最终解析得到的值。
        /// </summary>
        public TValue ResolvedValue;
    }
}
