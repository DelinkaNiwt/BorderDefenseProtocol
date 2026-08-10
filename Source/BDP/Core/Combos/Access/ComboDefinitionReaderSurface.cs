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
        /// 按两枚芯片 Thing 匹配当前唯一命中的组合技。
        /// 芯片身份优先取制造预设 defName，回退到 ThingDef.defName。
        /// 匹配规则不分 chipA / chipB 书写顺序。
        /// </summary>
        internal ComboDefinitionReadResult FindMatch(Thing chipA, Thing chipB)
        {
            return comboRuntimeIndex.FindMatch(chipA, chipB, Read);
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
