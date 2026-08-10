using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 当前这一轮目标交互输入帧。
    /// 它只描述输入事实，不解释业务语义。
    /// </summary>
    public sealed class TargetingInputFrame
    {
        /// <summary>
        /// 当前鼠标悬停目标。
        /// </summary>
        public LocalTargetInfo HoveredTarget { get; set; }

        /// <summary>
        /// 当前玩家选中的目标。
        /// </summary>
        public LocalTargetInfo SelectedTarget { get; set; }

        /// <summary>
        /// 当前这一轮输入对应的鼠标按钮事实。
        /// 它只描述玩家按了哪个按钮，不解释业务语义。
        /// </summary>
        public TargetingInputButton PressedButton { get; set; } = TargetingInputButton.None;

        /// <summary>
        /// 当前这一轮输入附带的修饰键事实。
        /// 它只描述玩家按住了哪些修饰键，不解释业务语义。
        /// </summary>
        public TargetingInputModifiers Modifiers { get; set; } = TargetingInputModifiers.None;

        /// <summary>
        /// 当前这一轮是否请求进入确认。
        /// </summary>
        public bool ConfirmRequested { get; set; }

        /// <summary>
        /// 当前这一轮是否请求取消。
        /// </summary>
        public bool CancelRequested { get; set; }
    }
}
