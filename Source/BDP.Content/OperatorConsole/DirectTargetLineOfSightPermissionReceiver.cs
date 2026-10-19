using System.Collections.Generic;
using BDP.Core.Permissions;
using BDP.Core.Projectiles.Interaction;
using BDP.Support.Diagnostics;

namespace BDP.Content.OperatorConsole
{
    /// <summary>
    /// 远程表达接收操作员控制台许可的业务接收器。
    /// 它只对显式声明许可键的结果给出业务查询，不修改 Core 的全局规则。
    /// </summary>
    public sealed class DirectTargetLineOfSightPermissionReceiver :
        IChipFunctionPermissionReceiver,
        IDirectTargetLineOfSightPermissionResolver,
        IGroundTargetingPermissionResolver,
        IIndirectRangedAttackPermissionResolver
    {
        /// <summary>接收器关注的许可键。</summary>
        private const string PermissionKey = OperatorConsolePermissionKeys.OperatorConsoleActive;

        /// <summary>接收运行时许可事实；不在这里修改正式 Verb 规格。</summary>
        public void Receive(MapPermissionContext context)
        {
            // 该接收器不缓存 Pawn 或地图引用，具体攻击阶段通过下方业务查询即时读取许可。
        }

        /// <summary>
        /// 判断一个结果是否显式接收控制台许可并且当前许可有效。
        /// 具体远程攻击业务可在自己的目标准入/执行阶段调用此方法。
        /// </summary>
        /// <param name="context">当前芯片许可上下文。</param>
        /// <returns>应由该业务考虑放宽直达视线时为 true。</returns>
        public bool ShouldRelaxDirectTargetLineOfSight(MapPermissionContext context)
        {
            bool optedIn = IsOptedIn(context);
            bool hasPermission = optedIn && context != null && context.HasPermission(PermissionKey);
            BdpDiagnostics.AttackExecutionThrottled(
                "direct_los.receiver." + (context != null ? context.ResultId : "null"),
                "直视许可接收器结果 | result=" + (context != null ? context.ResultId : "null")
                + " | optedIn=" + optedIn
                + " | key=" + PermissionKey
                + " | hasPermission=" + hasPermission,
                30);
            return hasPermission;
        }

        /// <summary>
        /// 把既有控制台直视许可适配到通用远程间接攻击协议。
        /// 控制台保持原有屋顶目标语义，不附加投射物绕过策略。
        /// </summary>
        public bool TryResolve(
            IndirectRangedAttackPermissionContext context,
            out IndirectRangedAttackPermissionGrant grant)
        {
            grant = null;
            MapPermissionContext permissionContext = context != null
                ? context.PermissionContext
                : null;
            if (!ShouldRelaxDirectTargetLineOfSight(permissionContext))
            {
                return false;
            }

            grant = new IndirectRangedAttackPermissionGrant
            {
                AllowRoofedTargets = true,
                InteractionPolicy = null
            };
            return true;
        }

        /// <summary>
        /// 解析当前表达是否获得地面目标能力。
        /// 未声明控制台许可键的表达不适用此业务，交回其它目标规则处理。
        /// </summary>
        /// <param name="context">当前芯片许可上下文。</param>
        /// <param name="allowed">输出控制台许可是否允许瞄准空地。</param>
        /// <returns>当前表达声明了控制台许可键时返回 true。</returns>
        public bool TryResolveGroundTargeting(MapPermissionContext context, out bool allowed)
        {
            allowed = false;
            if (!IsOptedIn(context))
            {
                return false;
            }

            allowed = context != null && context.HasPermission(PermissionKey);
            BdpDiagnostics.AttackExecutionThrottled(
                "ground_targeting.receiver." + (context != null ? context.ResultId : "null"),
                "地面目标许可接收器结果 | result=" + (context != null ? context.ResultId : "null")
                + " | key=" + PermissionKey
                + " | allowed=" + allowed,
                30);
            return true;
        }

        /// <summary>判断结果声明列表中是否包含该业务许可键。</summary>
        private static bool IsOptedIn(MapPermissionContext context)
        {
            IReadOnlyList<string> keys = context != null ? context.DeclaredPermissionKeys : null;
            if (keys == null)
            {
                return false;
            }

            for (int index = 0; index < keys.Count; index++)
            {
                if (keys[index] == PermissionKey)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
