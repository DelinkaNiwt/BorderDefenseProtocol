namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条组合技攻击阶段视觉绑定。
    /// 它把组合结果身份、阶段视觉预设和一次绘制归属收拢到视觉投影，
    /// 不改变组合结果本身的攻击语义，也不持久化运行时状态。
    /// </summary>
    internal sealed class ComboAttackVisualBinding
    {
        /// <summary>
        /// 当前组合技正式结果标识。
        /// </summary>
        public string ComboResultId { get; set; }

        /// <summary>
        /// 当前组合技攻击阶段使用的视觉预设 DefName。
        /// </summary>
        public string VisualPresetDefName { get; set; }

        /// <summary>
        /// 当前组合技视觉在来源视觉条目中的唯一绘制归属结果标识。
        /// </summary>
        public string OwnerSourceResultId { get; set; }
    }
}
