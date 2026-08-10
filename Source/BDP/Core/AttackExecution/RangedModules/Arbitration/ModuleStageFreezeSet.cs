using System.Collections.Generic;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 单阶段冻结集合。
    /// 一旦某维度被冻结，后续模块不得再覆盖该维度。
    /// </summary>
    internal sealed class ModuleStageFreezeSet
    {
        /// <summary>
        /// 当前阶段已经冻结的维度键集合。
        /// </summary>
        private readonly HashSet<string> frozenDimensions = new HashSet<string>();

        /// <summary>
        /// 冻结指定维度，阻止后续模块继续覆盖它。
        /// </summary>
        public void Freeze(string dimensionKey)
        {
            if (!string.IsNullOrWhiteSpace(dimensionKey))
            {
                frozenDimensions.Add(dimensionKey);
            }
        }

        /// <summary>
        /// 判断指定维度当前是否已经被冻结。
        /// </summary>
        public bool IsFrozen(string dimensionKey)
        {
            return !string.IsNullOrWhiteSpace(dimensionKey) && frozenDimensions.Contains(dimensionKey);
        }
    }
}
