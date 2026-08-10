using System.Collections.Generic;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义最低合法性校验结果。
    /// </summary>
    internal sealed class ComboDefinitionValidationResult
    {
        /// <summary>
        /// 当前组合技定义是否通过最低正式校验。
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// 当前组合技定义的错误集合。
        /// </summary>
        public IReadOnlyList<ComboDefinitionValidationMessage> Errors;

        /// <summary>
        /// 当前组合技定义的警告集合。
        /// </summary>
        public IReadOnlyList<ComboDefinitionValidationMessage> Warnings;
    }
}
