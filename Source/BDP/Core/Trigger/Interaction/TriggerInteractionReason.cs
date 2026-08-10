namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 交互语义结果的正式原因码。
    /// 它面向外部调用者稳定输出，不承担本地化文案职责。
    /// </summary>
    public enum TriggerInteractionReason
    {
        // 当前没有特殊原因。
        None,
        // 指定槽位不存在。
        MissingSlot,
        // 当前槽位没有装入芯片。
        EmptySlot,
        // 当前槽位被正式规则禁用。
        Disabled,
        // 当前侧或当前同步组仍处于切换过程中。
        SwitchingInProgress,
        // 当前目标正在等待与它冲突的芯片完成关闭。
        WaitingForConflicts,
        // 当前槽位只是镜像受控位。
        MirrorControlledByRoot,
        // 当前槽位已经正式激活。
        AlreadyActive,
        // 当前战斗体未开启，芯片不能进入正式战斗态。
        BattleModeUnavailable,
        // 当前角色不满足芯片声明的全部激活条件。
        ActivationRequirementsUnmet,
        // 当前侧整体没有明确的正式动作。
        NoFormalAction
    }
}
