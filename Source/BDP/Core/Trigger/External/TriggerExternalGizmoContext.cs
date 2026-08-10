using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 提供给外部按钮提供器的正式能力上下文。
    /// 它把正式读取、正式输入和正式诊断三类能力显式拆开，避免外部层再把“只读扩展”和“正式请求”混成一口。
    /// </summary>
    public sealed class TriggerExternalGizmoContext
    {
        /// <summary>
        /// 当前 Trigger 的正式读取口。
        /// 外部扩展如只需要观察状态，应只读取这组能力。
        /// </summary>
        public ITriggerLoadoutReader LoadoutReader { get; set; }

        /// <summary>
        /// 当前 Trigger 的正式交互语义读取口。
        /// 外部调用者应优先通过它理解“此刻该把某个槽位或某一侧看成什么动作”。
        /// </summary>
        public ITriggerInteractionReader InteractionReader { get; set; }

        /// <summary>
        /// 当前 Trigger 的正式输入口。
        /// 只有确实需要提交正式动作请求的扩展，才应使用这组能力。
        /// </summary>
        public ITriggerLoadoutCommands LoadoutCommands { get; set; }

        /// <summary>
        /// 当前装备该触发体的 Pawn。
        /// 如果没有宿主，这里会是 null。
        /// </summary>
        public Pawn OwnerPawn { get; set; }
    }
}
