using System.Collections.Generic;

namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 资源系统正式只读面。
    /// 这一面只暴露资源 owner 自己持有的事实，不混入战斗体或 Trigger 语义。
    /// </summary>
    public interface ITrionReader
    {
        /// <summary>
        /// 当前 Trion 总量。
        /// </summary>
        float Cur { get; }

        /// <summary>
        /// 当前最大 Trion 容量。
        /// </summary>
        float Max { get; }

        /// <summary>
        /// 已经被正式锁定的量。
        /// </summary>
        float Allocated { get; }

        /// <summary>
        /// 已声明但尚未转成正式锁定的预占用量。
        /// </summary>
        float Reserved { get; }

        /// <summary>
        /// 当前还可自由支配的量。
        /// </summary>
        float Available { get; }

        /// <summary>
        /// 当前每日自然恢复量。
        /// </summary>
        float RecoveryPerDay { get; }

        /// <summary>
        /// 当前聚合持续消耗总速率，单位为 Trion/秒。
        /// </summary>
        float TotalDrainPerSecond { get; }

        /// <summary>
        /// 读取当前持续消耗注册表快照。
        /// 这是只读 UI 和外部展示层允许读取的正式资料，不允许拿它改写账本。
        /// </summary>
        IReadOnlyDictionary<TrionDrainKey, float> GetDrainSnapshot();

        /// <summary>
        /// 当前是否冻结自然恢复。
        /// </summary>
        bool Frozen { get; }

        /// <summary>
        /// 角色尚未被腺体解锁的永久潜在容量。
        /// </summary>
        int TrionCapacityPotential { get; }

        /// <summary>
        /// 角色永久不变的先天 Trion 释放力。
        /// </summary>
        int InnateTrionIntensity { get; }
    }
}
