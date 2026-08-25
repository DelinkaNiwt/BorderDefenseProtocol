using System.Collections.Generic;
using BDP.Core.AttackExecution;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 被动表达向其它最终远程结果发布的一条开放式增强声明。
    /// 它只按目标结果能力匹配，不引用任何具体芯片或武装型身份。
    /// </summary>
    public sealed class RangedModuleAugmentationConfig
    {
        /// <summary>
        /// 当前增强声明适用的中性结果能力。
        /// </summary>
        public RangedModuleAugmentationTargetCapability AppliesToCapability =
            RangedModuleAugmentationTargetCapability.RangedWeapon;

        /// <summary>
        /// 当前增强声明追加的远程模块列表。
        /// </summary>
        public List<RangedModuleMountConfig> Modules = new List<RangedModuleMountConfig>();

        /// <summary>
        /// 当前增强是否为最终入口名称增加前缀。
        /// </summary>
        public RangedModuleAugmentationDisplayPrefixMode DisplayNamePrefixMode =
            RangedModuleAugmentationDisplayPrefixMode.None;

        /// <summary>
        /// 显式名称前缀。
        /// 只有 DisplayNamePrefixMode 为 Explicit 时消费。
        /// </summary>
        public string DisplayNamePrefix;

        /// <summary>
        /// 复制当前增强声明，避免表达快照共享可变配置列表。
        /// </summary>
        public RangedModuleAugmentationConfig Clone()
        {
            RangedModuleAugmentationConfig result = new RangedModuleAugmentationConfig
            {
                AppliesToCapability = AppliesToCapability,
                DisplayNamePrefixMode = DisplayNamePrefixMode,
                DisplayNamePrefix = DisplayNamePrefix,
                Modules = new List<RangedModuleMountConfig>()
            };

            if (Modules == null)
            {
                return result;
            }

            for (int index = 0; index < Modules.Count; index++)
            {
                RangedModuleMountConfig module = Modules[index];
                if (module != null)
                {
                    result.Modules.Add(module.Clone());
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 被动增强目标的中性能力枚举。
    /// </summary>
    public enum RangedModuleAugmentationTargetCapability
    {
        /// <summary>
        /// 可执行的远程武装表达结果。
        /// </summary>
        RangedWeapon
    }

    /// <summary>
    /// 被动增强对最终入口名称的修饰方式。
    /// </summary>
    public enum RangedModuleAugmentationDisplayPrefixMode
    {
        /// <summary>
        /// 不修改名称。
        /// </summary>
        None,

        /// <summary>
        /// 使用增强来源表达条目的本地化显示名称作为前缀。
        /// </summary>
        SourceExpressionLabel,

        /// <summary>
        /// 使用配置中显式填写的名称前缀。
        /// </summary>
        Explicit
    }
}
