using BDP.Content.Assembly.ChipManufacturing.Production;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口右侧芯片详情面板。
    /// 面板只消费统一解析模型；没有选中芯片时保持空白，不保留上一枚芯片的详情。
    /// </summary>
    internal sealed class Panel_ChipDetail
    {
        /// <summary>右栏面板底色，与左栏和中栏保持一致。</summary>
        private static readonly Color PanelColor = new Color(0.035f, 0.095f, 0.115f, 0.96f);

        /// <summary>右栏面板边框色，与左栏和中栏保持一致。</summary>
        private static readonly Color BorderColor = new Color(0.18f, 0.42f, 0.46f, 0.92f);

        /// <summary>右栏标题高度。</summary>
        private const float HeaderHeight = 58f;

        /// <summary>详情滚动位置。</summary>
        private Vector2 scrollPosition;

        /// <summary>当前详情所对应的芯片实例。</summary>
        private Thing displayedChip;

        /// <summary>绘制选中芯片详情。</summary>
        internal void DrawChipDetail(
            Rect rect,
            Thing selectedChip,
            ChipInformationComparisonModel comparison)
        {
            DrawPanelFrame(rect, true);
            if (selectedChip == null || comparison == null || comparison.Current == null)
            {
                if (displayedChip != null)
                {
                    ChipInformationDetailRenderer.ResetDeclaredComboExpansion();
                }
                displayedChip = null;
                scrollPosition = Vector2.zero;
                return;
            }

            if (!ReferenceEquals(displayedChip, selectedChip))
            {
                ChipInformationDetailRenderer.ResetDeclaredComboExpansion();
                displayedChip = selectedChip;
                scrollPosition = Vector2.zero;
            }

            DrawScrollableContent(
                rect,
                selectedChip.LabelCap.ToString(),
                comparison,
                ref scrollPosition,
                new ChipInformationDetailRenderer.ChipDetailIconSource(selectedChip));
        }

        /// <summary>绘制制造台预览并复用同一面板框架、滚动区与详情渲染器。</summary>
        internal static void DrawManufacturingChipDetail(
            Rect rect,
            string chipLabel,
            ChipInformationComparisonModel comparison,
            string statusText,
            ref Vector2 scrollPosition)
        {
            DrawPanelFrame(rect, false);
            if (!statusText.NullOrEmpty())
            {
                Rect statusRect = new Rect(rect.x + 12f, rect.y + HeaderHeight + 12f,
                    rect.width - 24f, rect.height - HeaderHeight - 24f);
                DrawLabel(statusRect, statusText, GameFont.Small,
                    new Color(0.78f, 0.82f, 0.88f, 1f), TextAnchor.UpperLeft);
                return;
            }

            if (comparison?.Current == null)
            {
                return;
            }

            ThingDef fallbackDef = DefDatabase<ThingDef>.GetNamedSilentFail(
                ChipProductCreationService.ManufacturedChipDefName);
            DrawScrollableContent(
                rect,
                chipLabel,
                comparison,
                ref scrollPosition,
                fallbackDef == null ? null : new ChipInformationDetailRenderer.ChipDetailIconSource(fallbackDef));
        }

        /// <summary>使用统一详情渲染器绘制当前面板的可滚动内容。</summary>
        private static void DrawScrollableContent(
            Rect rect,
            string chipLabel,
            ChipInformationComparisonModel comparison,
            ref Vector2 currentScrollPosition,
            ChipInformationDetailRenderer.ChipDetailIconSource iconSource = null)
        {
            chipLabel = ChipDisplayNameFormatter.Format(chipLabel);
            Rect body = new Rect(
                rect.x + 1f,
                rect.y + HeaderHeight + 1f,
                rect.width - 2f,
                rect.height - HeaderHeight - 2f);
            Rect inner = body.ContractedBy(10f);
            Rect outRect = new Rect(inner.x, inner.y, inner.width, inner.height);
            float contentHeight = ChipInformationDetailRenderer.MeasureContentHeight(
                chipLabel,
                comparison,
                inner.width - 16f);
            Rect viewRect = new Rect(0f, 0f, inner.width - 16f, Mathf.Max(inner.height, contentHeight));

            Widgets.BeginScrollView(outRect, ref currentScrollPosition, viewRect);
            Rect visibleContentRect = new Rect(
                currentScrollPosition.x,
                currentScrollPosition.y,
                outRect.width,
                outRect.height);
            ChipInformationDetailRenderer.DrawContent(
                new Rect(0f, 0f, viewRect.width, contentHeight),
                chipLabel,
                comparison,
                visibleContentRect,
                iconSource);
            Widgets.EndScrollView();
        }

        /// <summary>绘制右栏外框和芯片详情标题。</summary>
        private static void DrawPanelFrame(Rect rect, bool showAssemblyLabel)
        {
            Widgets.DrawBoxSolidWithOutline(rect, PanelColor, BorderColor);
            Rect headerRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, HeaderHeight);
            Widgets.DrawBoxSolid(headerRect, new Color(0.04f, 0.15f, 0.17f, 0.94f));
            if (showAssemblyLabel)
            {
                DrawLabel(
                    new Rect(headerRect.x + 12f, headerRect.y + 8f, headerRect.width - 24f, 14f),
                    "BDP_Window_TriggerAssembly_DetailPanel".Translate().ToString(),
                    GameFont.Tiny,
                    new Color(0.29f, 0.84f, 0.82f, 1f),
                    TextAnchor.MiddleLeft);
            }

            DrawLabel(
                new Rect(headerRect.x + 12f, headerRect.y + (showAssemblyLabel ? 25f : 16f), headerRect.width - 24f, 24f),
                "BDP_Window_TriggerAssembly_DetailPanelTitle".Translate().ToString(),
                GameFont.Medium,
                new Color(0.86f, 0.95f, 0.93f, 1f),
                TextAnchor.MiddleLeft);
            Widgets.DrawLineHorizontal(headerRect.x + 10f, headerRect.yMax, headerRect.width - 20f);
        }

        /// <summary>绘制统一样式的右栏文字。</summary>
        private static void DrawLabel(Rect rect, string text, GameFont font, Color color, TextAnchor anchor)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = font;
            Text.Anchor = anchor;
            GUI.color = color;
            Widgets.Label(rect, text ?? string.Empty);
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }

    }
}
