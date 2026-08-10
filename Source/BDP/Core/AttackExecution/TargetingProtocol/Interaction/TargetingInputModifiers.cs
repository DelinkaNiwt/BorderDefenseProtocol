using System;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 目标交互这一轮输入附带的修饰键事实。
    /// 它只描述玩家按住了哪些修饰键，不解释任何业务含义。
    /// </summary>
    [Flags]
    public enum TargetingInputModifiers
    {
        /// <summary>
        /// 当前没有修饰键。
        /// </summary>
        None = 0,

        /// <summary>
        /// 当前按住了 Shift 键。
        /// </summary>
        Shift = 1 << 0,

        /// <summary>
        /// 当前按住了 Ctrl 键。
        /// </summary>
        Control = 1 << 1,

        /// <summary>
        /// 当前按住了 Alt 键。
        /// </summary>
        Alt = 1 << 2
    }
}
