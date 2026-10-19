using BDP.Core.Information;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>
    /// 以表达条目 Id 定位功能事实的标准来源配置。
    /// </summary>
    public abstract class ExpressionEntryFeatureSourceConfig : ChipFeatureSourceConfig
    {
        /// <summary>表达条目来源必须至少定位一个表达条目。</summary>
        public override bool RequiresExpressionEntryReference => true;

    }
}
