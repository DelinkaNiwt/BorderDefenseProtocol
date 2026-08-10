namespace BDP.Core.Expressions
{
    /// <summary>
    /// 视觉投影层当前的关系类型。
    /// 它只回答视觉层应如何理解当前结果关系，不回写结果层身份。
    /// </summary>
    internal enum VisualExpressionRelationKind
    {
        /// <summary>
        /// 当前未给出明确视觉关系。
        /// </summary>
        None,

        /// <summary>
        /// 当前按普通单侧视觉理解。
        /// </summary>
        SingleSide,

        /// <summary>
        /// 当前按双武器视觉理解。
        /// </summary>
        DualWeapon,

        /// <summary>
        /// 当前按组合技视觉理解。
        /// </summary>
        Combo,

        /// <summary>
        /// 当前按双手锁定视觉双侧伪装理解。
        /// </summary>
        DualSideMask
    }
}
