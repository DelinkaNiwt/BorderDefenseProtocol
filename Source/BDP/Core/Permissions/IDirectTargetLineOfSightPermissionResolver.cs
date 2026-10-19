namespace BDP.Core.Permissions
{
    /// <summary>
    /// 直达目标视线许可的中性运行时解析器。
    /// Core 只定义判定接入点，不规定任何许可键或具体业务效果来源。
    /// </summary>
    public interface IDirectTargetLineOfSightPermissionResolver
    {
        /// <summary>
        /// 判断当前表达在当前运行时上下文中是否可以放宽直达目标视线。
        /// </summary>
        /// <param name="context">当前 Pawn、地图、派系、结果和显式许可声明。</param>
        /// <returns>允许放宽直达目标视线时返回 true。</returns>
        bool ShouldRelaxDirectTargetLineOfSight(MapPermissionContext context);
    }
}
