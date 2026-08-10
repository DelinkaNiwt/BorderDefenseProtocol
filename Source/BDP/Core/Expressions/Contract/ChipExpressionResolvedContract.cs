namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达契约解释后的最小结果对象。
    /// 它把正式契约和校验结果打包返回给下游。
    /// </summary>
    public sealed class ChipExpressionResolvedContract
    {
        /// <summary>
        /// 当前芯片在主模组眼中的正式契约。
        /// </summary>
        public ChipExpressionContract Contract;

        /// <summary>
        /// 当前契约的最小校验结果。
        /// </summary>
        public ChipExpressionContractValidationResult Validation;
    }
}
