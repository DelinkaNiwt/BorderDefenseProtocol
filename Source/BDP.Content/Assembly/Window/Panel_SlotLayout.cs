using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口左侧构型与操作者面板。
    /// 所有可变槽位都在固定的内部滚动区内绘制，避免槽位数量改变外部三栏尺寸。
    /// </summary>
    internal sealed class Panel_SlotLayout
    {
        /// <summary>最近一次绘制生成的槽位命中区域。</summary>
        private readonly List<TriggerAssemblySlotHit> slotHits = new List<TriggerAssemblySlotHit>();

        /// <summary>最近一次绘制生成的可清除槽位选择的板块区域。</summary>
        private readonly List<Rect> slotBoardRects = new List<Rect>();

        /// <summary>左栏内部滚动位置。</summary>
        private Vector2 scrollPosition;

        /// <summary>
        /// 一轮左栏绘制共用的芯片卡片文字度量。
        /// 字号按本轮最长内容统一确定，避免同类卡片因名称长度不同而出现字号跳变。
        /// </summary>
        private sealed class SlotTextMetrics
        {
            /// <summary>分类文字字号。</summary>
            internal readonly int CategoryFontSize;

            /// <summary>芯片名称字号。</summary>
            internal readonly int NameFontSize;

            /// <summary>构造统一文字度量。</summary>
            internal SlotTextMetrics(int categoryFontSize, int nameFontSize)
            {
                CategoryFontSize = categoryFontSize;
                NameFontSize = nameFontSize;
            }
        }

        /// <summary>左栏标题高度。</summary>
        private const float HeaderHeight = 58f;

        /// <summary>左栏操作者名称区高度。</summary>
        private const float OperatorMetaHeight = 32f;

        /// <summary>左栏芯片预占用区高度；删除底部摘要后按标题和轨道收紧。</summary>
        private const float TrionCapacityHeight = 62f;

        /// <summary>区块之间的间隔。</summary>
        private const float SectionGap = 8f;

        /// <summary>槽位卡片目标长宽比（宽 : 高）。</summary>
        private const float SlotCardAspectRatio = 3.3f;

        /// <summary>槽位卡片之间的垂直间隔。</summary>
        private const float SlotGap = 8f;

        /// <summary>装配示意区四周的内边距。</summary>
        private const float StageInset = 8f;

        /// <summary>特殊槽位卡片的最大宽度。</summary>
        private const float SpecialSlotMaxWidth = 136f;

        /// <summary>槽位文字区占卡片宽度的最低比例。</summary>
        private const float SlotTextWidthRatio = 0.72f;

        /// <summary>槽位文字区起始位置占卡片宽度的比例。</summary>
        private const float SlotTextLeftRatio = 0.26f;

        /// <summary>槽位图标区左侧内边距占卡片宽度的比例。</summary>
        private const float SlotIconInsetRatio = 0.05f;

        /// <summary>槽位图标边长上限。</summary>
        private const float SlotIconMaxSize = 28f;

        /// <summary>槽位卡片文字上下内边距。</summary>
        private const float SlotTextVerticalPadding = 2f;

        /// <summary>分类文字和芯片名称之间的间隔。</summary>
        private const float SlotTextLineGap = 0f;

        /// <summary>分类文字相对基础字号的缩放比例。</summary>
        private const float SlotCategoryFontScale = 0.78f;

        /// <summary>分类文字的固定行高。</summary>
        private const float SlotCategoryHeight = 14f;

        /// <summary>芯片名称保留两行空间，容纳完整构型名称。</summary>
        private const float SlotNameHeight = 36f;

        /// <summary>悬停放置结果卡片的整体透明度。</summary>
        private const float PreviewOpacity = 0.76f;

        /// <summary>左栏青绿色主色。</summary>
        private static readonly Color AccentColor = new Color(0.29f, 0.84f, 0.82f, 1f);

        /// <summary>左栏边框色。</summary>
        private static readonly Color BorderColor = new Color(0.18f, 0.42f, 0.46f, 0.92f);

        /// <summary>左栏底色。</summary>
        private static readonly Color PanelColor = new Color(0.035f, 0.095f, 0.115f, 0.96f);

        /// <summary>左栏卡片底色。</summary>
        private static readonly Color CardColor = new Color(0.045f, 0.14f, 0.16f, 0.92f);

        /// <summary>成对槽位的低对比关联线颜色。</summary>
        private static readonly Color PairedLinkColor = new Color(0.36f, 0.65f, 0.62f, 0.62f);

        /// <summary>绘制原型结构的左栏。</summary>
        internal void DrawSlotLayout(
            Rect rect,
            Pawn pawn,
            AssemblyPawnPortraitSnapshot operatorPortrait,
            ITriggerLoadoutReader reader,
            TriggerAssemblyPreviewSnapshot preview,
            TriggerAssemblyDragState dragState,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int selectedIndex)
        {
            Vector2 mousePosition = Event.current.mousePosition;
            slotHits.Clear();
            slotBoardRects.Clear();
            DrawPanelFrame(rect);

            Rect bodyRect = new Rect(
                rect.x + 1f,
                rect.y + HeaderHeight + 1f,
                rect.width - 2f,
                rect.height - HeaderHeight - 2f);
            int specialCount = CountSlots(reader, TriggerSide.Special);
            float contentWidth = Mathf.Max(1f, bodyRect.width - 18f);
            float contentLeft = Mathf.Max(0f, (bodyRect.width - contentWidth) * 0.5f);
            float sideWidth = GetStageSideWidth(contentWidth);
            float stageHeight = Mathf.Max(
                410f,
                16f + Mathf.Max(
                    GetSlotColumnHeight(reader, TriggerSide.Main, sideWidth),
                    GetSlotColumnHeight(reader, TriggerSide.Sub, sideWidth)));
            int specialRows = Mathf.Max(1, (specialCount + 1) / 2);
            float specialCardWidth = GetSpecialCardWidth(contentWidth);
            SlotTextMetrics textMetrics = BuildSlotTextMetrics(reader, specialCardWidth);
            float specialHeight = specialCount == 0
                ? 70f
                : 42f + GetSpecialRowsHeight(reader, specialRows, specialCardWidth);
            float contentHeight = OperatorMetaHeight
                + SectionGap
                + stageHeight
                + SectionGap
                + specialHeight
                + SectionGap
                + TrionCapacityHeight
                + 12f;
            Rect viewRect = new Rect(0f, 0f, bodyRect.width, Mathf.Max(bodyRect.height, contentHeight));

            Widgets.BeginScrollView(bodyRect, ref scrollPosition, viewRect);
            float currentY = 0f;
            DrawOperatorMeta(new Rect(contentLeft, currentY, contentWidth, OperatorMetaHeight), pawn);
            currentY += OperatorMetaHeight + SectionGap;

            Rect stageRect = new Rect(contentLeft, currentY, contentWidth, stageHeight);
            DrawAssemblyStage(
                stageRect,
                reader,
                operatorPortrait,
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex,
                textMetrics,
                bodyRect,
                mousePosition);
            AddVisibleBoardRect(stageRect, bodyRect);
            currentY += stageHeight + SectionGap;

            Rect specialRect = new Rect(contentLeft, currentY, contentWidth, specialHeight);
            DrawSpecialSection(
                specialRect,
                reader,
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex,
                textMetrics,
                bodyRect,
                mousePosition);
            AddVisibleBoardRect(specialRect, bodyRect);
            currentY += specialHeight + SectionGap;

            DrawTrionCapacity(new Rect(contentLeft, currentY, contentWidth, TrionCapacityHeight), preview);
            Widgets.EndScrollView();
        }

        /// <summary>按鼠标坐标查找最近一次绘制的槽位。</summary>
        internal TriggerAssemblySlotHit HitAt(Vector2 mousePosition)
        {
            for (int i = 0; i < slotHits.Count; i++)
            {
                if (slotHits[i].Rect.Contains(mousePosition))
                {
                    return slotHits[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 判断鼠标是否位于槽位板块内、但不在任何槽位格子上。
        /// 空槽位仍然是合法的装配目标，不能被当作板块空白区。
        /// </summary>
        internal bool IsBlankAreaAt(Vector2 mousePosition)
        {
            if (HitAt(mousePosition) != null)
            {
                return false;
            }

            for (int i = 0; i < slotBoardRects.Count; i++)
            {
                if (slotBoardRects[i].Contains(mousePosition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>绘制左栏外框和原型标题。</summary>
        private static void DrawPanelFrame(Rect rect)
        {
            Widgets.DrawBoxSolidWithOutline(rect, PanelColor, BorderColor);
            Rect headerRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, HeaderHeight);
            Widgets.DrawBoxSolid(headerRect, new Color(0.04f, 0.15f, 0.17f, 0.94f));
            DrawLabel(
                new Rect(headerRect.x + 12f, headerRect.y + 8f, headerRect.width - 24f, 14f),
                "BDP_Window_TriggerAssembly_AssemblyProfile".Translate().ToString(),
                GameFont.Tiny,
                AccentColor,
                TextAnchor.MiddleLeft);
            DrawLabel(
                new Rect(headerRect.x + 12f, headerRect.y + 25f, headerRect.width - 115f, 24f),
                "BDP_Window_TriggerAssembly_AssemblyProfileTitle".Translate().ToString(),
                GameFont.Medium,
                new Color(0.86f, 0.95f, 0.93f, 1f),
                TextAnchor.MiddleLeft);
            DrawLabel(
                new Rect(headerRect.xMax - 102f, headerRect.y + 28f, 90f, 18f),
                "BDP_Window_TriggerAssembly_LiveResult".Translate().ToString(),
                GameFont.Tiny,
                new Color(0.42f, 0.66f, 0.66f, 1f),
                TextAnchor.MiddleRight);
            Widgets.DrawLineHorizontal(headerRect.x + 10f, headerRect.yMax, headerRect.width - 20f);
        }

        /// <summary>绘制操作者摘要。</summary>
        private static void DrawOperatorMeta(Rect rect, Pawn pawn)
        {
            string pawnName = pawn != null
                ? pawn.LabelShortCap
                : "BDP_Window_TriggerAssembly_Unknown".Translate().ToString();
            DrawLabel(
                new Rect(rect.x + 8f, rect.y + 4f, rect.width - 100f, 22f),
                "BDP_Window_TriggerAssembly_OperatorName".Translate(pawnName).ToString(),
                GameFont.Small,
                new Color(0.86f, 0.94f, 0.92f, 1f),
                TextAnchor.MiddleLeft);
            Widgets.DrawLineHorizontal(rect.x + 8f, rect.yMax - 1f, rect.width - 16f);
        }

        /// <summary>绘制主手/副手槽位和中央操作者实际肖像。</summary>
        private void DrawAssemblyStage(
            Rect rect,
            ITriggerLoadoutReader reader,
            AssemblyPawnPortraitSnapshot operatorPortrait,
            TriggerAssemblyDragState dragState,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int selectedIndex,
            SlotTextMetrics textMetrics,
            Rect viewportRect,
            Vector2 mousePosition)
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.02f, 0.08f, 0.10f, 0.82f), BorderColor);
            DrawGrid(rect);

            float gap = SlotGap;
            float centerWidth = Mathf.Min(146f, rect.width * 0.40f);
            float sideWidth = GetStageSideWidth(rect.width);
            Rect mainRect = new Rect(rect.x + StageInset, rect.y + StageInset, sideWidth, rect.height - StageInset * 2f);
            Rect centerRect = new Rect(mainRect.xMax + gap, rect.y + StageInset, centerWidth, rect.height - StageInset * 2f);
            Rect subRect = new Rect(centerRect.xMax + gap, rect.y + StageInset, sideWidth, rect.height - StageInset * 2f);

            List<TriggerAssemblySlotHit> mainSlotCards = BuildSlotColumnLayout(
                mainRect,
                reader,
                TriggerSide.Main,
                out float mainColumnTop);
            List<TriggerAssemblySlotHit> subSlotCards = BuildSlotColumnLayout(
                subRect,
                reader,
                TriggerSide.Sub,
                out float subColumnTop);

            // 关联线先于肖像和卡片绘制，始终位于角色预览贴图下层。
            DrawPairedSlotConnectors(mainSlotCards, subSlotCards);
            DrawOperatorPortrait(centerRect, operatorPortrait);
            DrawSlotColumn(
                mainRect,
                reader,
                "BDP_Window_TriggerAssembly_MainColumn".Translate().ToString(),
                mainColumnTop,
                mainSlotCards,
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex,
                textMetrics,
                viewportRect);
            DrawSlotColumn(
                subRect,
                reader,
                "BDP_Window_TriggerAssembly_SubColumn".Translate().ToString(),
                subColumnTop,
                subSlotCards,
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex,
                textMetrics,
                viewportRect);
            DrawStagePlacementPreview(
                mainSlotCards,
                subSlotCards,
                reader,
                dragState,
                mousePosition,
                textMetrics);
        }

        /// <summary>绘制原版渲染管线生成的操作者正面肖像。</summary>
        private static void DrawOperatorPortrait(
            Rect rect,
            AssemblyPawnPortraitSnapshot operatorPortrait)
        {
            if (operatorPortrait == null || operatorPortrait.Texture == null)
            {
                return;
            }

            float textureAspect = (float)operatorPortrait.Texture.width
                / Mathf.Max(1f, operatorPortrait.Texture.height);
            float portraitWidth = Mathf.Min(
                rect.width - StageInset * 2f,
                (rect.height - StageInset * 2f) * textureAspect);
            float portraitHeight = portraitWidth / Mathf.Max(0.01f, textureAspect);
            if (portraitWidth <= 0f || portraitHeight <= 0f)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.BeginGroup(rect);
            GUI.DrawTexture(
                new Rect(
                    (rect.width - portraitWidth) * 0.5f,
                    (rect.height - portraitHeight) * 0.5f,
                    portraitWidth,
                    portraitHeight),
                operatorPortrait.Texture,
                ScaleMode.ScaleToFit,
                true);
            GUI.EndGroup();
            GUI.color = previousColor;
        }

        /// <summary>绘制原型中的细网格背景。</summary>
        private static void DrawGrid(Rect rect)
        {
            Color gridColor = new Color(0.20f, 0.60f, 0.62f, 0.12f);
            for (float x = rect.x + 16f; x < rect.xMax; x += 28f)
            {
                Widgets.DrawLine(new Vector2(x, rect.y), new Vector2(x, rect.yMax), gridColor, 1f);
            }

            for (float y = rect.y + 16f; y < rect.yMax; y += 28f)
            {
                Widgets.DrawLine(new Vector2(rect.x, y), new Vector2(rect.xMax, y), gridColor, 1f);
            }
        }

        /// <summary>计算主手或副手槽位卡片的布局。</summary>
        private List<TriggerAssemblySlotHit> BuildSlotColumnLayout(
            Rect rect,
            ITriggerLoadoutReader reader,
            TriggerSide side,
            out float columnTop)
        {
            List<TriggerAssemblySlotHit> cards = new List<TriggerAssemblySlotHit>();
            float columnHeight = GetSlotColumnHeight(reader, side, rect.width);
            columnTop = rect.y + Mathf.Max(0f, (rect.height - columnHeight) * 0.5f);
            float currentY = columnTop + 18f;
            if (reader == null)
            {
                return cards;
            }

            foreach (ITriggerSlotState slot in reader.GetSlots(side))
            {
                if (slot == null)
                {
                    continue;
                }

                float slotHeight = GetSlotCardHeight(slot, rect.width);
                Rect slotRect = new Rect(rect.x, currentY, rect.width, slotHeight);
                cards.Add(new TriggerAssemblySlotHit(slotRect, slot));
                currentY += slotHeight + SlotGap;
            }

            return cards;
        }

        /// <summary>绘制主手或副手槽位列。</summary>
        private void DrawSlotColumn(
            Rect rect,
            ITriggerLoadoutReader reader,
            string title,
            float columnTop,
            List<TriggerAssemblySlotHit> slotCards,
            TriggerAssemblyDragState dragState,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int selectedIndex,
            SlotTextMetrics textMetrics,
            Rect viewportRect)
        {
            DrawLabel(new Rect(rect.x, columnTop, rect.width, 18f), title, GameFont.Tiny, new Color(0.63f, 0.78f, 0.77f, 1f), TextAnchor.MiddleLeft);

            if (slotCards == null)
            {
                return;
            }

            foreach (TriggerAssemblySlotHit card in slotCards)
            {
                if (card == null || card.Slot == null)
                {
                    continue;
                }

                DrawSlot(card.Rect, reader, card.Slot, dragState, hasSelectedSlot, selectedSide, selectedIndex, textMetrics);
                AddVisibleHit(card.Rect, card.Slot, viewportRect);
            }
        }

        /// <summary>在主副手舞台绘制有效目标及成对伙伴的结果预览。</summary>
        private void DrawStagePlacementPreview(
            List<TriggerAssemblySlotHit> mainSlotCards,
            List<TriggerAssemblySlotHit> subSlotCards,
            ITriggerLoadoutReader reader,
            TriggerAssemblyDragState dragState,
            Vector2 mousePosition,
            SlotTextMetrics textMetrics)
        {
            TriggerAssemblySlotHit hoveredSlot = HitAt(mousePosition);
            if (hoveredSlot == null
                || hoveredSlot.Slot == null
                || hoveredSlot.Slot.Side == TriggerSide.Special
                || !IsValidDropPreviewTarget(hoveredSlot, reader, dragState))
            {
                return;
            }

            ITriggerSlotState targetRoot = ResolveBindingRoot(hoveredSlot.Slot, reader);
            DrawStageSlotPlacementPreview(
                mainSlotCards,
                subSlotCards,
                targetRoot.Side,
                targetRoot.Index,
                dragState.Chip,
                textMetrics);

            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(dragState.Chip);
            TriggerSide partnerSide = targetRoot.HasBindingPartner
                ? targetRoot.BindingPartnerSide
                : targetRoot.Side == TriggerSide.Main ? TriggerSide.Sub : TriggerSide.Main;
            int partnerIndex = targetRoot.HasBindingPartner
                ? targetRoot.BindingPartnerIndex
                : targetRoot.Index;
            ITriggerSlotState partnerSlot = FindSlot(reader, partnerSide, partnerIndex);

            if (snapshot != null && snapshot.SlotOccupancy == ChipSlotOccupancy.PairedHands)
            {
                if (partnerSlot != null)
                {
                    DrawStageSlotPlacementPreview(
                        mainSlotCards,
                        subSlotCards,
                        partnerSide,
                        partnerIndex,
                        dragState.Chip,
                        textMetrics);
                }
            }
            else if (targetRoot.HasBindingPartner && partnerSlot != null)
            {
                DrawStageEmptySlotPlacementPreview(
                    mainSlotCards,
                    subSlotCards,
                    partnerSide,
                    partnerIndex);
            }
        }

        /// <summary>在舞台卡片列表中按侧别和索引绘制一张结果预览。</summary>
        private static void DrawStageSlotPlacementPreview(
            List<TriggerAssemblySlotHit> mainSlotCards,
            List<TriggerAssemblySlotHit> subSlotCards,
            TriggerSide side,
            int index,
            Thing chip,
            SlotTextMetrics textMetrics)
        {
            List<TriggerAssemblySlotHit> cards = side == TriggerSide.Main
                ? mainSlotCards
                : subSlotCards;
            TriggerAssemblySlotHit card = FindSlotCard(cards, index);
            if (card != null)
            {
                DrawSlotPlacementPreview(card.Rect, chip, textMetrics);
            }
        }

        /// <summary>在被替换芯片释放的镜像伙伴槽绘制空槽结果预览。</summary>
        private static void DrawStageEmptySlotPlacementPreview(
            List<TriggerAssemblySlotHit> mainSlotCards,
            List<TriggerAssemblySlotHit> subSlotCards,
            TriggerSide side,
            int index)
        {
            List<TriggerAssemblySlotHit> cards = side == TriggerSide.Main
                ? mainSlotCards
                : subSlotCards;
            TriggerAssemblySlotHit card = FindSlotCard(cards, index);
            if (card != null)
            {
                DrawEmptySlotPlacementPreview(card.Rect);
            }
        }

        /// <summary>验证当前悬停槽是否是拖拽事务可接受的目标。</summary>
        private static bool IsValidDropPreviewTarget(
            TriggerAssemblySlotHit hoveredSlot,
            ITriggerLoadoutReader reader,
            TriggerAssemblyDragState dragState)
        {
            if (hoveredSlot == null
                || hoveredSlot.Slot == null
                || dragState == null
                || !dragState.IsDragging
                || dragState.Chip == null
                || !TriggerAssemblyChipCompatibility.IsCompatibleWithSlot(
                    dragState.Chip,
                    hoveredSlot.Slot.Side)
                || !TriggerAssemblyChipCompatibility.HasAvailablePairedPartner(
                    dragState.Chip,
                    hoveredSlot.Slot,
                    reader))
            {
                return false;
            }

            return dragState.SourceType != TriggerAssemblyDragSourceType.SlotChip
                || !IsSameBindingSelection(
                    hoveredSlot.Slot,
                    dragState.SourceSide,
                    dragState.SourceIndex,
                    reader);
        }

        /// <summary>在特殊槽区的有效悬停目标绘制结果预览。</summary>
        private void DrawSpecialPlacementPreview(
            List<TriggerAssemblySlotHit> specialSlotCards,
            ITriggerLoadoutReader reader,
            TriggerAssemblyDragState dragState,
            Vector2 mousePosition,
            SlotTextMetrics textMetrics)
        {
            TriggerAssemblySlotHit hoveredSlot = HitAt(mousePosition);
            if (hoveredSlot == null
                || hoveredSlot.Slot == null
                || hoveredSlot.Slot.Side != TriggerSide.Special
                || !IsValidDropPreviewTarget(hoveredSlot, reader, dragState))
            {
                return;
            }

            ITriggerSlotState targetRoot = ResolveBindingRoot(hoveredSlot.Slot, reader);
            TriggerAssemblySlotHit card = FindSlotCard(specialSlotCards, targetRoot.Index);
            if (card != null)
            {
                DrawSlotPlacementPreview(card.Rect, dragState.Chip, textMetrics);
            }
        }

        /// <summary>绘制低对比的芯片放置结果卡片，不修改真实槽位内容。</summary>
        private static void DrawSlotPlacementPreview(
            Rect rect,
            Thing chip,
            SlotTextMetrics textMetrics)
        {
            if (chip == null)
            {
                return;
            }

            Color oldGuiColor = GUI.color;
            try
            {
                Widgets.DrawBoxSolidWithOutline(
                    rect,
                    new Color(0.03f, 0.13f, 0.15f, 0.88f),
                    new Color(0.52f, 0.88f, 0.59f, PreviewOpacity));
                Widgets.DrawBoxSolid(
                    new Rect(rect.x, rect.y, 3f, rect.height),
                    new Color(0.52f, 0.89f, 0.58f, PreviewOpacity));
                DrawSlotContent(rect, chip, textMetrics, PreviewOpacity);
            }
            finally
            {
                GUI.color = oldGuiColor;
            }
        }

        /// <summary>绘制芯片替换后释放的空槽结果卡片。</summary>
        private static void DrawEmptySlotPlacementPreview(Rect rect)
        {
            Color oldGuiColor = GUI.color;
            try
            {
                Widgets.DrawBoxSolidWithOutline(
                    rect,
                    new Color(0.02f, 0.08f, 0.10f, 0.88f),
                    new Color(0.19f, 0.43f, 0.47f, PreviewOpacity));
                Widgets.DrawBoxSolid(
                    new Rect(rect.x, rect.y, 3f, rect.height),
                    new Color(0.19f, 0.34f, 0.37f, PreviewOpacity));
                DrawLabel(
                    new Rect(rect.x + 8f, rect.y + 18f, rect.width - 16f, 22f),
                    "BDP_Window_TriggerAssembly_EmptySlot".Translate().ToString(),
                    GameFont.Tiny,
                    WithOpacity(new Color(0.42f, 0.62f, 0.63f, 1f), PreviewOpacity),
                    TextAnchor.MiddleCenter);
            }
            finally
            {
                GUI.color = oldGuiColor;
            }
        }

        /// <summary>绘制卡片芯片图标、类别和名称，预览与正式卡共用位置度量。</summary>
        private static void DrawSlotContent(
            Rect rect,
            Thing chip,
            SlotTextMetrics textMetrics,
            float opacity)
        {
            float iconSize = GetSlotIconSize(rect.width, rect.height);
            Color oldGuiColor = GUI.color;
            try
            {
                GUI.color = WithOpacity(oldGuiColor, opacity);
                Widgets.ThingIcon(
                    new Rect(
                        rect.x + rect.width * SlotIconInsetRatio,
                        rect.y + (rect.height - iconSize) * 0.5f,
                        iconSize,
                        iconSize),
                    chip);
            }
            finally
            {
                GUI.color = oldGuiColor;
            }

            float textLeftOffset = GetSlotTextLeftOffset(rect.width);
            float textWidth = GetSlotTextWidth(rect.width);
            float contentHeight = SlotTextVerticalPadding * 2f
                + SlotCategoryHeight
                + SlotTextLineGap
                + SlotNameHeight;
            float textTop = rect.y + Mathf.Max(
                SlotTextVerticalPadding,
                (rect.height - contentHeight) * 0.5f);
            float categoryY = textTop;
            float nameY = categoryY + SlotCategoryHeight + SlotTextLineGap;
            DrawSlotLabel(
                new Rect(rect.x + textLeftOffset, categoryY, textWidth, SlotCategoryHeight),
                BuildSlotCategoryLabel(chip),
                GameFont.Tiny,
                textMetrics.CategoryFontSize,
                WithOpacity(new Color(0.45f, 0.66f, 0.66f, 1f), opacity),
                TextAnchor.UpperLeft,
                FontStyle.Normal);
            DrawSlotLabel(
                new Rect(rect.x + textLeftOffset, nameY, textWidth, SlotNameHeight),
                BuildSlotChipName(chip),
                GameFont.Small,
                textMetrics.NameFontSize,
                WithOpacity(new Color(0.86f, 0.95f, 0.92f, 1f), opacity),
                TextAnchor.MiddleLeft,
                FontStyle.Bold,
                true);
        }

        /// <summary>按指定透明度调整绘制颜色。</summary>
        private static Color WithOpacity(Color color, float opacity)
        {
            color.a *= opacity;
            return color;
        }

        /// <summary>在主、副槽卡片间绘制实际绑定关系的细线。</summary>
        private static void DrawPairedSlotConnectors(
            List<TriggerAssemblySlotHit> mainSlotCards,
            List<TriggerAssemblySlotHit> subSlotCards)
        {
            DrawPairedSlotConnectorsForRoots(mainSlotCards, subSlotCards);
            DrawPairedSlotConnectorsForRoots(subSlotCards, mainSlotCards);
        }

        /// <summary>从指定根槽绘制到其镜像伙伴槽。</summary>
        private static void DrawPairedSlotConnectorsForRoots(
            List<TriggerAssemblySlotHit> roots,
            List<TriggerAssemblySlotHit> partners)
        {
            if (roots == null || partners == null)
            {
                return;
            }

            foreach (TriggerAssemblySlotHit rootCard in roots)
            {
                ITriggerSlotState root = rootCard != null ? rootCard.Slot : null;
                if (root == null || !root.HasBindingPartner || root.IsBindingMirror)
                {
                    continue;
                }

                TriggerAssemblySlotHit partnerCard = FindSlotCard(partners, root.BindingPartnerIndex);
                ITriggerSlotState partner = partnerCard != null ? partnerCard.Slot : null;
                if (!IsBindingPartner(root, partner))
                {
                    continue;
                }

                Vector2 start = root.Side == TriggerSide.Main
                    ? new Vector2(rootCard.Rect.xMax, rootCard.Rect.center.y)
                    : new Vector2(partnerCard.Rect.xMax, partnerCard.Rect.center.y);
                Vector2 end = root.Side == TriggerSide.Main
                    ? new Vector2(partnerCard.Rect.x, partnerCard.Rect.center.y)
                    : new Vector2(rootCard.Rect.x, rootCard.Rect.center.y);
                Widgets.DrawLine(start, end, PairedLinkColor, 1f);
            }
        }

        /// <summary>查找指定侧列中对应编号的槽位卡片。</summary>
        private static TriggerAssemblySlotHit FindSlotCard(List<TriggerAssemblySlotHit> cards, int index)
        {
            if (cards == null)
            {
                return null;
            }

            foreach (TriggerAssemblySlotHit card in cards)
            {
                if (card != null && card.Slot != null && card.Slot.Index == index)
                {
                    return card;
                }
            }

            return null;
        }

        /// <summary>确认槽位卡片代表同一组根槽与镜像伙伴关系。</summary>
        private static bool IsBindingPartner(ITriggerSlotState root, ITriggerSlotState partner)
        {
            return root != null
                && partner != null
                && root.HasBindingPartner
                && !root.IsBindingMirror
                && partner.HasBindingPartner
                && partner.IsBindingMirror
                && root.BindingPartnerSide == partner.Side
                && root.BindingPartnerIndex == partner.Index
                && partner.BindingRootSide == root.Side
                && partner.BindingRootIndex == root.Index;
        }

        /// <summary>判断当前槽位是否与选中的根槽属于同一组成对芯片。</summary>
        private static bool IsSameBindingSelection(
            ITriggerSlotState slot,
            TriggerSide selectedSide,
            int selectedIndex,
            ITriggerLoadoutReader reader)
        {
            if (slot == null)
            {
                return false;
            }

            if (slot.Side == selectedSide && slot.Index == selectedIndex)
            {
                return true;
            }

            ITriggerSlotState selectedSlot = FindSlot(reader, selectedSide, selectedIndex);
            ITriggerSlotState slotRoot = ResolveBindingRoot(slot, reader);
            ITriggerSlotState selectedRoot = ResolveBindingRoot(selectedSlot, reader);
            return slotRoot != null
                && selectedRoot != null
                && slotRoot.Side == selectedRoot.Side
                && slotRoot.Index == selectedRoot.Index;
        }

        /// <summary>按侧别和槽位编号读取指定槽位。</summary>
        private static ITriggerSlotState FindSlot(
            ITriggerLoadoutReader reader,
            TriggerSide side,
            int index)
        {
            if (reader == null)
            {
                return null;
            }

            foreach (ITriggerSlotState slot in reader.GetSlots(side))
            {
                if (slot != null && slot.Index == index)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>将有效镜像槽解析为绑定根槽，以统一其玩家可见状态。</summary>
        private static ITriggerSlotState ResolveBindingRoot(
            ITriggerSlotState slot,
            ITriggerLoadoutReader reader)
        {
            if (slot == null || !slot.HasBindingPartner || !slot.IsBindingMirror || reader == null)
            {
                return slot;
            }

            ITriggerSlotState root = FindSlot(reader, slot.BindingRootSide, slot.BindingRootIndex);
            return root != null
                && root.HasBindingPartner
                && !root.IsBindingMirror
                && root.BindingPartnerSide == slot.Side
                && root.BindingPartnerIndex == slot.Index
                && root.LoadedChip == slot.LoadedChip
                ? root
                : slot;
        }

        /// <summary>绘制单个原型槽位卡片。</summary>
        private static void DrawSlot(
            Rect rect,
            ITriggerLoadoutReader reader,
            ITriggerSlotState slot,
            TriggerAssemblyDragState dragState,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int selectedIndex,
            SlotTextMetrics textMetrics)
        {
            bool selected = hasSelectedSlot
                && IsSameBindingSelection(slot, selectedSide, selectedIndex, reader);
            bool occupied = slot.LoadedChip != null;
            bool dropTarget = dragState != null
                && dragState.IsDragging
                && TriggerAssemblyChipCompatibility.IsCompatibleWithSlot(dragState.Chip, slot.Side)
                && TriggerAssemblyChipCompatibility.HasAvailablePairedPartner(
                    dragState.Chip,
                    slot,
                    reader);
            Color fillColor = selected
                ? new Color(0.08f, 0.32f, 0.34f, 0.92f)
                : occupied
                    ? CardColor
                    : new Color(0.02f, 0.08f, 0.10f, 0.66f);
            Color borderColor = selected
                ? AccentColor
                : dropTarget
                    ? new Color(0.52f, 0.88f, 0.59f, 0.9f)
                    : new Color(0.19f, 0.43f, 0.47f, 0.82f);
            Widgets.DrawBoxSolidWithOutline(rect, fillColor, borderColor);
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y, 3f, rect.height),
                occupied ? new Color(0.52f, 0.89f, 0.58f, 0.95f) : new Color(0.19f, 0.34f, 0.37f, 0.9f));

            if (occupied)
            {
                DrawSlotContent(rect, slot.LoadedChip, textMetrics, 1f);
            }
            else
            {
                DrawLabel(
                    new Rect(rect.x + 8f, rect.y + 18f, rect.width - 16f, 22f),
                    "BDP_Window_TriggerAssembly_EmptySlot".Translate().ToString(),
                    GameFont.Tiny,
                    new Color(0.42f, 0.62f, 0.63f, 1f),
                    TextAnchor.MiddleCenter);
            }

            TooltipHandler.TipRegion(rect, BuildSlotTooltip(slot, reader));
        }

        /// <summary>绘制特殊槽位区。</summary>
        private void DrawSpecialSection(
            Rect rect,
            ITriggerLoadoutReader reader,
            TriggerAssemblyDragState dragState,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int selectedIndex,
            SlotTextMetrics textMetrics,
            Rect viewportRect,
            Vector2 mousePosition)
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.025f, 0.10f, 0.12f, 0.82f), BorderColor);
            DrawLabel(
                new Rect(rect.x + 9f, rect.y + 6f, rect.width * 0.6f, 18f),
                "BDP_Window_TriggerAssembly_SpecialChannel".Translate().ToString(),
                GameFont.Tiny,
                new Color(0.63f, 0.78f, 0.77f, 1f),
                TextAnchor.MiddleLeft);
            DrawLabel(
                new Rect(rect.xMax - 130f, rect.y + 6f, 120f, 18f),
                "BDP_Window_TriggerAssembly_SpecialChannelNote".Translate().ToString(),
                GameFont.Tiny,
                new Color(0.32f, 0.52f, 0.53f, 1f),
                TextAnchor.MiddleRight);
            Widgets.DrawLineHorizontal(rect.x + 9f, rect.y + 28f, rect.width - 18f);

            int count = CountSlots(reader, TriggerSide.Special);
            if (count == 0)
            {
                DrawLabel(
                    new Rect(rect.x + 8f, rect.y + 36f, rect.width - 16f, 28f),
                    "BDP_Window_TriggerAssembly_NoSpecialSlots".Translate().ToString(),
                    GameFont.Tiny,
                    new Color(0.40f, 0.60f, 0.61f, 1f),
                    TextAnchor.MiddleCenter);
                return;
            }

            float cardWidth = GetSpecialCardWidth(rect.width);
            int columnCount = Mathf.Min(2, count);
            float specialContentWidth = columnCount * cardWidth
                + Mathf.Max(0, columnCount - 1) * SlotGap;
            float startX = rect.x + Mathf.Max(8f, (rect.width - specialContentWidth) * 0.5f);
            int drawn = 0;
            List<TriggerAssemblySlotHit> specialSlotCards = new List<TriggerAssemblySlotHit>();
            int currentRow = -1;
            float currentRowY = rect.y + 36f;
            float currentRowHeight = 0f;
            foreach (ITriggerSlotState slot in reader.GetSlots(TriggerSide.Special))
            {
                int row = drawn / 2;
                int column = drawn % 2;
                if (row != currentRow)
                {
                    if (currentRow >= 0)
                    {
                        currentRowY += currentRowHeight + SlotGap;
                    }

                    currentRow = row;
                    currentRowHeight = GetSpecialRowHeight(cardWidth);
                }

                Rect slotRect = new Rect(
                    startX + column * (cardWidth + SlotGap),
                    currentRowY,
                    cardWidth,
                    currentRowHeight);
                DrawSlot(slotRect, reader, slot, dragState, hasSelectedSlot, selectedSide, selectedIndex, textMetrics);
                AddVisibleHit(slotRect, slot, viewportRect);
                specialSlotCards.Add(new TriggerAssemblySlotHit(slotRect, slot));
                drawn++;
            }

            DrawSpecialPlacementPreview(
                specialSlotCards,
                reader,
                dragState,
                mousePosition,
                textMetrics);
        }

        /// <summary>计算主手/副手槽位列的实际内容高度。</summary>
        private static float GetSlotColumnHeight(
            ITriggerLoadoutReader reader,
            TriggerSide side,
            float slotWidth)
        {
            float height = 18f;
            if (reader == null)
            {
                return height;
            }

            foreach (ITriggerSlotState slot in reader.GetSlots(side))
            {
                height += GetSlotCardHeight(slot, slotWidth) + SlotGap;
            }

            return height;
        }

        /// <summary>计算特殊槽位全部行的实际内容高度。</summary>
        private static float GetSpecialRowsHeight(
            ITriggerLoadoutReader reader,
            int rowCount,
            float cardWidth)
        {
            float height = 0f;
            for (int row = 0; row < rowCount; row++)
            {
                height += GetSpecialRowHeight(cardWidth);
                if (row < rowCount - 1)
                {
                    height += SlotGap;
                }
            }

            return height;
        }

        /// <summary>计算特殊槽位行的固定卡片高度。</summary>
        private static float GetSpecialRowHeight(float cardWidth)
        {
            return GetBaseSlotHeight(cardWidth);
        }

        /// <summary>计算装配示意区两侧槽位列的宽度。</summary>
        private static float GetStageSideWidth(float stageWidth)
        {
            float centerWidth = Mathf.Min(146f, stageWidth * 0.40f);
            return Mathf.Max(82f, (stageWidth - StageInset * 2f - centerWidth - SlotGap * 2f) / 2f);
        }

        /// <summary>计算特殊槽位卡片宽度，并限制其横向长度。</summary>
        private static float GetSpecialCardWidth(float sectionWidth)
        {
            float availableWidth = Mathf.Max(1f, (sectionWidth - 25f) / 2f);
            return Mathf.Min(SpecialSlotMaxWidth, availableWidth);
        }

        /// <summary>计算槽位卡片右侧文字可用宽度。</summary>
        private static float GetSlotTextWidth(float cardWidth)
        {
            return Mathf.Max(1f, cardWidth * SlotTextWidthRatio);
        }

        /// <summary>计算槽位卡片右侧文字区的起始位置。</summary>
        private static float GetSlotTextLeftOffset(float cardWidth)
        {
            return Mathf.Max(1f, cardWidth * SlotTextLeftRatio);
        }

        /// <summary>计算槽位卡片的目标基础高度。</summary>
        private static float GetBaseSlotHeight(float cardWidth)
        {
            return Mathf.Max(SlotTextVerticalPadding * 2f + SlotCategoryHeight + SlotNameHeight,
                cardWidth / SlotCardAspectRatio);
        }

        /// <summary>按卡片宽高比例计算图标边长。</summary>
        private static float GetSlotIconSize(float cardWidth, float cardHeight)
        {
            return Mathf.Max(
                1f,
                Mathf.Min(
                    SlotIconMaxSize,
                    cardWidth * 0.20f,
                    cardHeight * 0.72f));
        }

        /// <summary>按卡片比例及两行名称所需空间计算槽位高度。</summary>
        private static float GetSlotCardHeight(ITriggerSlotState slot, float cardWidth)
        {
            return GetBaseSlotHeight(cardWidth);
        }

        /// <summary>从芯片快照读取当前语言下的主分类名称。</summary>
        private static string BuildSlotCategoryLabel(Thing chip)
        {
            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(chip);
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.CategoryDefName))
            {
                return "BDP_Window_TriggerAssembly_Unknown".Translate().ToString();
            }

            ChipCategoryDef category = DefDatabase<ChipCategoryDef>.GetNamedSilentFail(snapshot.CategoryDefName);
            return category != null
                ? category.LabelCap.ToString()
                : snapshot.CategoryDefName;
        }

        /// <summary>显示完整成品名；功能与构型保持连续命名。</summary>
        private static string BuildSlotChipName(Thing chip)
        {
            if (chip == null) return "BDP_Window_TriggerAssembly_Unknown".Translate().ToString();
            return ChipDisplayNameFormatter.Format(chip.LabelShortCap);
        }

        /// <summary>收集一轮绘制中最长的分类和名称，供所有芯片卡片共用字号。</summary>
        private static void CollectLongestSlotLabels(
            ITriggerLoadoutReader reader,
            TriggerSide side,
            ref string longestCategory,
            ref string longestName)
        {
            if (reader == null)
            {
                return;
            }

            IEnumerable<ITriggerSlotState> slots = reader.GetSlots(side);
            if (slots == null)
            {
                return;
            }

            foreach (ITriggerSlotState slot in slots)
            {
                if (slot == null || slot.LoadedChip == null)
                {
                    continue;
                }

                string category = BuildSlotCategoryLabel(slot.LoadedChip);
                string name = BuildSlotChipName(slot.LoadedChip);
                if (category.Length > longestCategory.Length)
                {
                    longestCategory = category;
                }

                if (name.Length > longestName.Length)
                {
                    longestName = name;
                }
            }
        }

        /// <summary>按最窄卡片和本轮最长文本计算公共字号。</summary>
        private static SlotTextMetrics BuildSlotTextMetrics(
            ITriggerLoadoutReader reader,
            float minimumCardWidth)
        {
            string longestCategory = string.Empty;
            string longestName = string.Empty;
            CollectLongestSlotLabels(reader, TriggerSide.Main, ref longestCategory, ref longestName);
            CollectLongestSlotLabels(reader, TriggerSide.Sub, ref longestCategory, ref longestName);
            CollectLongestSlotLabels(reader, TriggerSide.Special, ref longestCategory, ref longestName);

            float textWidth = GetSlotTextWidth(minimumCardWidth);
            int categoryFontSize = GetCommonSlotFontSize(
                GameFont.Tiny,
                FontStyle.Normal,
                SlotCategoryFontScale,
                textWidth,
                SlotCategoryHeight,
                longestCategory);
            int nameFontSize = GetCommonSlotFontSize(
                GameFont.Small,
                FontStyle.Bold,
                1f,
                textWidth,
                SlotNameHeight,
                longestName,
                true);
            return new SlotTextMetrics(categoryFontSize, nameFontSize);
        }

        /// <summary>在不改变卡片几何的前提下，为本轮所有卡片求同一字号。</summary>
        private static int GetCommonSlotFontSize(
            GameFont font,
            FontStyle fontStyle,
            float preferredScale,
            float maxWidth,
            float maxHeight,
            string longestText,
            bool wordWrap = false)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            try
            {
                Text.Font = font;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = wordWrap;
                GUIStyle labelStyle = new GUIStyle(Text.CurFontStyle)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontStyle = fontStyle,
                    wordWrap = wordWrap,
                    clipping = TextClipping.Clip
                };
                int nativeFontSize = labelStyle.font != null
                    ? labelStyle.font.fontSize
                    : labelStyle.fontSize;
                if (nativeFontSize <= 0)
                {
                    return 0;
                }

                int fontSize = Mathf.Max(
                    1,
                    Mathf.FloorToInt(nativeFontSize * Mathf.Clamp(preferredScale, 0.01f, 1f)));
                labelStyle.fontSize = fontSize;
                GUIContent content = new GUIContent(longestText ?? string.Empty);
                while (fontSize > 1
                    && ((!wordWrap && labelStyle.CalcSize(content).x > maxWidth)
                        || labelStyle.CalcHeight(content, maxWidth) > maxHeight))
                {
                    fontSize--;
                    labelStyle.fontSize = fontSize;
                }

                return fontSize;
            }
            finally
            {
                Text.WordWrap = oldWordWrap;
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
            }
        }

        /// <summary>
        /// 在固定矩形内绘制槽位文字；完整名称允许换行。
        /// 字号由当前面板本轮统一文字度量提供，不按单张卡片独立缩放。
        /// </summary>
        private static void DrawSlotLabel(
            Rect rect,
            string text,
            GameFont font,
            int fontSize,
            Color color,
            TextAnchor anchor,
            FontStyle fontStyle,
            bool wordWrap = false)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = font;
                Text.Anchor = anchor;
                Text.WordWrap = wordWrap;
                GUI.color = color;
                GUIStyle labelStyle = new GUIStyle(Text.CurFontStyle)
                {
                    alignment = anchor,
                    fontStyle = fontStyle,
                    wordWrap = wordWrap,
                    clipping = TextClipping.Clip
                };

                if (fontSize > 0)
                {
                    labelStyle.fontSize = fontSize;
                }

                GUIContent content = new GUIContent(text ?? string.Empty);

                // 槽位文字矩形已经处于滚动内容坐标中；使用原版局部坐标组建立裁剪边界，
                // 子标签从 (0, 0) 开始绘制，避免改变滚动区和其它控件的坐标体系。
                GUI.BeginGroup(rect);
                try
                {
                    GUI.Label(new Rect(0f, 0f, rect.width, rect.height), content, labelStyle);
                }
                finally
                {
                    GUI.EndGroup();
                }
            }
            finally
            {
                GUI.color = oldColor;
                Text.WordWrap = oldWordWrap;
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
            }
        }

        /// <summary>绘制累计芯片预占用轨道；整条轨道始终显示容量分段。</summary>
        private static void DrawTrionCapacity(Rect rect, TriggerAssemblyPreviewSnapshot preview)
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.04f, 0.15f, 0.17f, 0.82f), BorderColor);
            DrawLabel(
                new Rect(rect.x + 10f, rect.y + 7f, rect.width * 0.55f, 19f),
                "BDP_Window_TriggerAssembly_TrionCapacity".Translate().ToString(),
                GameFont.Small,
                new Color(0.86f, 0.94f, 0.92f, 1f),
                TextAnchor.MiddleLeft);
            float reserved = preview != null ? Mathf.Max(0f, preview.PreviewReserved) : 0f;
            float maximum = preview != null ? Mathf.Max(0f, preview.Max) : 0f;
            DrawLabel(
                new Rect(rect.xMax - 156f, rect.y + 7f, 146f, 19f),
                Format(reserved) + " / " + Format(maximum),
                GameFont.Small,
                AccentColor,
                TextAnchor.MiddleRight);

            Rect trackRect = new Rect(rect.x + 10f, rect.y + 32f, rect.width - 20f, 18f);
            Color remainingColor = new Color(0.10f, 0.30f, 0.32f, 0.96f);
            Color reservedColor = new Color(0.29f, 0.78f, 0.80f, 0.96f);
            Widgets.DrawBoxSolidWithOutline(trackRect, remainingColor, BorderColor);
            float ratio = maximum > 0f ? Mathf.Clamp01(reserved / maximum) : 0f;
            float innerWidth = Mathf.Max(0f, trackRect.width - 2f);
            if (innerWidth > 0f && ratio > 0f)
            {
                Widgets.DrawBoxSolid(
                    new Rect(trackRect.x + 1f, trackRect.y + 1f, innerWidth * ratio, trackRect.height - 2f),
                    reservedColor);
            }

            if (ratio > 0f && ratio < 1f)
            {
                float separatorX = trackRect.x + 1f + innerWidth * ratio;
                Widgets.DrawBoxSolid(
                    new Rect(separatorX - 1f, trackRect.y + 1f, 2f, trackRect.height - 2f),
                    new Color(0.82f, 0.96f, 0.90f, 0.95f));
            }
        }

        /// <summary>把可见槽位转换为窗口坐标命中区域。</summary>
        private void AddVisibleHit(Rect localRect, ITriggerSlotState slot, Rect viewportRect)
        {
            Rect screenRect = new Rect(
                viewportRect.x + localRect.x,
                viewportRect.y + localRect.y - scrollPosition.y,
                localRect.width,
                localRect.height);
            float xMin = Mathf.Max(screenRect.x, viewportRect.x);
            float yMin = Mathf.Max(screenRect.y, viewportRect.y);
            float xMax = Mathf.Min(screenRect.xMax, viewportRect.xMax);
            float yMax = Mathf.Min(screenRect.yMax, viewportRect.yMax);
            if (xMax > xMin && yMax > yMin)
            {
                slotHits.Add(new TriggerAssemblySlotHit(new Rect(xMin, yMin, xMax - xMin, yMax - yMin), slot));
            }
        }

        /// <summary>把可见槽位板块转换为窗口坐标区域，供板块空白区交互使用。</summary>
        private void AddVisibleBoardRect(Rect localRect, Rect viewportRect)
        {
            Rect screenRect = new Rect(
                viewportRect.x + localRect.x,
                viewportRect.y + localRect.y - scrollPosition.y,
                localRect.width,
                localRect.height);
            float xMin = Mathf.Max(screenRect.x, viewportRect.x);
            float yMin = Mathf.Max(screenRect.y, viewportRect.y);
            float xMax = Mathf.Min(screenRect.xMax, viewportRect.xMax);
            float yMax = Mathf.Min(screenRect.yMax, viewportRect.yMax);
            if (xMax > xMin && yMax > yMin)
            {
                slotBoardRects.Add(new Rect(xMin, yMin, xMax - xMin, yMax - yMin));
            }
        }

        /// <summary>统计某一侧的槽位数量。</summary>
        private static int CountSlots(ITriggerLoadoutReader reader, TriggerSide side)
        {
            int count = 0;
            if (reader == null)
            {
                return count;
            }

            foreach (ITriggerSlotState slot in reader.GetSlots(side))
            {
                if (slot != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>构建槽位 Tooltip。</summary>
        private static string BuildSlotTooltip(ITriggerSlotState slot, ITriggerLoadoutReader reader)
        {
            ITriggerSlotState rootSlot = ResolveBindingRoot(slot, reader);
            string pairedSlots = rootSlot.HasBindingPartner
                ? "BDP_Window_TriggerAssembly_PairedSlotRelation".Translate(
                    BuildSideLabel(rootSlot.BindingRootSide),
                    rootSlot.BindingRootIndex,
                    BuildSideLabel(rootSlot.BindingPartnerSide),
                    rootSlot.BindingPartnerIndex).ToString()
                : "BDP_None".Translate().ToString();
            return "BDP_Window_TriggerAssembly_SideInfo".Translate(
                BuildSideLabel(slot.Side),
                slot.Index,
                slot.LoadedChip != null
                    ? ChipDisplayNameFormatter.Format(slot.LoadedChip.LabelShortCap)
                    : "BDP_None".Translate().ToString(),
                pairedSlots);
        }

        /// <summary>把槽位侧别转换为玩家可读名称。</summary>
        private static string BuildSideLabel(TriggerSide side)
        {
            switch (side)
            {
                case TriggerSide.Main:
                    return "BDP_Side_MainFull".Translate().ToString();
                case TriggerSide.Sub:
                    return "BDP_Side_SubFull".Translate().ToString();
                case TriggerSide.Special:
                    return "BDP_Side_SpecialFull".Translate().ToString();
                default:
                    return side.ToString();
            }
        }

        /// <summary>格式化资源数值。</summary>
        private static string Format(float value)
        {
            return value.ToString("0.#");
        }

        /// <summary>绘制统一样式的文本并恢复 GUI 状态。</summary>
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

    /// <summary>槽位命中区域。</summary>
    internal sealed class TriggerAssemblySlotHit
    {
        /// <summary>命中区域。</summary>
        internal readonly Rect Rect;

        /// <summary>命中的槽位。</summary>
        internal readonly ITriggerSlotState Slot;

        /// <summary>构造槽位命中区域。</summary>
        internal TriggerAssemblySlotHit(Rect rect, ITriggerSlotState slot)
        {
            Rect = rect;
            Slot = slot;
        }
    }
}
