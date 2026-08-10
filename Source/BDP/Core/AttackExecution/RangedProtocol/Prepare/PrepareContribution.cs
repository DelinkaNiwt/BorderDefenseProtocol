using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;

namespace BDP.Core.AttackExecution.RangedProtocol.Prepare
{
    /// <summary>
    /// Prepare 阶段模块贡献。
    /// 它只允许模块影响准备事实，不允许直接改 projectile 或在途飞行状态。
    /// </summary>
    public sealed class PrepareContribution
    {
        /// <summary>
        /// 当前模块提交的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        public float AddedResourceCost { get; set; }

        public bool HasMinimumRequiredCandidate { get; set; }

        public float MinimumRequiredCandidate { get; set; }

        public bool SkipResourceConsumption { get; set; }

        public bool HasWarmupTicksCandidate { get; set; }

        public int WarmupTicksCandidate { get; set; }

        public bool HasChargeTicksCandidate { get; set; }

        public int ChargeTicksCandidate { get; set; }

        public bool RequiresLock { get; set; }

        public bool LockSatisfied { get; set; } = true;

        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
