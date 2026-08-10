namespace BDP.Core.Expressions
{
    /// <summary>
    /// 视觉层解析当前执行焦点时使用的静态政策。
    /// 真正的当轮焦点标识由 TriggerVisualRuntimeState（视觉运行时状态）提供。
    /// </summary>
    internal enum VisualExecutionFocusPolicy
    {
        /// <summary>
        /// 不从攻击执行态推导视觉焦点。
        /// </summary>
        None,

        /// <summary>
        /// 以当前宿主结果作为视觉焦点。
        /// </summary>
        HostResult,

        /// <summary>
        /// 以当前 cast（施放动作）来源结果作为视觉焦点。
        /// </summary>
        CastResult
    }
}
