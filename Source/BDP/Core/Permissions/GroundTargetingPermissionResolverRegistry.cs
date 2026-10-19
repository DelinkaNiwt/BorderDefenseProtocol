using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 地面目标许可解析器注册表。
    /// 注册表只维护中性解析器，不包含任何设施、芯片或许可键名称。
    /// </summary>
    public static class GroundTargetingPermissionResolverRegistry
    {
        /// <summary>当前有效的地面目标许可解析器实例。</summary>
        private static readonly List<IGroundTargetingPermissionResolver> Resolvers =
            new List<IGroundTargetingPermissionResolver>();

        /// <summary>注册一个解析器；重复实例不会重复加入。</summary>
        /// <param name="resolver">待注册的解析器。</param>
        public static void Register(IGroundTargetingPermissionResolver resolver)
        {
            if (resolver != null && !Resolvers.Contains(resolver))
            {
                Resolvers.Add(resolver);
            }
        }

        /// <summary>移除一个解析器实例。</summary>
        /// <param name="resolver">待移除的解析器。</param>
        public static void Unregister(IGroundTargetingPermissionResolver resolver)
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
        /// 按注册顺序询问解析器；第一个适用的解析器给出最终结果。
        /// 单个业务异常被隔离后继续查询，不阻断攻击主链。
        /// </summary>
        /// <param name="context">当前运行时许可上下文。</param>
        /// <param name="allowed">输出适用解析器给出的许可结果。</param>
        /// <returns>存在适用解析器时返回 true。</returns>
        public static bool TryResolveGroundTargeting(
            MapPermissionContext context,
            out bool allowed)
        {
            allowed = false;
            if (context == null || Resolvers.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < Resolvers.Count; index++)
            {
                IGroundTargetingPermissionResolver resolver = Resolvers[index];
                if (resolver == null)
                {
                    continue;
                }

                try
                {
                    if (resolver.TryResolveGroundTargeting(context, out bool resolved))
                    {
                        allowed = resolved;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    BdpDiagnostics.Once(
                        "chip_permission.ground_targeting_resolver_failed." + resolver.GetType().FullName,
                        "芯片地面目标许可解析器执行失败，已隔离该业务异常。resolver="
                        + resolver.GetType().FullName + "\n" + ex);
                }
            }

            return false;
        }
    }
}
