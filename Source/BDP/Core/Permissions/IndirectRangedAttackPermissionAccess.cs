using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 从正式远程表达结果访问结构化间接攻击许可的中性入口。
    /// </summary>
    internal static class IndirectRangedAttackPermissionAccess
    {
        /// <summary>
        /// 按具体目标查询间接攻击授权。
        /// </summary>
        internal static bool TryResolve(
            Pawn pawn,
            FormalExpressionResult result,
            LocalTargetInfo target,
            out IndirectRangedAttackPermissionGrant grant)
        {
            grant = null;
            if (pawn == null
                || result == null
                || !result.SupportsIndirectTargeting
                || result.ResolvedVerbSpec == null)
            {
                return false;
            }

            IndirectRangedAttackPermissionContext context =
                new IndirectRangedAttackPermissionContext(
                    pawn,
                    result.Id,
                    result.DeclaredPermissionKeys,
                    target,
                    result.SupportsIndirectTargeting,
                    result.ResolvedVerbSpec.RequiresDirectTargetLineOfSight);
            return IndirectRangedAttackPermissionResolverRegistry.TryResolve(context, out grant);
        }
    }
}
