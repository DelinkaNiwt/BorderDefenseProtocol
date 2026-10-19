namespace BDP.Core.Permissions
{
    /// <summary>
    /// 地面目标许可的中性运行时解析器。
    /// Core 只定义“是否适用、是否允许”的查询接入点，不规定具体许可来源。
    /// </summary>
    public interface IGroundTargetingPermissionResolver
    {
        /// <summary>
        /// 尝试解析当前表达是否获得地面目标能力。
        /// </summary>
        /// <param name="context">当前 Pawn、地图、派系、结果和显式许可声明。</param>
        /// <param name="allowed">解析器适用时输出是否允许瞄准空地。</param>
        /// <returns>解析器适用于当前表达时返回 true；不适用时返回 false。</returns>
        bool TryResolveGroundTargeting(MapPermissionContext context, out bool allowed);
    }
}
