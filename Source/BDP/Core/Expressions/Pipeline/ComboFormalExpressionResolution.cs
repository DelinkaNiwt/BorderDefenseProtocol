using BDP.Core.Combos;
using BDP.Core.Requirements;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一次 Combo 正式结果构建所需的已解析输入。
    /// 它只负责把零散参数收成一个对象，不在这里承载构建逻辑。
    /// </summary>
    internal sealed class ComboFormalExpressionResolution
    {
        /// <summary>
        /// 当前命中的 ComboDef 读取结果。
        /// </summary>
        public ComboDefinitionReadResult ComboReadResult { get; set; }

        /// <summary>
        /// 当前条目对应的 Combo 作者配置。
        /// </summary>
        public ComboExpressionEntryConfig EntryConfig { get; set; }

        /// <summary>
        /// 当前条目对应的正式解释结果。
        /// </summary>
        public ChipExpressionEntryContract EntryContract { get; set; }

        /// <summary>
        /// 当前 Combo 主来源材料。
        /// 它代表该侧芯片与内部参数上下文，不等于主来源正式结果。
        /// </summary>
        public ExpressionSourceMaterial MainSourceMaterial { get; set; }

        /// <summary>
        /// 当前 Combo 副来源材料。
        /// 它代表该侧芯片与内部参数上下文，不等于副来源正式结果。
        /// </summary>
        public ExpressionSourceMaterial SubSourceMaterial { get; set; }

        /// <summary>
        /// 当前 Combo 主来源结果。
        /// 它只服务 Verb / Execution 这类正式字段的补值回退。
        /// </summary>
        public FormalExpressionResult MainSourceResult { get; set; }

        /// <summary>
        /// 当前 Combo 副来源结果。
        /// 它只服务 Verb / Execution 这类正式字段的补值回退。
        /// </summary>
        public FormalExpressionResult SubSourceResult { get; set; }

        /// <summary>
        /// 当前条目对应的 Verb 字段求值结果。
        /// </summary>
        public ComboResolvedVerbProps ResolvedVerbProps { get; set; }

        /// <summary>
        /// 当前条目对应的执行节奏求值结果。
        /// </summary>
        public ComboResolvedExecution ResolvedExecution { get; set; }

        /// <summary>
        /// 当前 Pawn 对整个 Combo 顶层使用条件的检查结果。
        /// </summary>
        public PawnRequirementCheckResult UseRequirementCheck { get; set; }
    }
}
