using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达契约解释器。
    /// 它负责把作者原始配置翻译成主模组正式承认的契约。
    /// </summary>
    internal interface IChipExpressionContractInterpreter
    {
        /// <summary>
        /// 解析指定芯片在当前 Trigger 事实下应展开成的正式契约。
        /// </summary>
        ChipExpressionResolvedContract Resolve(Thing chip, ITriggerLoadoutReader triggerLoadoutReader);

        /// <summary>
        /// 使用指定表达块配置解析芯片在当前 Trigger 事实下应展开成的正式契约。
        /// 这用于让表达系统通过芯片定义层转交表达块，而不是自己直接读取 Def 原始写法。
        /// </summary>
        ChipExpressionResolvedContract Resolve(
            Thing chip,
            ChipExpressionConfig config,
            ITriggerLoadoutReader triggerLoadoutReader);
    }
}
