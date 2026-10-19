using BDP.Content.Assembly;
using UnityEngine;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>制造台入口复用装配台的芯片详情外框与信息渲染器。</summary>
    public static class ChipManufacturingPreviewPanel
    {
        /// <summary>绘制有效成品详情或既有无效组合状态。</summary>
        public static void Draw(
            Rect rect,
            ChipManufacturingPreviewModel model,
            ref Vector2 scrollPosition)
        {
            if (model == null)
            {
                return;
            }

            Panel_ChipDetail.DrawManufacturingChipDetail(
                rect,
                model.ProductLabel,
                model.Comparison,
                model.StatusText,
                ref scrollPosition);
        }
    }
}
