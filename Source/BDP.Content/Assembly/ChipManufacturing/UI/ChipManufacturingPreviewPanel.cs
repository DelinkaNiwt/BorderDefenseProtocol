using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>按芯片规格、武装型修正、动作形态的顺序绘制中栏。</summary>
    public static class ChipManufacturingPreviewPanel
    {
        /// <summary>普通字段行高。</summary>
        private const float RowHeight = 24f;

        /// <summary>段标题行高。</summary>
        private const float HeaderHeight = 26f;

        /// <summary>主标题与第一组属性之间的留白。</summary>
        private const float ProductTitleGap = 8f;

        /// <summary>在固定栏宽中绘制可纵向滚动的完整预览。</summary>
        public static void Draw(
            Rect rect,
            ChipManufacturingPreviewModel model,
            ref Vector2 scrollPosition)
        {
            Widgets.DrawMenuSection(rect);
            Rect outRect = rect.ContractedBy(8f);
            float contentHeight = CalculateHeight(model, outRect.width - 18f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f,
                Mathf.Max(outRect.height, contentHeight));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0f;
            if (!model.StatusText.NullOrEmpty())
            {
                Widgets.Label(new Rect(4f, y, viewRect.width - 8f, 60f), model.StatusText);
                Widgets.EndScrollView();
                return;
            }

            DrawProductTitle(viewRect.width, ref y, model.ProductLabel);
            DrawHeader(
                viewRect.width,
                ref y,
                "BDP_ChipManufacturing_Preview_Specifications".Translate());
            for (int index = 0; index < model.Specifications.Count; index++)
            {
                DrawMetric(viewRect.width, ref y, model.Specifications[index]);
            }

            if (model.ArmamentFormMetrics.Count > 0
                || model.ArmamentFormAdjustments.Count > 0)
            {
                y += 10f;
                DrawHeader(
                    viewRect.width,
                    ref y,
                    "BDP_ChipManufacturing_Preview_ArmamentFormAdjustments".Translate());
                for (int index = 0; index < model.ArmamentFormMetrics.Count; index++)
                {
                    DrawMetric(viewRect.width, ref y, model.ArmamentFormMetrics[index]);
                }

                if (model.ArmamentFormAdjustments.Count > 0)
                {
                    y += 4f;
                    DrawAdjustmentGrid(
                        viewRect.width,
                        ref y,
                        model.ArmamentFormAdjustments);
                }
            }

            foreach (ChipActionFormPreview form in model.ActionForms)
            {
                y += 12f;
                DrawHeader(viewRect.width, ref y,
                    "BDP_ChipManufacturing_Preview_ActionForm".Translate(form.Label));
                if (form.Metrics.Count == 0)
                {
                    Widgets.Label(new Rect(6f, y, viewRect.width - 12f, RowHeight),
                        "BDP_ChipManufacturing_Preview_NoNumericMetrics".Translate());
                    y += RowHeight;
                    continue;
                }

                for (int index = 0; index < form.Metrics.Count; index++)
                {
                    DrawMetric(viewRect.width, ref y, form.Metrics[index]);
                }
            }

            Widgets.EndScrollView();
        }

        /// <summary>绘制中栏唯一的中号完整成品名称。</summary>
        private static void DrawProductTitle(float width, ref float y, string label)
        {
            if (label.NullOrEmpty())
            {
                return;
            }

            float height = ProductTitleHeight(width, label);
            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(4f, y, width - 8f, height), label);
            }
            finally
            {
                Text.Font = oldFont;
            }

            y += height + ProductTitleGap;
        }

        /// <summary>绘制段标题。</summary>
        private static void DrawHeader(float width, ref float y, string label)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                GUI.color = new Color(0.78f, 0.82f, 0.88f);
                Widgets.Label(new Rect(4f, y, width - 8f, HeaderHeight - 3f), label);
                Widgets.DrawLineHorizontal(4f, y + HeaderHeight - 3f, width - 8f);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
            }

            y += HeaderHeight;
        }

        /// <summary>绘制一行字段；条形图居中，精确数值固定在其右侧。</summary>
        private static void DrawMetric(
            float width,
            ref float y,
            ChipMetricPreview metric)
        {
            float labelWidth = Mathf.Min(138f, width * 0.34f);
            float valueWidth = 76f;
            float rowHeight = MetricHeight(width, metric);
            Rect labelRect = new Rect(6f, y, labelWidth - 8f, rowHeight);
            Rect barRect = new Rect(labelWidth, y + 5f,
                Mathf.Max(0f, width - labelWidth - valueWidth - 12f), 14f);
            Rect valueRect = metric.ShowBar
                ? new Rect(barRect.xMax + 6f, y, valueWidth, rowHeight)
                : new Rect(labelWidth, y, width - labelWidth - 6f, rowHeight);

            Widgets.Label(labelRect, metric.LabelKey.Translate());
            if (metric.ShowBar)
            {
                Widgets.FillableBar(barRect, metric.NormalizedValue);
            }

            Color oldColor = GUI.color;
            if (metric.IsModified)
            {
                GUI.color = new Color(0.45f, 0.85f, 1f);
            }
            Widgets.Label(valueRect, metric.ValueText + (metric.IsModified ? " *" : ""));
            GUI.color = oldColor;
            y += rowHeight;
        }

        /// <summary>把武装型修正按两列紧凑绘制。</summary>
        private static void DrawAdjustmentGrid(
            float width,
            ref float y,
            IList<ChipAdjustmentPreview> adjustments)
        {
            const float columnGap = 12f;
            float columnWidth = (width - columnGap) / 2f;
            for (int index = 0; index < adjustments.Count; index += 2)
            {
                DrawAdjustmentCell(
                    new Rect(4f, y, columnWidth - 4f, RowHeight),
                    adjustments[index]);
                if (index + 1 < adjustments.Count)
                {
                    DrawAdjustmentCell(
                        new Rect(columnWidth + columnGap, y, columnWidth - 4f, RowHeight),
                        adjustments[index + 1]);
                }

                y += RowHeight;
            }
        }

        /// <summary>在一列内绘制一项紧凑武装型修正。</summary>
        private static void DrawAdjustmentCell(
            Rect rect,
            ChipAdjustmentPreview adjustment)
        {
            float labelWidth = rect.width * 0.58f;
            Widgets.Label(
                new Rect(rect.x, rect.y, labelWidth, rect.height),
                adjustment.LabelKey.Translate());
            Widgets.Label(
                new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height),
                adjustment.OperationText);
        }

        /// <summary>按实际组数估算滚动内容高度，不为不存在的第二形态留空。</summary>
        private static float CalculateHeight(
            ChipManufacturingPreviewModel model,
            float width)
        {
            if (!model.StatusText.NullOrEmpty())
            {
                return 60f;
            }

            float height = ProductTitleHeight(width, model.ProductLabel)
                + ProductTitleGap
                + HeaderHeight;
            for (int index = 0; index < model.Specifications.Count; index++)
            {
                height += MetricHeight(width, model.Specifications[index]);
            }
            if (model.ArmamentFormMetrics.Count > 0
                || model.ArmamentFormAdjustments.Count > 0)
            {
                height += 10f + HeaderHeight;
                for (int index = 0; index < model.ArmamentFormMetrics.Count; index++)
                {
                    height += MetricHeight(width, model.ArmamentFormMetrics[index]);
                }

                if (model.ArmamentFormAdjustments.Count > 0)
                {
                    height += 4f
                        + Mathf.CeilToInt(model.ArmamentFormAdjustments.Count / 2f) * RowHeight;
                }
            }

            foreach (ChipActionFormPreview form in model.ActionForms)
            {
                height += 12f + HeaderHeight;
                if (form.Metrics.Count == 0)
                {
                    height += RowHeight;
                }
                else
                {
                    for (int index = 0; index < form.Metrics.Count; index++)
                    {
                        height += MetricHeight(width, form.Metrics[index]);
                    }
                }
            }

            return height + 8f;
        }

        /// <summary>按当前栏宽测量完整成品名称。</summary>
        private static float ProductTitleHeight(float width, string label)
        {
            if (label.NullOrEmpty())
            {
                return 0f;
            }

            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = GameFont.Medium;
                return Mathf.Max(32f, Text.CalcHeight(label, width - 8f));
            }
            finally
            {
                Text.Font = oldFont;
            }
        }

        /// <summary>按标签与文本实际高度扩展行，条形图字段仍保持紧凑单行。</summary>
        private static float MetricHeight(float width, ChipMetricPreview metric)
        {
            float labelWidth = Mathf.Min(138f, width * 0.34f);
            float valueWidth = metric.ShowBar ? 76f : width - labelWidth - 6f;
            float labelHeight = Text.CalcHeight(metric.LabelKey.Translate(), labelWidth - 8f);
            float valueHeight = Text.CalcHeight(metric.ValueText ?? "", valueWidth);
            return Mathf.Max(RowHeight, Mathf.Max(labelHeight, valueHeight) + 4f);
        }
    }
}
