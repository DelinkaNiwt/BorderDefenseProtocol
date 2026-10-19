using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息解析器消费的中性事实集合。
    /// </summary>
    public sealed class ChipInformationFacts
    {
        /// <summary>画像字段事实。</summary>
        public List<ChipInformationFactValue> ProfileFields { get; } =
            new List<ChipInformationFactValue>();

        /// <summary>装载字段事实。</summary>
        public List<ChipInformationFactValue> LoadoutFields { get; } =
            new List<ChipInformationFactValue>();

        /// <summary>Trion 字段事实。</summary>
        public List<ChipInformationFactValue> TrionFields { get; } =
            new List<ChipInformationFactValue>();

        /// <summary>激活条件事实。</summary>
        public List<ChipInformationConditionFact> ActivationConditions { get; } =
            new List<ChipInformationConditionFact>();

        /// <summary>默认表达事实。</summary>
        public List<ChipInformationExpressionFact> Expressions { get; } =
            new List<ChipInformationExpressionFact>();

        /// <summary>根级结构化信息块事实。</summary>
        public List<ChipInformationFactBlock> StructuredBlocks { get; } =
            new List<ChipInformationFactBlock>();

        /// <summary>形态与姿态上下文事实。</summary>
        public List<ChipInformationContextFact> Contexts { get; } =
            new List<ChipInformationContextFact>();

        /// <summary>未被适配器解释的扩展事实，只进入诊断，不进入玩家字段。</summary>
        public List<ChipInformationUnknownFact> Unknowns { get; } =
            new List<ChipInformationUnknownFact>();
    }
}
