using System.Collections.Generic;
using BDP.Core.UI.Charts;
using UnityEngine;
using Verse;

namespace BDP.Development.UI.Charts
{
    /// <summary>Development 专用中性图表视觉预览窗口。</summary>
    public sealed class Window_ChartToolsPreview : Window
    {
        /// <summary>纵向滚动位置。</summary>
        private Vector2 scrollPosition;

        /// <summary>创建图表工具预览窗口。</summary>
        public Window_ChartToolsPreview()
        {
            forcePause = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
        }

        /// <summary>预览窗口尺寸。</summary>
        public override Vector2 InitialSize
        {
            get { return new Vector2(1120f, 760f); }
        }

        /// <summary>绘制固定假数据图表并验证滚动裁切。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 30f),
                "BDP_Development_ChartTools_Title".Translate());
            GUI.color = new Color(0.62f, 0.76f, 0.75f, 1f);
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 30f, inRect.width, 24f),
                "BDP_Development_ChartTools_Description".Translate());
            GUI.color = Color.white;

            Rect viewport = new Rect(inRect.x, inRect.y + 62f, inRect.width, inRect.height - 62f);
            Rect view = new Rect(0f, 0f, viewport.width - 16f, 1040f);
            Widgets.BeginScrollView(viewport, ref scrollPosition, view);
            try
            {
                Rect visibleContentRect = new Rect(
                    scrollPosition.x,
                    scrollPosition.y,
                    viewport.width - 16f,
                    viewport.height);
                DrawRadarPreviews(view.width, visibleContentRect);
                DrawLinePreview(view.width, visibleContentRect);
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        /// <summary>绘制六轴和四轴雷达图样例。</summary>
        private static void DrawRadarPreviews(float width, Rect visibleContentRect)
        {
            Rect first = new Rect(0f, 8f, width, 340f);
            Widgets.Label(new Rect(first.x, first.y, first.width, 24f), "BDP_Development_ChartTools_Radar".Translate());
            RadarChartRenderer.Draw(
                new Rect(first.x, first.y + 28f, first.width, 300f),
                visibleContentRect,
                CreateRangedRadarData(),
                CreateRadarStyle());

            Rect second = new Rect(0f, 360f, width, 270f);
            Widgets.Label(new Rect(second.x, second.y, second.width, 24f), "BDP_Development_ChartTools_RadarSmall".Translate());
            RadarChartRenderer.Draw(
                new Rect(second.x, second.y + 28f, second.width, 230f),
                visibleContentRect,
                CreateMeleeRadarData(),
                CreateRadarStyle());
        }

        /// <summary>绘制两条可比较折线样例。</summary>
        private static void DrawLinePreview(float width, Rect visibleContentRect)
        {
            Rect rect = new Rect(0f, 650f, width, 360f);
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), "BDP_Development_ChartTools_Line".Translate());
            LineChartRenderer.Draw(
                new Rect(rect.x, rect.y + 28f, rect.width, 320f),
                visibleContentRect,
                CreateLineData(),
                CreateLineStyle());
        }

        /// <summary>创建六轴雷达假数据。</summary>
        private static RadarChartData CreateRangedRadarData()
        {
            List<RadarChartAxis> axes = new List<RadarChartAxis>
            {
                new RadarChartAxis("射程", 0f, 80f),
                new RadarChartAxis("伤害", 0f, 60f),
                new RadarChartAxis("穿透", 0f, 50f),
                new RadarChartAxis("弹速", 0f, 200f),
                new RadarChartAxis("停止力", 0f, 4f),
                new RadarChartAxis("频率", 0f, 180f)
            };
            ChartSeriesStyle baseStyle = new ChartSeriesStyle
            {
                LineColor = new Color(0.43f, 0.61f, 0.63f, 0.90f),
                FillColor = new Color(0.28f, 0.46f, 0.47f, 1f),
                FillOpacity = 0.14f,
                Dashed = true,
                Filled = true,
                ShowValues = false
            };
            ChartSeriesStyle currentStyle = new ChartSeriesStyle
            {
                LineColor = new Color(0.30f, 0.84f, 0.82f, 1f),
                FillColor = new Color(0.18f, 0.55f, 0.56f, 1f),
                FillOpacity = 0.24f,
                Filled = true,
                ShowValues = true,
                ValueColor = new Color(0.30f, 0.84f, 0.82f, 1f)
            };
            return new RadarChartData(
                axes,
                new List<RadarChartSeries>
                {
                    new RadarChartSeries("基础", new[] { 48f, 30f, 22f, 120f, 2f, 90f }, baseStyle),
                    new RadarChartSeries("最终", new[] { 64f, 43f, 35f, 170f, 3f, 135f }, currentStyle)
                });
        }

        /// <summary>创建四轴雷达假数据。</summary>
        private static RadarChartData CreateMeleeRadarData()
        {
            List<RadarChartAxis> axes = new List<RadarChartAxis>
            {
                new RadarChartAxis("距离", 0f, 3f),
                new RadarChartAxis("力量", 0f, 30f),
                new RadarChartAxis("穿透", 0f, 50f),
                new RadarChartAxis("频率", 0f, 100f)
            };
            ChartSeriesStyle style = new ChartSeriesStyle
            {
                LineColor = new Color(0.30f, 0.84f, 0.82f, 1f),
                FillColor = new Color(0.18f, 0.55f, 0.56f, 1f),
                FillOpacity = 0.24f,
                Filled = true,
                ShowValues = true,
                ValueColor = new Color(0.30f, 0.84f, 0.82f, 1f)
            };
            return new RadarChartData(
                axes,
                new List<RadarChartSeries>
                {
                    new RadarChartSeries("近战", new[] { 2.2f, 24f, 18f, 72f }, style)
                });
        }

        /// <summary>创建精度走势等价的折线假数据。</summary>
        private static LineChartData CreateLineData()
        {
            ChartSeriesStyle baseStyle = new ChartSeriesStyle
            {
                LineColor = new Color(0.43f, 0.61f, 0.63f, 0.90f),
                Dashed = true,
                ShowPoints = true,
                PointColor = new Color(0.43f, 0.61f, 0.63f, 0.90f)
            };
            ChartSeriesStyle currentStyle = new ChartSeriesStyle
            {
                LineColor = new Color(0.30f, 0.84f, 0.82f, 1f),
                ShowPoints = true,
                PointColor = new Color(0.30f, 0.84f, 0.82f, 1f),
                ShowValues = true,
                ValueColor = new Color(0.30f, 0.84f, 0.82f, 1f)
            };
            return new LineChartData(
                new ChartRange(0f, 50f),
                new ChartRange(0f, 100f),
                new List<LineChartSeries>
                {
                    new LineChartSeries("基础", new[]
                    {
                        new ChartPoint(0f, 85f), new ChartPoint(3f, 85f), new ChartPoint(12f, 70f),
                        new ChartPoint(25f, 45f), new ChartPoint(40f, 20f), new ChartPoint(50f, 20f)
                    }, baseStyle),
                    new LineChartSeries("最终", new[]
                    {
                        new ChartPoint(0f, 100f), new ChartPoint(3f, 100f), new ChartPoint(12f, 88f),
                        new ChartPoint(25f, 62f), new ChartPoint(40f, 34f), new ChartPoint(50f, 34f)
                    }, currentStyle)
                },
                new[] { 0f, 3f, 12f, 25f, 40f, 50f },
                new[] { 0f, 50f, 100f });
        }

        /// <summary>创建雷达图默认预览样式。</summary>
        private static ChartStyle CreateRadarStyle()
        {
            return new ChartStyle
            {
                GridLevels = 4,
                LabelColor = new Color(0.70f, 0.82f, 0.81f, 1f),
                ValueColor = new Color(0.30f, 0.84f, 0.82f, 1f)
            };
        }

        /// <summary>创建折线图默认预览样式。</summary>
        private static ChartStyle CreateLineStyle()
        {
            return new ChartStyle
            {
                LabelColor = new Color(0.70f, 0.82f, 0.81f, 1f),
                ValueColor = new Color(0.30f, 0.84f, 0.82f, 1f),
                ValueFormatter = value => value.ToString("0.#") + "%"
            };
        }
    }
}
