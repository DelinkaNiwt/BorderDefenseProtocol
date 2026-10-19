using BDP.Content.Assembly;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>制造台只准备共享详情面板所需的名称、状态和比较模型。</summary>
    public static class ChipManufacturingPreviewBuilder
    {
        /// <summary>把当前有效组合转换为共用详情预览。</summary>
        public static ChipManufacturingPreviewModel Build(
            ChipCombinationResolution resolution)
        {
            ChipManufacturingPreviewModel model = new ChipManufacturingPreviewModel();
            if (resolution == null
                || resolution.Status != ChipCombinationResolutionStatus.Valid
                || resolution.ResolvedConfig == null)
            {
                model.StatusText = "BDP_ChipManufacturing_PreviewIncomplete".Translate();
                return model;
            }

            model.ProductLabel = resolution.ResolvedLabel;
            model.Comparison = ChipAssemblyDetailModelBuilder.BuildManufacturingComparison(resolution);
            if (model.Comparison == null)
            {
                model.StatusText = "BDP_ChipManufacturing_PreviewIncomplete".Translate();
            }

            return model;
        }
    }
}
