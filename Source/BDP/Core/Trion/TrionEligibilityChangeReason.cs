namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 实际使用资格变化的来源。
    /// </summary>
    public enum TrionEligibilityChangeReason
    {
        /// <summary>普通派生值重算，不改写当前量语义。</summary>
        Recalculate,

        /// <summary>角色创建阶段已有腺体，随后由首次资源初始化填满。</summary>
        InitialSetup,

        /// <summary>游戏运行中获得或重新激活腺体，从零开始积累。</summary>
        RuntimeGranted,

        /// <summary>腺体被移除或失活，实际容量归零。</summary>
        Lost
    }
}
