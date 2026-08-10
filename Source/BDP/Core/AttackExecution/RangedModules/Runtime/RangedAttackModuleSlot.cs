namespace BDP.Core.AttackExecution.RangedModules.Runtime
{
    /// <summary>
    /// 单个模块挂载实例在运行时会话中的槽位。
    /// 槽位顺序与 XML 挂载顺序一致。
    /// </summary>
    public sealed class RangedAttackModuleSlot
    {
        /// <summary>
        /// 当前槽位对应的挂载顺序索引。
        /// </summary>
        public int MountIndex { get; set; }

        /// <summary>
        /// 当前槽位绑定的模块运行时实例。
        /// </summary>
        public IRangedAttackModuleRuntime Runtime { get; set; }

    }
}
