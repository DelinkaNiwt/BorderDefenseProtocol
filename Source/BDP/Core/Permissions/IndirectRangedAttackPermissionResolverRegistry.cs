using System;
using System.Collections.Generic;
using BDP.Core.Projectiles.Interaction;
using BDP.Support.Diagnostics;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 远程间接攻击许可解析器注册表。
    /// 任一业务来源授权即可通过；投射物策略按 OR 合并。
    /// </summary>
    public static class IndirectRangedAttackPermissionResolverRegistry
    {
        /// <summary>当前注册的业务解析器。</summary>
        private static readonly List<IIndirectRangedAttackPermissionResolver> Resolvers =
            new List<IIndirectRangedAttackPermissionResolver>();

        /// <summary>注册一个解析器，重复实例不会重复加入。</summary>
        public static void Register(IIndirectRangedAttackPermissionResolver resolver)
        {
            if (resolver != null && !Resolvers.Contains(resolver))
            {
                Resolvers.Add(resolver);
            }
        }

        /// <summary>移除一个解析器实例。</summary>
        public static void Unregister(IIndirectRangedAttackPermissionResolver resolver)
        {
            if (resolver != null)
            {
                Resolvers.Remove(resolver);
            }
        }

        /// <summary>清空注册表，供重载和测试场景使用。</summary>
        public static void Clear()
        {
            Resolvers.Clear();
        }

        /// <summary>
        /// 查询当前目标感知的授权，并在授权后执行屋顶后置条件。
        /// 单个业务异常隔离为未授权，不阻断攻击主链。
        /// </summary>
        public static bool TryResolve(
            IndirectRangedAttackPermissionContext context,
            out IndirectRangedAttackPermissionGrant grant)
        {
            grant = null;
            if (context == null || Resolvers.Count == 0)
            {
                return false;
            }

            ProjectileInteractionPolicy mergedPolicy = null;
            bool allowed = false;
            bool allowRoofedTargets = false;
            for (int index = 0; index < Resolvers.Count; index++)
            {
                IIndirectRangedAttackPermissionResolver resolver = Resolvers[index];
                if (resolver == null)
                {
                    continue;
                }

                try
                {
                    if (!resolver.TryResolve(context, out IndirectRangedAttackPermissionGrant candidate)
                        || candidate == null)
                    {
                        continue;
                    }

                    // 许可来源先声明授权，再由 Core 统一执行目标屋顶后置条件。
                    if (context.TargetIsRoofed && !candidate.AllowRoofedTargets)
                    {
                        continue;
                    }

                    allowed = true;
                    allowRoofedTargets = allowRoofedTargets || candidate.AllowRoofedTargets;
                    mergedPolicy = MergePolicy(mergedPolicy, candidate.InteractionPolicy);
                }
                catch (Exception ex)
                {
                    BdpDiagnostics.Once(
                        "indirect_ranged_permission_resolver_failed." + resolver.GetType().FullName,
                        "远程间接攻击许可解析器执行失败，已隔离该业务异常。resolver="
                        + resolver.GetType().FullName + "\n" + ex);
                }
            }

            if (!allowed)
            {
                return false;
            }

            grant = new IndirectRangedAttackPermissionGrant
            {
                AllowRoofedTargets = allowRoofedTargets,
                InteractionPolicy = mergedPolicy
            };
            return true;
        }

        /// <summary>按 OR 合并多来源投射物交互策略。</summary>
        private static ProjectileInteractionPolicy MergePolicy(
            ProjectileInteractionPolicy current,
            ProjectileInteractionPolicy incoming)
        {
            if (current == null)
            {
                return incoming != null ? incoming.Clone() : null;
            }

            if (incoming == null)
            {
                return current.Clone();
            }

            return new ProjectileInteractionPolicy
            {
                BypassProjectileInterceptors = current.BypassProjectileInterceptors
                    || incoming.BypassProjectileInterceptors,
                BypassRegisteredDamageShields = current.BypassRegisteredDamageShields
                    || incoming.BypassRegisteredDamageShields,
                AllowNonTargetPassThrough = current.AllowNonTargetPassThrough
                    || incoming.AllowNonTargetPassThrough
            };
        }
    }
}
