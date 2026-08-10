using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程攻击模块定义。
    /// 它只描述模块运行时入口与默认配置，不承载阶段业务语义。
    /// </summary>
    public sealed class BdpRangedAttackModuleDef : Def
    {
        /// <summary>
        /// 当前模块运行时实现类型。
        /// </summary>
        public Type runtimeClass;

        /// <summary>
        /// 当前模块的默认配置块。
        /// </summary>
        public RangedModuleConfigNode defaultConfig;

        /// <summary>
        /// 校验远程模块运行时入口，避免错误 Def 到攻击热路径才爆出。
        /// </summary>
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (runtimeClass == null)
            {
                yield return defName + " 缺少 runtimeClass。";
                yield break;
            }

            if (!typeof(IRangedAttackModuleRuntime).IsAssignableFrom(runtimeClass))
            {
                yield return defName + " 的 runtimeClass 必须实现 IRangedAttackModuleRuntime。";
            }

            if (runtimeClass.GetConstructor(Type.EmptyTypes) == null)
            {
                yield return defName + " 的 runtimeClass 必须提供公开无参构造。";
            }
        }
    }
}
