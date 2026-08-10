using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 芯片仓内容查看页签。
    /// </summary>
    public class ITab_ChipStorageContents : ITab_ContentsBase
    {
        /// <summary>
        /// 原版基因仓使用的单项弹出按钮图标。
        /// </summary>
        private static readonly CachedTexture DropTex = new CachedTexture("UI/Buttons/Drop");

        /// <summary>
        /// 空内容列表，用于组件缺失时避免空引用。
        /// </summary>
        private static readonly List<Thing> EmptyContents = new List<Thing>();

        /// <summary>
        /// 芯片图标左边距，对齐原版基因仓内容行。
        /// </summary>
        private const float ChipIconX = 24f;

        /// <summary>
        /// 芯片名称左边距，对齐原版基因仓内容行。
        /// </summary>
        private const float ChipLabelX = 60f;

        /// <summary>
        /// 行按钮尺寸。
        /// </summary>
        private const float RowButtonSize = 24f;

        /// <summary>
        /// 返回芯片仓内部容器中的真实持有物。
        /// </summary>
        public override IList<Thing> container
        {
            get
            {
                CompChipContainer chipContainer = SelThing?.TryGetComp<CompChipContainer>();
                if (chipContainer != null)
                {
                    return chipContainer.InnerContainer;
                }

                return EmptyContents;
            }
        }

        /// <summary>
        /// 构造芯片仓内容查看页签。
        /// </summary>
        public ITab_ChipStorageContents()
        {
            labelKey = "TabCasketContents";
            containedItemsKey = "TabCasketContents";
        }

        /// <summary>
        /// 绘制基因仓式内容列表，而不是原版通用容器的红叉丢弃列表。
        /// </summary>
        protected override void DoItemsLists(Rect inRect, ref float curY)
        {
            Widgets.BeginGroup(inRect);
            Widgets.ListSeparator(ref curY, inRect.width, containedItemsKey.Translate());

            IList<Thing> chips = container;
            bool hasChip = false;
            for (int i = 0; i < chips.Count; i++)
            {
                Thing chip = chips[i];
                if (chip == null)
                {
                    continue;
                }

                hasChip = true;
                DoChipRow(chip, inRect.width, ref curY);
            }

            if (!hasChip)
            {
                Widgets.NoneLabel(ref curY, inRect.width);
            }

            Widgets.EndGroup();
        }

        /// <summary>
        /// 绘制单个芯片行：信息按钮、芯片图标、名称和右侧弹出箭头。
        /// </summary>
        private void DoChipRow(Thing chip, float width, ref float curY)
        {
            Rect rowRect = new Rect(0f, curY, width, ThingRowHeight);
            Rect dropRect = new Rect(
                rowRect.xMax - RowButtonSize,
                curY + (ThingRowHeight - RowButtonSize) / 2f,
                RowButtonSize,
                RowButtonSize);
            Rect bodyRect = new Rect(rowRect.x, rowRect.y, rowRect.width - RowButtonSize, rowRect.height);

            DrawChipRowHighlight(chip, bodyRect);
            Widgets.InfoCardButton(0f, curY, chip);
            Widgets.ThingIcon(new Rect(ChipIconX, curY, ThingIconSize, ThingIconSize), chip);
            DrawChipLabel(chip, rowRect, dropRect);
            DrawDropButton(chip, dropRect);

            curY += ThingRowHeight;
        }

        /// <summary>
        /// 鼠标悬停时绘制行高亮，并让原版目标高亮系统尝试定位该芯片。
        /// </summary>
        private static void DrawChipRowHighlight(Thing chip, Rect bodyRect)
        {
            if (!Mouse.IsOver(bodyRect))
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = ThingHighlightColor;
            GUI.DrawTexture(bodyRect, TexUI.HighlightTex);
            GUI.color = oldColor;
            TargetHighlighter.Highlight(chip, arrow: true, colonistBar: false);
            TooltipHandler.TipRegion(bodyRect, chip.LabelCap);
        }

        /// <summary>
        /// 绘制芯片名称文本。
        /// </summary>
        private static void DrawChipLabel(Thing chip, Rect rowRect, Rect dropRect)
        {
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;

            GUI.color = ThingLabelColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Rect labelRect = new Rect(
                ChipLabelX,
                rowRect.y,
                Mathf.Max(0f, dropRect.x - ChipLabelX - 4f),
                rowRect.height);
            Widgets.Label(labelRect, chip.LabelCap.Truncate(labelRect.width));

            Text.WordWrap = oldWordWrap;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }

        /// <summary>
        /// 绘制右侧弹出按钮，并直接把芯片落到芯片仓附近。
        /// </summary>
        private void DrawDropButton(Thing chip, Rect dropRect)
        {
            if (Widgets.ButtonImage(dropRect, DropTex.Texture))
            {
                OnDropThing(chip, chip.stackCount);
            }

            TooltipHandler.TipRegion(dropRect, "BDP_Window_ChipStorage_DropTooltip".Translate());
        }
    }
}
