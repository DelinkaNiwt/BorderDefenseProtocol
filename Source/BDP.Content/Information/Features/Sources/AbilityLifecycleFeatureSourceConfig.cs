using BDP.Core.Information;
using BDP.Content.Information.Features.Workers;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>读取能力共用施放生命周期与来源级资源信息。</summary>
    public sealed class AbilityLifecycleFeatureSourceConfig : ExpressionEntryFeatureSourceConfig
    {
        /// <summary>默认使用能力生命周期语义 Worker。</summary>
        public AbilityLifecycleFeatureSourceConfig()
        {
            WorkerClass = typeof(AbilityLifecycleFeatureSourceWorker);
        }
    }
}
