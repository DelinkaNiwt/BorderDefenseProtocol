using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 说明读取投影结果。
    /// 它只保存说明层需要的结构化结果，
    /// 不反查内部运算过程。
    /// </summary>
    internal sealed class ExpressionInfoProjection
    {
        /// <summary>
        /// 当前结果说明文本列表。
        /// </summary>
        public IReadOnlyList<string> Lines { get; set; }

        /// <summary>
        /// 当前结构化结果说明条目列表。
        /// 它用于 UI、调试或可视化读取正式结果，
        /// 而不是反查内部计算过程。
        /// </summary>
        public IReadOnlyList<ExpressionInfoProjectionEntry> Entries { get; set; }

        /// <summary>
        /// 当前默认主远程结果标识。
        /// 没有正式答案时应为空。
        /// </summary>
        public string PrimaryRangedResultId { get; set; }

        /// <summary>
        /// 当前默认主近战结果标识。
        /// 没有正式答案时应为空。
        /// </summary>
        public string PrimaryMeleeResultId { get; set; }

        /// <summary>
        /// 当前执行表达结果标识。
        /// 没有正式答案时应为空。
        /// </summary>
        public string CurrentExecutingResultId { get; set; }

        /// <summary>
        /// 当前是否存在特殊侧武器拦截。
        /// </summary>
        public bool HasSpecialWeaponOverride { get; set; }

        /// <summary>
        /// 当前活跃芯片契约诊断列表。
        /// 它只说明主模组是否接受了这些芯片的契约。
        /// 常规说明读取默认留空，只有显式诊断请求才会填充。
        /// </summary>
        public IReadOnlyList<ExpressionContractDiagnosticEntry> ContractDiagnostics { get; set; }

        /// <summary>
        /// 当前活跃芯片的定义层诊断列表。
        /// 它只说明主模组是否正式接受了这些芯片定义。
        /// 常规说明读取默认留空，只有显式诊断请求才会填充。
        /// </summary>
        public IReadOnlyList<ChipDefinitionDiagnosticEntry> ChipDefinitionDiagnostics { get; set; }
    }
}
