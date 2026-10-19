using System.Collections.Generic;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达正规化契约只读面。
    /// Content（内容程序集）和其它消费者通过这里读取结果，不直接依赖内部解释器或缓存。
    /// </summary>
    public static class ChipExpressionContractSurfaceAccess
    {
        /// <summary>
        /// 读取指定芯片当前可解析的正规化表达契约。
        /// </summary>
        public static ChipExpressionResolvedContract Resolve(
            Thing chip,
            ITriggerLoadoutReader triggerLoadoutReader)
        {
            return new ChipExpressionContractInterpreter().Resolve(chip, triggerLoadoutReader);
        }

        /// <summary>
        /// 使用指定配置读取正规化表达契约。
        /// 适用于制造期合并后的中性配置，不要求创建临时芯片对象。
        /// </summary>
        public static ChipExpressionResolvedContract ResolveWithConfig(
            Thing chip,
            ChipExpressionConfig config,
            ITriggerLoadoutReader triggerLoadoutReader)
        {
            return new ChipExpressionContractInterpreter().Resolve(
                chip,
                config,
                triggerLoadoutReader);
        }

        /// <summary>
        /// 构造缺少表达块时的空正规化结果，保证消费者不需要判空内部集合。
        /// </summary>
        public static ChipExpressionResolvedContract Empty()
        {
            return new ChipExpressionResolvedContract
            {
                Contract = new ChipExpressionContract
                {
                    Entries = new List<ChipExpressionEntryContract>(),
                    DefaultModeKey = null,
                    Modes = new List<ChipExpressionModeContract>()
                },
                Validation = new ChipExpressionContractValidationResult
                {
                    IsValid = true,
                    Errors = new List<string>(),
                    Warnings = new List<string>()
                }
            };
        }
    }
}
