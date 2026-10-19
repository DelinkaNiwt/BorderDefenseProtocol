using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 直达目标视线许可解析器注册表。
    /// 注册表只维护中性解析器，不包含任何具体设施、芯片或许可键名称。
    /// </summary>
    public static class DirectTargetLineOfSightPermissionResolverRegistry
    {
        /// <summary>当前有效的直达目标视线解析器实例。</summary>
        private static readonly List<IDirectTargetLineOfSightPermissionResolver> Resolvers =
            new List<IDirectTargetLineOfSightPermissionResolver>();

        /// <summary>注册一个解析器；重复实例不会重复加入。</summary>
        /// <param name="resolver">待注册的解析器。</param>
        public static void Register(IDirectTargetLineOfSightPermissionResolver resolver)
        {
            if (resolver != null && !Resolvers.Contains(resolver))
            {
                Resolvers.Add(resolver);
            }
        }

        /// <summary>移除一个解析器实例。</summary>
        /// <param name="resolver">待移除的解析器。</param>
        public static void Unregister(IDirectTargetLineOfSightPermissionResolver resolver)
        {
            if (resolver != null)
            {
                Resolvers.Remove(resolver);
            }
        }

        /// <summary>清空解析器注册表，供重载和测试场景使用。</summary>
        public static void Clear()
        {
            Resolvers.Clear();
        }

        /// <summary>
        /// 按注册顺序询问解析器；任意解析器允许即可放宽。
        /// 单个业务异常被隔离后按未授权处理，不阻断攻击主链。
        /// </summary>
        /// <param name="context">当前运行时许可上下文。</param>
        /// <returns>至少一个解析器允许放宽时返回 true。</returns>
        public static bool ShouldRelaxDirectTargetLineOfSight(MapPermissionContext context)
        {
            if (context == null || Resolvers.Count == 0)
            {
                BdpDiagnostics.AttackExecutionThrottled(
                    "direct_los.registry.empty",
                    "直视许可解析器链为空 | context=" + (context != null)
                    + " | resolverCount=" + Resolvers.Count,
                    30);
                return false;
            }

            BdpDiagnostics.AttackExecutionThrottled(
                "direct_los.registry.query." + (context.ResultId ?? "null"),
                "直视许可解析器查询开始 | result=" + (context.ResultId ?? "null")
                + " | resolverCount=" + Resolvers.Count,
                30);

            for (int index = 0; index < Resolvers.Count; index++)
            {
                IDirectTargetLineOfSightPermissionResolver resolver = Resolvers[index];
                if (resolver == null)
                {
                    continue;
                }

                try
                {
                    bool allowed = resolver.ShouldRelaxDirectTargetLineOfSight(context);
                    BdpDiagnostics.AttackExecutionThrottled(
                        "direct_los.registry.result." + (context.ResultId ?? "null") + "." + resolver.GetType().FullName,
                        "直视许可解析器结果 | result=" + (context.ResultId ?? "null")
                        + " | resolver=" + resolver.GetType().FullName
                        + " | allowed=" + allowed,
                        30);
                    if (allowed)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    BdpDiagnostics.Once(
                        "chip_permission.direct_los_resolver_failed." + resolver.GetType().FullName,
                        "芯片直视许可解析器执行失败，已隔离该业务异常。resolver="
                        + resolver.GetType().FullName + "\n" + ex);
                }
            }

            return false;
        }
    }
}
