using System;
using UnityEngine;

namespace BDP.Core.UI.Charts
{
    /// <summary>中性图表的受控绘图画布。</summary>
    public sealed class ChartCanvas
    {
        /// <summary>图表在当前 GUI（图形界面）中的外部区域。</summary>
        public readonly Rect Bounds;

        /// <summary>图表与外部可见区域求交后的裁切区域。</summary>
        public readonly Rect ClipRect;

        /// <summary>图表是否存在可绘制区域。</summary>
        public bool IsVisible
        {
            get { return ClipRect.width > 0f && ClipRect.height > 0f; }
        }

        /// <summary>创建一块受控图表画布。</summary>
        public ChartCanvas(Rect bounds, Rect visibleRect)
        {
            Bounds = bounds;
            ClipRect = Intersect(bounds, visibleRect.width > 0f && visibleRect.height > 0f ? visibleRect : bounds);
        }

        /// <summary>在当前画布中执行绘图回调。</summary>
        public void Draw(Action<ChartDrawingContext> drawAction)
        {
            if (!IsVisible || drawAction == null)
            {
                return;
            }

            drawAction(new ChartDrawingContext(this));
        }

        /// <summary>计算两个矩形的交集。</summary>
        private static Rect Intersect(Rect first, Rect second)
        {
            float xMin = Mathf.Max(first.xMin, second.xMin);
            float yMin = Mathf.Max(first.yMin, second.yMin);
            float xMax = Mathf.Min(first.xMax, second.xMax);
            float yMax = Mathf.Min(first.yMax, second.yMax);
            return new Rect(xMin, yMin, Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        }
    }
}
