using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>
    /// 额外效果来源 Worker 的扩展基类。
    /// 具体业务必须由派生 Worker 显式读取，基类不猜测效果类型或属性。
    /// </summary>
    public abstract class ExtraEffectFeatureSourceWorker : IChipFeatureSourceWorker
    {
        /// <summary>由具体额外效果 Worker 实现其只读信息投影。</summary>
        public abstract IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context);
    }
}
