using BDP.Core.Permissions;
using BDP.Core.Projectiles.Interaction;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 把当前 Pawn 自身的浮空 Hediff 许可接入通用远程间接攻击协议。
    /// </summary>
    public sealed class FloatingFlightIndirectAttackPermissionReceiver :
        IIndirectRangedAttackPermissionResolver
    {
        /// <summary>
        /// 解析当前 Pawn 是否拥有浮空自身许可。
        /// </summary>
        public bool TryResolve(
            IndirectRangedAttackPermissionContext context,
            out IndirectRangedAttackPermissionGrant grant)
        {
            grant = null;
            if (context == null
                || !context.SupportsIndirectTargeting
                || context.PermissionContext == null
                || !context.PermissionContext.HasSelfPermission(FloatingFlightPermissionKeys.SelfActive))
            {
                return false;
            }

            grant = new IndirectRangedAttackPermissionGrant
            {
                // 浮空许可只允许开放空间的非直视目标，屋顶下目标必须回到原版 LOS 规则。
                AllowRoofedTargets = false,
                InteractionPolicy = new ProjectileInteractionPolicy
                {
                    AllowNonTargetPassThrough = true
                }
            };
            return true;
        }
    }
}
