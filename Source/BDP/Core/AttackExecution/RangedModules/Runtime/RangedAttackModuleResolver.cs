using System;
using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程攻击模块运行时解析器。
    /// 它按正式结果上的挂载快照创建运行时实例，不承担阶段调度。
    /// </summary>
    internal sealed class RangedAttackModuleResolver
    {
        /// <summary>
        /// 按正式结果上的模块挂载快照解析运行时实例列表。
        /// </summary>
        internal IReadOnlyList<RangedAttackModuleSlot> ResolveSlots(Pawn pawn, FormalExpressionResult result)
        {
            List<RangedAttackModuleSlot> slots = new List<RangedAttackModuleSlot>();
            IReadOnlyList<RangedModuleMountConfig> mounts = result != null ? result.RangedModules : null;
            if (mounts == null)
            {
                return slots;
            }

            for (int i = 0; i < mounts.Count; i++)
            {
                RangedModuleMountConfig mount = mounts[i];
                if (mount == null || !mount.enabled || mount.moduleDef == null || mount.moduleDef.runtimeClass == null)
                {
                    continue;
                }

                if (!typeof(IRangedAttackModuleRuntime).IsAssignableFrom(mount.moduleDef.runtimeClass))
                {
                    continue;
                }

                try
                {
                    IRangedAttackModuleRuntime runtime = Activator.CreateInstance(mount.moduleDef.runtimeClass) as IRangedAttackModuleRuntime;
                    if (runtime == null)
                    {
                        continue;
                    }

                    runtime.Initialize(new RangedAttackModuleRuntimeContext
                    {
                        MountIndex = i,
                        Pawn = pawn,
                        Result = result,
                        Mount = mount.Clone(),
                        ModuleDef = mount.moduleDef,
                        Config = ResolveConfigSnapshot(mount)
                    });

                    slots.Add(new RangedAttackModuleSlot
                    {
                        MountIndex = i,
                        Runtime = runtime
                    });
                }
                catch (Exception ex)
                {
                    BdpDiagnostics.Once(
                        "ranged_module.runtime_failed." + mount.moduleDef.defName,
                        "远程模块运行时创建或初始化失败，已跳过。module=" + mount.moduleDef.defName + ", runtimeClass=" + mount.moduleDef.runtimeClass + "\n" + ex);
                    continue;
                }
            }

            return slots;
        }

        /// <summary>
        /// 解析当前模块实例真正应看到的配置快照。
        /// 显式挂载配置优先，未声明时回退到模块 Def 默认配置。
        /// </summary>
        private static RangedModuleConfigNode ResolveConfigSnapshot(RangedModuleMountConfig mount)
        {
            if (mount?.config != null)
            {
                return mount.config.Clone();
            }

            return mount?.moduleDef?.defaultConfig != null
                ? mount.moduleDef.defaultConfig.Clone()
                : null;
        }
    }
}
