using BDP.Core.Information;
using BDP.Content.Information.Features.Workers;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>读取一项近战攻击方式对应的原版 Tool 表面。</summary>
    public sealed class MeleeAttackMethodFeatureSourceConfig : ExpressionEntryFeatureSourceConfig
    {
        /// <summary>作者声明顺序中的零基 Tool 索引；默认读取第一把 Tool。</summary>
        public int ToolIndex;

        /// <summary>默认使用逐 Tool 近战语义 Worker。</summary>
        public MeleeAttackMethodFeatureSourceConfig()
        {
            ToolIndex = 0;
            WorkerClass = typeof(MeleeAttackMethodFeatureSourceWorker);
        }
    }
}
