namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 当前对外动作语义类型。
    /// 它只表达“外部此刻应把它理解成什么动作”，不代表命令一定可执行。
    /// </summary>
    public enum TriggerInteractionOperationKind
    {
        // 当前没有正式动作语义。
        None,
        // 当前应被理解成激活动作。
        Activate,
        // 当前应被理解成切换到该槽位。
        SwitchTo,
        // 当前应被理解成关闭当前激活。
        Deactivate,
        // 当前槽位只是镜像受控位，不应被理解成独立动作入口。
        Mirror,
        // 当前槽位或侧别没有可供正式提交的动作。
        Unavailable
    }
}
