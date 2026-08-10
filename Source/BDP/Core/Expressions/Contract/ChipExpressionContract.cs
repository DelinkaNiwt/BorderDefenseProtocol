using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 主模组正式承认的一枚芯片表达契约。
    /// 它不是作者原始写法，
    /// 而是经过解释后可被主模组消费的契约对象。
    /// </summary>
    public sealed class ChipExpressionContract
    {
        /// <summary>
        /// 当前芯片正式承认的基础条目集合。
        /// </summary>
        public List<ChipExpressionEntryContract> Entries;

        /// <summary>
        /// 多形态芯片没有运行中当前形态时采用的默认形态键。
        /// </summary>
        public string DefaultModeKey;

        /// <summary>
        /// 当前芯片正式承认的形态契约集合。
        /// </summary>
        public List<ChipExpressionModeContract> Modes;
    }
}
