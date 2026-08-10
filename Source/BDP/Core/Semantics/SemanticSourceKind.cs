namespace BDP.Core.Semantics
{
    /// <summary>
    /// 语义上下文的来源种类。
    /// 第一阶段先只覆盖当前最明确的几类来源。
    /// </summary>
    public enum SemanticSourceKind
    {
        /// <summary>
        /// 未明确分类时使用。
        /// </summary>
        Unknown,

        /// <summary>
        /// 攻击行为。
        /// </summary>
        AttackAction,

        /// <summary>
        /// 崩裂触发。
        /// </summary>
        CollapseTrigger,

        /// <summary>
        /// 资源变化。
        /// </summary>
        ResourceChange
    }
}
