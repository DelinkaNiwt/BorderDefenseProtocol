namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条已经从激活被动结果中提取的运行时增强声明。
    /// </summary>
    internal sealed class ExpressionAugmentationDeclaration
    {
        /// <summary>
        /// 发布该增强的被动正式结果。
        /// </summary>
        public FormalExpressionResult SourceResult { get; set; }

        /// <summary>
        /// 当前增强的冻结配置。
        /// </summary>
        public RangedModuleAugmentationConfig Config { get; set; }

        /// <summary>
        /// 当前增强在来源顺序中的稳定序号。
        /// </summary>
        public int Order { get; set; }
    }
}
