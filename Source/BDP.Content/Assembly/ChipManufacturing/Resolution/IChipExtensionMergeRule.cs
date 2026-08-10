using System;
using BDP.Core.Chips;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>一种芯片扩展参与双动作合并时的显式规则。</summary>
    public interface IChipExtensionMergeRule
    {
        /// <summary>当前规则负责的扩展具体类型。</summary>
        Type ExtensionType { get; }

        /// <summary>把同类型扩展合成一个新结果。</summary>
        ChipExtensionConfig Merge(ChipExtensionConfig first, ChipExtensionConfig second);
    }
}
