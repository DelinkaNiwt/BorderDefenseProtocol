using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 从正式表达结果构造地面目标许可上下文的中性辅助入口。
    /// </summary>
    internal static class GroundTargetingPermissionAccess
    {
        /// <summary>
        /// 按当前 Pawn 和正式表达结果即时查询地面目标许可。
        /// </summary>
        /// <param name="pawn">当前攻击 Pawn。</param>
        /// <param name="result">当前正式表达结果。</param>
        /// <param name="allowed">输出当前许可链是否允许瞄准空地。</param>
        /// <returns>存在适用许可解析器时返回 true。</returns>
        internal static bool TryResolveGroundTargeting(
            Pawn pawn,
            FormalExpressionResult result,
            out bool allowed)
        {
            allowed = false;
            if (pawn == null || result == null)
            {
                return false;
            }

            MapPermissionContext context = new MapPermissionContext(
                pawn,
                result.Id,
                result.DeclaredPermissionKeys);
            return GroundTargetingPermissionResolverRegistry.TryResolveGroundTargeting(
                context,
                out allowed);
        }
    }
}
