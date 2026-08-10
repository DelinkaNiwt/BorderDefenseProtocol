namespace BDP.Core.Expressions
{
    /// <summary>
    /// 单个手动入口项。
    /// 它只表达“这个入口是什么”，不直接绑定 UI 控件。
    /// </summary>
    internal sealed class ManualEntryProjectionItem
    {
        /// <summary>
        /// 当前入口项稳定标识。
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// 当前入口项显示名。
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// 当前入口项手动按钮贴图路径。
        /// </summary>
        public string ManualEntryIconTexPath { get; set; }

        /// <summary>
        /// 当前入口项关联的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前入口项是否属于主入口。
        /// 它只表达投影身份，不承担默认选择逻辑。
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// 当前入口项对应的表达结果类型。
        /// </summary>
        public ExpressionResultKind ResultKind { get; set; }

        /// <summary>
        /// 当前入口项对应的武器模式。
        /// 非武器类结果默认为 None。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }
    }
}
