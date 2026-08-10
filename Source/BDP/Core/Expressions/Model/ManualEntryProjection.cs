using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 手动入口投影结果。
    /// 它只保存“玩家当前可操作什么”，不直接等于具体按钮控件。
    /// </summary>
    internal sealed class ManualEntryProjection
    {
        /// <summary>
        /// 当前已正式生成的手动入口组集合。
        /// </summary>
        public IReadOnlyList<ManualEntryProjectionGroup> Groups { get; set; }
    }
}
