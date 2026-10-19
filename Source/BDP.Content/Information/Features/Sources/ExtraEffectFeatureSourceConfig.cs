using BDP.Core.Information;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>
    /// BDP 额外效果来源的稳定扩展基类。
    /// 具体效果必须使用自己的来源子类和 Worker，不根据入口类型猜测属性。
    /// </summary>
    public abstract class ExtraEffectFeatureSourceConfig : ExpressionEntryFeatureSourceConfig
    {
        /// <summary>要求具体额外效果子类显式绑定业务 Worker。</summary>
        protected ExtraEffectFeatureSourceConfig()
        {
            WorkerClass = null;
        }
    }
}
