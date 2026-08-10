using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一组手动入口投影结果。
    /// 它表示“这一组操作入口共同对应哪条正式结果”。
    /// </summary>
    internal sealed class ManualEntryProjectionGroup
    {
        /// <summary>
        /// 当前入口组稳定标识。
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// 当前入口组对应的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前入口组显示名。
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// 当前入口组手动按钮贴图路径。
        /// </summary>
        public string ManualEntryIconTexPath { get; set; }

        /// <summary>
        /// 当前入口组内的实际操作入口列表。
        /// </summary>
        public IReadOnlyList<ManualEntryProjectionItem> Items { get; set; }
    }
}
