namespace BDP.Core.Expressions
{
    /// <summary>
    /// 宿主原装备贴图的绘制策略。
    /// 它只描述视觉边界是否允许原版装备继续画，不承担当轮攻击动态状态。
    /// </summary>
    internal enum HostEquipmentRenderMode
    {
        /// <summary>
        /// 保留原版装备贴图，并允许 BDP 视觉层只追加附加层。
        /// </summary>
        Keep,

        /// <summary>
        /// 用 BDP 发布的视觉条目替换原版装备贴图。
        /// </summary>
        Replace,

        /// <summary>
        /// 只沿用原版手持物姿态，把原版装备贴图替换为单枚武器芯片贴图。
        /// </summary>
        ReplaceTextureOnly,

        /// <summary>
        /// 强制压制原版装备贴图。
        /// 若没有可解析的视觉条目，本次绘制会表现为不显示手持物。
        /// </summary>
        Suppress
    }
}
