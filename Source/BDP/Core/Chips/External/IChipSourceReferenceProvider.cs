using System.Collections.Generic;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 提供芯片实例来源身份的中性契约。
    /// </summary>
    public interface IChipSourceReferenceProvider
    {
        /// <summary>按业务顺序保存的来源键。</summary>
        IReadOnlyList<string> OrderedSourceKeys { get; }

        /// <summary>可空的来源变体键。</summary>
        string SourceVariantKey { get; }

        /// <summary>可空的来源变体显示标签。</summary>
        string SourceVariantLabel { get; }
    }
}
