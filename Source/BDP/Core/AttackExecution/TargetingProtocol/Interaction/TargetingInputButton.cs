namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 目标交互这一轮输入所对应的鼠标按钮事实。
    /// 它只描述玩家按下了哪个按钮，不解释任何业务语义。
    /// </summary>
    public enum TargetingInputButton
    {
        /// <summary>
        /// 当前没有已知按钮事实。
        /// </summary>
        None = 0,

        /// <summary>
        /// 当前输入来自鼠标左键。
        /// </summary>
        Left = 1,

        /// <summary>
        /// 当前输入来自鼠标右键。
        /// </summary>
        Right = 2,

        /// <summary>
        /// 当前输入来自鼠标中键。
        /// </summary>
        Middle = 3
    }
}
