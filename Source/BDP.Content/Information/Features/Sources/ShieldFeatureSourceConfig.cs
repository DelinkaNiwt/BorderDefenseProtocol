using BDP.Core.Information;
using BDP.Content.Information.Features.Workers;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>读取护盾功能的正式抵挡参数。</summary>
    public sealed class ShieldFeatureSourceConfig : ExpressionEntryFeatureSourceConfig
    {
        /// <summary>是否读取聚合护盾状态下的抵挡参数。</summary>
        public bool UseStackedValues;

        /// <summary>默认读取单枚护盾参数。</summary>
        public ShieldFeatureSourceConfig()
        {
            UseStackedValues = false;
            WorkerClass = typeof(ShieldFeatureSourceWorker);
        }
    }
}
