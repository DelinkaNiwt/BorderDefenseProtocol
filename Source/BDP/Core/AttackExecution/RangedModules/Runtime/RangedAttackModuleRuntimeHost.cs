using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程攻击模块运行时宿主。
    /// 它负责为单次攻击创建模块会话，不直接参与阶段裁决。
    /// </summary>
    internal sealed class RangedAttackModuleRuntimeHost
    {
        /// <summary>
        /// 当前宿主持有的模块运行时解析器。
        /// </summary>
        private readonly RangedAttackModuleResolver resolver;

        /// <summary>
        /// 使用指定解析器构造模块运行时宿主。
        /// </summary>
        public RangedAttackModuleRuntimeHost(RangedAttackModuleResolver resolver)
        {
            this.resolver = resolver ?? new RangedAttackModuleResolver();
        }

        /// <summary>
        /// 为一次攻击创建模块运行时会话。
        /// </summary>
        internal RangedAttackModuleSession CreateSession(Pawn pawn, FormalExpressionResult result)
        {
            IReadOnlyList<RangedModuleMountConfig> mounts = result != null ? CloneMounts(result.RangedModules) : new List<RangedModuleMountConfig>();
            IReadOnlyList<RangedAttackModuleSlot> slots = resolver.ResolveSlots(pawn, result);
            return new RangedAttackModuleSession
            {
                AttackContext = new AttackContext(),
                Pawn = pawn,
                Result = result,
                Mounts = mounts,
                Slots = slots
            };
        }

        /// <summary>
        /// 复制一份挂载顺序快照，避免运行时回写正式结果对象。
        /// </summary>
        private static IReadOnlyList<RangedModuleMountConfig> CloneMounts(IReadOnlyList<RangedModuleMountConfig> mounts)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (mounts == null)
            {
                return result;
            }

            for (int i = 0; i < mounts.Count; i++)
            {
                RangedModuleMountConfig mount = mounts[i];
                if (mount == null)
                {
                    continue;
                }

                result.Add(mount.Clone());
            }

            return result;
        }
    }
}
