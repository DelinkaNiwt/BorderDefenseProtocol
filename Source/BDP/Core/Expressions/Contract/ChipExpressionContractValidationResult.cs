using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达契约解释后的最小校验结果。
    /// 第一版先记录是否有效、错误和警告。
    /// </summary>
    public sealed class ChipExpressionContractValidationResult
    {
        /// <summary>
        /// 当前契约是否通过最小校验。
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// 当前契约校验发现的错误列表。
        /// </summary>
        public List<string> Errors;

        /// <summary>
        /// 当前契约校验发现的警告列表。
        /// </summary>
        public List<string> Warnings;
    }
}
