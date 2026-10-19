using BDP.Core.Information;
using BDP.Content.Information.Features.Workers;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>读取一项机动方式的目标、距离与动作参数。</summary>
    public sealed class MovementMethodFeatureSourceConfig : ExpressionEntryFeatureSourceConfig
    {
        /// <summary>默认使用机动方式语义 Worker。</summary>
        public MovementMethodFeatureSourceConfig()
        {
            WorkerClass = typeof(MovementMethodFeatureSourceWorker);
        }
    }
}
