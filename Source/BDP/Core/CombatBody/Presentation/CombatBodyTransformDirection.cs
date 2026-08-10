namespace BDP.Core.CombatBody.Presentation
{
    /// <summary>
    /// 战斗体宿主变换的表现方向。
    /// 只表达进入或离开，不携带具体视觉业务。
    /// </summary>
    public enum CombatBodyTransformDirection
    {
        /// <summary>
        /// 从原身进入战斗体。
        /// </summary>
        Enter,

        /// <summary>
        /// 从战斗体恢复原身。
        /// </summary>
        Exit
    }
}
