namespace BDP.Core.Expressions
{
    /// <summary>
    /// 枪口锚点解析时跟随哪类动态来源的静态政策。
    /// 它只决定读取哪组运行时标识，不直接保存当轮发射标识。
    /// </summary>
    internal enum VisualMuzzleFollowPolicy
    {
        /// <summary>
        /// 不使用视觉系统解算枪口锚点。
        /// </summary>
        None,

        /// <summary>
        /// 枪口锚点跟随当前宿主结果。
        /// </summary>
        HostResult,

        /// <summary>
        /// 枪口锚点跟随当前 cast（施放动作）来源结果。
        /// </summary>
        CastResult,

        /// <summary>
        /// 枪口锚点跟随当前 emit（实际效果实例）的源结果。
        /// </summary>
        EmitSourceResult
    }
}
