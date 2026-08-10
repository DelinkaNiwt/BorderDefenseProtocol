using System.Collections.Generic;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 发射前准备阶段的正式结果。
    /// 它只记录资源、预热、锁定等准备事实。
    /// </summary>
    internal sealed class PrepareRecord
    {
        public bool IsAborted { get; set; }

        public string AbortReason { get; set; }

        public float ResourceCost { get; set; }

        public float MinimumRequired { get; set; }

        public bool SkipResourceConsumption { get; set; }

        public bool RequiresWarmup { get; set; }

        public int WarmupTicks { get; set; }

        public bool RequiresCharge { get; set; }

        public int ChargeTicks { get; set; }

        public bool RequiresLock { get; set; }

        public bool LockSatisfied { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }
}
