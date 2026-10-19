using BDP.Core.Information;
using BDP.Content.Information.Features.Workers;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>读取远程攻击结果、精度与投射物事实。</summary>
    public sealed class RangedAttackFeatureSourceConfig : ExpressionEntryFeatureSourceConfig
    {
        /// <summary>默认使用远程攻击语义 Worker。</summary>
        public RangedAttackFeatureSourceConfig()
        {
            WorkerClass = typeof(RangedAttackFeatureSourceWorker);
        }
    }
}
