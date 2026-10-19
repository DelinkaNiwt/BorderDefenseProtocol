namespace BDP.Core.Expressions
{
    /// <summary>
    /// Hediff 宿主生命周期控制模式键。
    /// 这些键属于 Core 通用表达设施，业务内容只通过 XML 选择模式。
    /// </summary>
    internal static class HediffControlModeKeys
    {
        /// <summary>
        /// 自动由 Hediff 同步器维护。
        /// </summary>
        internal const string Automatic = "Automatic";

        /// <summary>
        /// 由 Ability 按钮手动切换，不能由 Hediff 同步器主动添加。
        /// </summary>
        internal const string AbilityButton = "AbilityButton";

        /// <summary>
        /// 判断指定模式是否为能力按钮控制模式。
        /// </summary>
        internal static bool IsAbilityButton(string mode)
        {
            return string.Equals(mode, AbilityButton, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
