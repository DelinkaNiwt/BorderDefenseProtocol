using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using Verse;
using UnityEngine;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>制造台首页：提供制造方向导航，并展示当前真实制造队列。</summary>
    public sealed class ChipManufacturingOverviewPanel
    {
        private const float HeaderHeight = 52f;
        private const float ColumnGap = 10f;
        private const float LeftColumnRatio = 0.58f;
        private const float PanelPadding = 10f;
        private const float CategoryCardHeight = 82f;
        private const float CategoryCardGap = 8f;

        private Vector2 categoryScrollPosition;

        /// <summary>绘制首页总览及其分类导航卡片。</summary>
        public void Draw(
            Rect rect,
            Building_ChipFabricator building,
            ChipManufacturingEditorState editorState,
            IList<ChipCategoryDef> categories,
            Action<ChipCategoryDef> onCategorySelected,
            ChipManufacturingQueuePanel queuePanel,
            Action onConfigurationLoaded)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Rect inner = rect.ContractedBy(PanelPadding);
            DrawHeader(inner, out float contentY);

            Rect content = new Rect(
                inner.x,
                contentY,
                inner.width,
                Mathf.Max(0f, inner.yMax - contentY));
            float leftWidth = Mathf.Floor(
                (content.width - ColumnGap) * LeftColumnRatio);
            float rightWidth = content.width - leftWidth - ColumnGap;
            Rect left = new Rect(content.x, content.y, leftWidth, content.height);
            Rect right = new Rect(
                left.xMax + ColumnGap,
                content.y,
                rightWidth,
                content.height);

            DrawCategoryPanel(left, categories, onCategorySelected);
            Widgets.DrawMenuSection(right);
            queuePanel?.Draw(
                right.ContractedBy(PanelPadding),
                building,
                editorState,
                onConfigurationLoaded);
        }

        /// <summary>绘制制造台首页标题与简短引导。</summary>
        private static void DrawHeader(Rect rect, out float contentY)
        {
            Widgets.Label(
                new Rect(rect.x, rect.y, rect.width, 28f),
                "BDP_ChipManufacturing_Overview_Title".Translate());
            Widgets.Label(
                new Rect(rect.x, rect.y + 28f, rect.width, 22f),
                "BDP_ChipManufacturing_Overview_Subtitle".Translate());
            contentY = rect.y + HeaderHeight;
        }

        /// <summary>绘制五个主分类的导航卡片。</summary>
        private void DrawCategoryPanel(
            Rect rect,
            IList<ChipCategoryDef> categories,
            Action<ChipCategoryDef> onCategorySelected)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(PanelPadding);
            Widgets.Label(
                new Rect(inner.x, inner.y, inner.width, 24f),
                "BDP_ChipManufacturing_Overview_Directions".Translate());

            Rect outRect = new Rect(
                inner.x,
                inner.y + 28f,
                inner.width,
                Mathf.Max(0f, inner.height - 28f));
            int count = categories?.Count ?? 0;
            int rows = Mathf.CeilToInt(count / 2f);
            float viewHeight = rows * CategoryCardHeight
                + Mathf.Max(0, rows - 1) * CategoryCardGap;
            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(1f, outRect.width - 16f),
                Mathf.Max(outRect.height, viewHeight));
            Widgets.BeginScrollView(outRect, ref categoryScrollPosition, viewRect);

            for (int index = 0; index < count; index++)
            {
                int row = index / 2;
                int column = index % 2;
                float cardWidth = (viewRect.width - CategoryCardGap) / 2f;
                Rect card = new Rect(
                    column * (cardWidth + CategoryCardGap),
                    row * (CategoryCardHeight + CategoryCardGap),
                    cardWidth,
                    CategoryCardHeight);
                DrawCategoryCard(card, categories[index], onCategorySelected);
            }

            Widgets.EndScrollView();
        }

        /// <summary>绘制单个分类卡片及其动作数量概览。</summary>
        private static void DrawCategoryCard(
            Rect rect,
            ChipCategoryDef category,
            Action<ChipCategoryDef> onCategorySelected)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.12f, 0.62f));
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 24f),
                category.LabelCap);

            Text.Font = GameFont.Tiny;
            string description = category.description ?? string.Empty;
            Widgets.Label(
                new Rect(rect.x + 10f, rect.y + 31f, rect.width - 20f, 22f),
                description);
            Widgets.Label(
                new Rect(rect.x + 10f, rect.y + 57f, rect.width - 20f, 18f),
                "BDP_ChipManufacturing_Overview_ActionCount".Translate(
                    ChipManufacturingListModel.GetActionCount(category)));
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;

            if (Widgets.ButtonInvisible(rect))
            {
                onCategorySelected?.Invoke(category);
            }
        }
    }
}
