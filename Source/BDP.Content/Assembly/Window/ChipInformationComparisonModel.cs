using BDP.Core.Information;
using BDP.Core.Chips;

namespace BDP.Content.Assembly
{
    /// <summary>芯片信息图使用的比较基准预设。</summary>
    internal enum ChipInformationComparisonBaselineMode
    {
        /// <summary>以该功能的声明定义值作为比较基准。</summary>
        DeclaredDefinition,

        /// <summary>以应用 Trion 立方体构型修正后的功能结果作为比较基准。</summary>
        TrionCubeAdjustedFunction,

        /// <summary>以组合技宿主功能当前展示的结果值作为比较基准。</summary>
        HostFunctionResult
    }

    /// <summary>详情界面使用的当前信息树与比较基准配对。</summary>
    internal sealed class ChipInformationComparisonModel
    {
        /// <summary>当前成品或实例的信息模型。</summary>
        internal ChipInformationModel Current { get; }

        /// <summary>比较基准的信息模型。</summary>
        internal ChipInformationModel Baseline { get; }

        /// <summary>基准构型的玩家可见名称；没有自定义基准时为空。</summary>
        internal string BaselineLabel { get; }

        /// <summary>本次图表所采用的明确基准预设。</summary>
        internal ChipInformationComparisonBaselineMode BaselineMode { get; }

        /// <summary>当前成品的有效配置；组合理论结果必须保留实际成品身份与复合来源。</summary>
        internal ChipDefinitionConfig SourceConfig { get; }

        /// <summary>当前成品的有序来源、最终职业和构型；与正式匹配入口使用同一事实。</summary>
        internal ChipSourceReferenceSnapshot SourceReference { get; }

        /// <summary>创建信息树比较对。</summary>
        private ChipInformationComparisonModel(
            ChipInformationModel current,
            ChipInformationModel baseline,
            string baselineLabel,
            ChipInformationComparisonBaselineMode baselineMode,
            ChipDefinitionConfig sourceConfig,
            ChipSourceReferenceSnapshot sourceReference)
        {
            Current = current;
            Baseline = baseline;
            BaselineLabel = baselineLabel;
            BaselineMode = baselineMode;
            SourceConfig = sourceConfig;
            SourceReference = sourceReference;
        }

        /// <summary>创建当前与比较基准配对。</summary>
        internal static ChipInformationComparisonModel Create(
            ChipInformationModel current,
            ChipInformationModel baseline,
            string baselineLabel,
            ChipInformationComparisonBaselineMode baselineMode,
            ChipDefinitionConfig sourceConfig,
            ChipSourceReferenceSnapshot sourceReference)
        {
            return new ChipInformationComparisonModel(
                current,
                baseline,
                baselineLabel,
                baselineMode,
                sourceConfig,
                sourceReference);
        }
    }
}
