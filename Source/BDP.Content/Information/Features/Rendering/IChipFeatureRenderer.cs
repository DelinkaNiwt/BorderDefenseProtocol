using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>功能展示器接收的只读展示上下文。</summary>
    internal sealed class ChipFeatureRenderContext
    {
        /// <summary>当前上下文稳定键；根级功能为空。</summary>
        internal string ContextKey { get; }

        /// <summary>创建展示上下文。</summary>
        internal ChipFeatureRenderContext(string contextKey)
        {
            ContextKey = contextKey;
        }
    }

    /// <summary>
    /// 只按玩家语义结果生成展示结果的功能展示器。
    /// 展示器不得读取 Verb、Ability、Hediff 或其它实现对象。
    /// </summary>
    internal interface IChipFeatureRenderer
    {
        /// <summary>生成当前功能的专用展示行和已消费属性集合。</summary>
        ChipFeatureRenderResult Render(
            ChipInformationNode feature,
            ChipFeatureRenderContext context);
    }
}
