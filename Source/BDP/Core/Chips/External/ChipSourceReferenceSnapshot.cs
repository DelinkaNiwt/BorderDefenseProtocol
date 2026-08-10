using System.Collections.Generic;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 从实例提供器复制出的稳定只读来源快照。
    /// </summary>
    public sealed class ChipSourceReferenceSnapshot
    {
        /// <summary>按业务顺序保存的来源键。</summary>
        public IReadOnlyList<string> OrderedSourceKeys { get; set; }

        /// <summary>可空的来源变体键。</summary>
        public string SourceVariantKey { get; set; }

        /// <summary>可空的来源变体显示标签。</summary>
        public string SourceVariantLabel { get; set; }
    }
}
