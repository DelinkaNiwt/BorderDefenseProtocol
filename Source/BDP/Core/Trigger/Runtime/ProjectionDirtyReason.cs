namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// 已发布战斗投影的失效来源。
    /// 它只描述“为什么需要重建”，不承担额外业务语义。
    /// </summary>
    internal enum ProjectionDirtyReason
    {
        /// <summary>
        /// 默认空原因。
        /// </summary>
        None = 0,

        /// <summary>
        /// Trigger 装卸内容发生变化。
        /// </summary>
        LoadoutChanged = 1,

        /// <summary>
        /// 某个槽位已经正式完成启用提交。
        /// </summary>
        SlotActivationCommitted = 2,

        /// <summary>
        /// 某个槽位已经正式停用。
        /// </summary>
        SlotDeactivated = 3,

        /// <summary>
        /// 读档后的首次正式投影发布。
        /// </summary>
        PostLoadFinalize = 4,

        /// <summary>
        /// runtime tick 中同步到了新的槽位禁用真值。
        /// </summary>
        DisableStateChanged = 5,

        /// <summary>
        /// runtime tick 中结算了到期切换上下文。
        /// </summary>
        SwitchTransitionResolved = 6,

        /// <summary>
        /// 武器已从 Pawn 身上卸下，需要清空已发布投影。
        /// </summary>
        Unequipped = 7,

        /// <summary>
        /// 战斗会话状态已变化，需要重新裁定当前是否允许发布投影。
        /// </summary>
        CombatBodySessionStateChanged = 8,

        /// <summary>
        /// 正式启用芯片的当前形态发生变化。
        /// </summary>
        ChipModeChanged = 9,

        /// <summary>
        /// Combo 自己的角色使用条件满足状态发生变化。
        /// </summary>
        ComboUseRequirementChanged = 10
    }
}

