using BDP.Core.Expressions.Runtime;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义读取表面。
    /// 它把解释、依赖检查和最小合法性校验收成一次稳定读取。
    /// </summary>
    internal sealed class ComboDefinitionService : IComboDefinitionReader
    {
        /// <summary>
        /// 当前正式服务依赖的契约解释器。
        /// </summary>
        private static readonly ComboDefinitionContractResolver ContractResolver =
            new ComboDefinitionContractResolver();

        /// <summary>
        /// 当前正式服务依赖的最低合法性校验器。
        /// </summary>
        private static readonly ComboDefinitionValidator Validator =
            new ComboDefinitionValidator();

        /// <summary>
        /// 当前正式服务依赖的组合技运行时索引。
        /// </summary>
        private readonly ComboRuntimeIndex comboRuntimeIndex;

        /// <summary>
        /// 使用默认索引构造正式服务。
        /// </summary>
        public ComboDefinitionService()
            : this(new ComboRuntimeIndex())
        {
        }

        /// <summary>
        /// 使用指定索引构造正式服务。
        /// </summary>
        public ComboDefinitionService(ComboRuntimeIndex comboRuntimeIndex)
        {
            this.comboRuntimeIndex = comboRuntimeIndex ?? new ComboRuntimeIndex();
        }

        /// <summary>
        /// 读取指定 ComboDef 的正式组合技定义结果。
        /// </summary>
        public ComboDefinitionReadResult Read(ComboDef comboDef)
        {
            ComboDefinitionContract contract = ContractResolver.Resolve(comboDef);
            ComboDefinitionValidationResult validation = Validator.Validate(contract);
            return new ComboDefinitionReadResult
            {
                ComboDef = comboDef,
                Contract = contract,
                Validation = validation
            };
        }

        /// <summary>
        /// 按 DefName 读取指定组合技的正式定义结果。
        /// </summary>
        public ComboDefinitionReadResult Read(string defName)
        {
            return Read(!string.IsNullOrWhiteSpace(defName)
                ? DefDatabase<ComboDef>.GetNamedSilentFail(defName)
                : null);
        }

        /// <summary>
        /// 按两枚芯片 Thing 匹配第一份动作身份与成品准入都成立的组合技。
        /// 动作身份只取制造来源首个预设 DefName，第一来源/第二来源分配允许正反向尝试。
        /// </summary>
        internal ComboDefinitionReadResult FindMatch(Thing firstSourceChip, Thing secondSourceChip)
        {
            return comboRuntimeIndex.FindMatch(firstSourceChip, secondSourceChip, Read);
        }

        /// <summary>按两枚芯片匹配组合技，并返回集中诊断失败摘要。</summary>
        internal ComboDefinitionReadResult FindMatch(
            Thing firstSourceChip,
            Thing secondSourceChip,
            out string failureReason)
        {
            return comboRuntimeIndex.FindMatch(firstSourceChip, secondSourceChip, Read, out failureReason);
        }

        /// <summary>
        /// 读取当前服务使用的契约解释器。
        /// 表达系统需要通过这里复用字段级求值协议。
        /// </summary>
        internal ComboDefinitionContractResolver ResolveContractResolver()
        {
            return ContractResolver;
        }

    }
}
