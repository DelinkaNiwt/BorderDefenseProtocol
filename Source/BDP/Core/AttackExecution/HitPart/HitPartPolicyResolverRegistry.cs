using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 命中部位策略解析器注册表。
    /// 它是 Core 与 Content 之间的中性桥，不包含任何具体策略。
    /// </summary>
    public static class HitPartPolicyResolverRegistry
    {
        private static readonly List<IHitPartPolicyResolver> Resolvers = new List<IHitPartPolicyResolver>();

        /// <summary>
        /// 注册一个命中部位策略解析器。
        /// </summary>
        public static void Register(IHitPartPolicyResolver resolver)
        {
            if (resolver != null && !Resolvers.Contains(resolver))
            {
                Resolvers.Add(resolver);
            }
        }

        /// <summary>
        /// 注销一个命中部位策略解析器。
        /// </summary>
        public static void Unregister(IHitPartPolicyResolver resolver)
        {
            if (resolver != null)
            {
                Resolvers.Remove(resolver);
            }
        }

        /// <summary>
        /// 清空注册表，供模组重载或测试收口使用。
        /// </summary>
        public static void Clear()
        {
            Resolvers.Clear();
        }

        /// <summary>
        /// 按注册顺序尝试解析策略。
        /// 解析器异常不会破坏原版攻击流程。
        /// </summary>
        public static bool TryResolve(
            Pawn attacker,
            Thing target,
            DamageDef damageDef,
            BodyPartGroupDef bodyPartGroup,
            HitPartPolicyConfig policy,
            out BodyPartRecord hitPart)
        {
            hitPart = null;
            if (policy == null || target == null || Resolvers.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < Resolvers.Count; i++)
            {
                IHitPartPolicyResolver resolver = Resolvers[i];
                if (resolver == null)
                {
                    continue;
                }

                try
                {
                    if (resolver.TryResolve(attacker, target, damageDef, bodyPartGroup, policy, out hitPart)
                        && hitPart != null)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    hitPart = null;
                }
            }

            return false;
        }
    }
}
