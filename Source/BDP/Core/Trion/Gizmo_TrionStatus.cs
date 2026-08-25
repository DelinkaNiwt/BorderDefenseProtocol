using System.Collections.Generic;
using System.Text;
using BDP.Core.Trion.External;
using UnityEngine;
using Verse;

namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 资源状态卡。
    /// 仅表达 Trion 自身真值，不直接耦合 CombatBodySession、Trigger、CombatBody。
    /// </summary>
    public sealed class Gizmo_TrionStatus : Gizmo
    {
        /// <summary>
        /// Trion 左侧基础卡片宽度。
        /// 保持紧凑，但比原版标准 160f 更宽，容纳底部说明文本。
        /// </summary>
        private const float BaseCardWidth = 228f;

        /// <summary>
        /// Trion 基础卡片与右侧扩展面板之间的水平间距。
        /// </summary>
        private const float PanelSpacing = 4f;

        /// <summary>
        /// Trion 卡片高度。
        /// 必须与原版 gizmo 按钮高度保持一致。
        /// </summary>
        private const float CardHeight = 75f;

        /// <summary>
        /// 卡片内边距。
        /// </summary>
        private const float InnerPadding = 4f;

        /// <summary>
        /// 标题区高度。
        /// </summary>
        private const float TitleHeight = 21f;

        /// <summary>
        /// 标题与主条之间的垂直间距。
        /// </summary>
        private const float TitleToBarSpacing = 2f;

        /// <summary>
        /// 主资源条高度。
        /// 相比旧实现更粗。
        /// </summary>
        private const float BarHeight = 24f;

        /// <summary>
        /// 主资源条与底部信息区之间的垂直间距。
        /// </summary>
        private const float BarToBottomSpacing = 2f;

        /// <summary>
        /// 底部信息区高度。
        /// </summary>
        private const float BottomHeight = 18f;

        /// <summary>
        /// 底部说明文本与徽标区之间的水平间距。
        /// </summary>
        private const float TopBadgeSpacing = 4f;

        /// <summary>
        /// 底部左右信息组之间的间距。
        /// </summary>
        private const float BottomGroupSpacing = 6f;

        /// <summary>
        /// 底部速率文本与冻结图标之间的间距。
        /// </summary>
        private const float FrozenRateSpacing = 3f;

        /// <summary>
        /// 徽标尺寸。
        /// </summary>
        private const float BadgeSize = 14f;

        /// <summary>
        /// 徽标之间的水平间距。
        /// </summary>
        private const float BadgeSpacing = 3f;

        /// <summary>
        /// 可见徽标最大数量。
        /// 超过部分不再继续加入卡片。
        /// </summary>
        private const int MaxVisibleBadges = 4;

        /// <summary>
        /// 分隔线单段长度。
        /// 只画上段与下段，中间留空。
        /// </summary>
        private const float DividerInset = 2f;

        /// <summary>
        /// 分隔刻痕宽度。
        /// </summary>
        private const float DividerWidth = 1f;

        /// <summary>
        /// 正式锁定区段颜色。
        /// </summary>
        private static readonly Color AllocatedColor = new Color(0.19f, 0.47f, 0.78f);

        /// <summary>
        /// 当前可用区段颜色。
        /// </summary>
        private static readonly Color AvailableColor = new Color(0.27f, 0.84f, 0.92f);

        /// <summary>
        /// 未激活时的预测锁定分隔颜色。
        /// </summary>
        private static readonly Color ReservedDividerColor = new Color(1f, 0.93f, 0.42f);

        /// <summary>
        /// 已激活时的正式锁定分隔颜色。
        /// </summary>
        private static readonly Color AllocatedDividerColor = new Color(0.92f, 0.97f, 1f);

        /// <summary>
        /// 资源条背景颜色。
        /// </summary>
        private static readonly Color BarBackgroundColor = new Color(0.08f, 0.10f, 0.13f);

        /// <summary>
        /// 资源条边框颜色。
        /// </summary>
        private static readonly Color BorderColor = new Color(0.32f, 0.36f, 0.42f);

        /// <summary>
        /// 徽标槽背景颜色。
        /// </summary>
        private static readonly Color BadgeSlotColor = new Color(0.11f, 0.13f, 0.16f);

        /// <summary>
        /// 徽标槽边框颜色。
        /// </summary>
        private static readonly Color BadgeBorderColor = new Color(0.25f, 0.28f, 0.33f);

        /// <summary>
        /// 恢复冻结徽标高亮色。
        /// </summary>
        private static readonly Color FrozenBadgeColor = new Color(0.38f, 0.75f, 1f);

        /// <summary>
        /// 恢复未冻结时的灰暗徽标色。
        /// </summary>
        private static readonly Color FrozenBadgeDimColor = new Color(0.45f, 0.50f, 0.58f);

        /// <summary>
        /// 左侧 Trion 区与右侧扩展面板之间的柔和分隔线颜色。
        /// </summary>
        private static readonly Color PanelDividerColor = new Color(0.18f, 0.20f, 0.24f, 0.92f);

        /// <summary>
        /// 当前宿主对象。
        /// </summary>
        private readonly Thing owner;

        /// <summary>
        /// 当前 Trion 正式只读口。
        /// </summary>
        private readonly ITrionReader reader;

        /// <summary>
        /// 初始化 Trion 状态卡。
        /// </summary>
        public Gizmo_TrionStatus(Thing owner, ITrionReader reader)
        {
            this.owner = owner;
            this.reader = reader;
            Order = -100f;
        }

        /// <summary>
        /// 返回卡片宽度，不拉满整行。
        /// 有右侧面板时，外层宽度由 Trion 基础区和面板区共同决定。
        /// </summary>
        public override float GetWidth(float maxWidth)
        {
            TrionGizmoExtensionContext context = new TrionGizmoExtensionContext(
                owner,
                reader,
                new Rect(0f, 0f, BaseCardWidth, CardHeight));

            float panelWidth;
            ResolvePanelExtension(context, out panelWidth);
            return panelWidth > 0f ? BaseCardWidth + PanelSpacing + panelWidth : BaseCardWidth;
        }

        /// <summary>
        /// 绘制 Trion 状态卡。
        /// </summary>
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            List<TrionGizmoExtensionBadge> badges = CollectBadges();
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), CardHeight);
            Rect baseRect = new Rect(topLeft.x, topLeft.y, BaseCardWidth, CardHeight);
            Rect baseInnerRect = baseRect.ContractedBy(InnerPadding);
            Rect titleRect = new Rect(baseInnerRect.x, baseInnerRect.y, baseInnerRect.width, TitleHeight);
            Rect barRect = new Rect(baseInnerRect.x, titleRect.yMax + TitleToBarSpacing, baseInnerRect.width, BarHeight);
            Rect bottomRect = new Rect(baseInnerRect.x, barRect.yMax + BarToBottomSpacing, baseInnerRect.width, BottomHeight);

            TrionGizmoExtensionContext context = new TrionGizmoExtensionContext(owner, reader, baseRect);
            float panelWidth;
            ITrionGizmoPanelExtensionProvider panelProvider = ResolvePanelExtension(context, out panelWidth);
            Rect panelRect = panelProvider != null && panelWidth > 0f
                ? new Rect(baseRect.xMax + PanelSpacing, topLeft.y, panelWidth, CardHeight)
                : Rect.zero;

            Widgets.DrawWindowBackground(outerRect);
            DrawTitleRow(titleRect, badges);
            DrawBar(barRect);
            DrawBottomRow(bottomRect);
            DrawPanelDivider(baseRect, panelRect);

            GizmoResult panelResult = DrawPanelExtension(panelProvider, context, panelRect, parms);

            if (Mouse.IsOver(baseRect))
            {
                Widgets.DrawHighlight(baseRect);
                TooltipHandler.TipRegion(baseRect, new TipSignal(BuildTooltip(), BuildTooltipId(1937421)));
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            return panelResult.State == GizmoState.Interacted
                ? panelResult
                : new GizmoResult(GizmoState.Clear);
        }

        /// <summary>
        /// 绘制 Trion 基础区与右侧扩展面板之间的轻量分隔。
        /// 这里不画完整面板框，避免右侧内容看起来像独立小窗。
        /// </summary>
        private void DrawPanelDivider(Rect baseRect, Rect panelRect)
        {
            if (panelRect.width <= 0f)
            {
                return;
            }

            float x = Mathf.Round(baseRect.xMax + PanelSpacing * 0.5f);
            Widgets.DrawBoxSolid(
                new Rect(x, baseRect.y + 7f, 1f, Mathf.Max(0f, baseRect.height - 14f)),
                PanelDividerColor);
        }

        /// <summary>
        /// 解析当前第一个有效右侧面板扩展。
        /// 第一版只消费第一个返回正宽度的面板，避免多个大面板挤占 Gizmo 区域。
        /// </summary>
        private ITrionGizmoPanelExtensionProvider ResolvePanelExtension(
            TrionGizmoExtensionContext context,
            out float panelWidth)
        {
            panelWidth = 0f;
            foreach (ITrionGizmoPanelExtensionProvider provider in TrionGizmoExtensionRegistry.GetPanelProviders())
            {
                if (provider == null)
                {
                    continue;
                }

                float requestedWidth = Mathf.Max(0f, provider.GetWidth(context));
                if (requestedWidth <= 0f)
                {
                    continue;
                }

                panelWidth = requestedWidth;
                return provider;
            }

            return null;
        }

        /// <summary>
        /// 绘制右侧面板扩展。
        /// </summary>
        private GizmoResult DrawPanelExtension(
            ITrionGizmoPanelExtensionProvider provider,
            TrionGizmoExtensionContext context,
            Rect panelRect,
            GizmoRenderParms parms)
        {
            if (provider == null || panelRect.width <= 0f)
            {
                return new GizmoResult(GizmoState.Clear);
            }

            TrionGizmoExtensionContext panelContext = new TrionGizmoExtensionContext(owner, reader, panelRect);
            return provider.DrawPanel(panelContext, panelRect, parms);
        }

        /// <summary>
        /// 绘制标题行。
        /// 左侧保留标题，右侧仅保留扩展状态图标。
        /// </summary>
        private void DrawTitleRow(Rect titleRect, List<TrionGizmoExtensionBadge> badges)
        {
            float badgeAreaWidth = GetBadgeRowWidth(badges.Count);
            Rect badgeRect = badgeAreaWidth > 0f
                ? new Rect(
                    titleRect.xMax - badgeAreaWidth,
                    titleRect.y + (titleRect.height - BadgeSize) * 0.5f,
                    badgeAreaWidth,
                    BadgeSize)
                : Rect.zero;
            float rightCursor = badgeAreaWidth > 0f ? badgeRect.x - TopBadgeSpacing : titleRect.xMax;

            Text.Font = GameFont.Small;
            Rect titleLabelRect = new Rect(titleRect.x, titleRect.y + 1f, Mathf.Max(0f, rightCursor - titleRect.x), titleRect.height - 1f);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleLabelRect, "BDP_Trion_Status_Title".Translate());

            if (badgeAreaWidth > 0f)
            {
                DrawBadgeRow(badgeRect, badges);
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// 绘制底部行。
        /// 左侧显示可用量，右侧显示恢复/消耗与冻结图标。
        /// </summary>
        private void DrawBottomRow(Rect bottomRect)
        {
            Rect frozenRect = new Rect(
                bottomRect.xMax - BadgeSize,
                bottomRect.y + (bottomRect.height - BadgeSize) * 0.5f,
                BadgeSize,
                BadgeSize);
            Rect rateRect = new Rect(
                bottomRect.x,
                bottomRect.y + 1f,
                Mathf.Max(0f, frozenRect.x - FrozenRateSpacing - bottomRect.x),
                bottomRect.height - 1f);

            float rateTextWidth = Mathf.Min(Text.CalcSize(BuildRateText()).x + 8f, rateRect.width);
            Rect rightRateRect = new Rect(
                rateRect.xMax - rateTextWidth,
                rateRect.y,
                rateTextWidth,
                rateRect.height);
            Rect leftInfoRect = new Rect(
                bottomRect.x,
                bottomRect.y + 1f,
                Mathf.Max(0f, rightRateRect.x - BottomGroupSpacing - bottomRect.x),
                bottomRect.height - 1f);

            DrawBottomInfo(leftInfoRect);
            DrawRateText(rightRateRect);
            DrawBadge(frozenRect, CreateFrozenBadge(), -1);
        }

        /// <summary>
        /// 绘制底部右侧速率文本。
        /// 与冻结图标属于同一组信息。
        /// </summary>
        private void DrawRateText(Rect rateRect)
        {
            if (rateRect.width <= 0f)
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rateRect, BuildRateText().Truncate(rateRect.width));
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// 绘制底部左侧说明文本。
        /// </summary>
        private void DrawBottomInfo(Rect infoRect)
        {
            if (infoRect.width <= 0f)
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(infoRect, BuildBottomInfoText().Truncate(infoRect.width));
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// 构建底部说明文本。
        /// 所有可见数值统一保留一位小数。
        /// </summary>
        private string BuildBottomInfoText()
        {
            return "BDP_Trion_Status_Available".Translate(
                reader.Available.ToString("F1"),
                reader.Max.ToString("F1"));
        }

        /// <summary>
        /// 构建顶部右侧速率文案。
        /// 存在正式持续消耗时显示消耗，否则显示自然恢复。
        /// </summary>
        private string BuildRateText()
        {
            if (reader.TotalDrainPerSecond > 0f)
            {
                return "BDP_Trion_Status_Drain".Translate(
                    reader.TotalDrainPerSecond.ToString("F1"));
            }

            return "BDP_Trion_Status_Recovery".Translate(
                reader.RecoveryPerDay.ToString("F1"));
        }

        /// <summary>
        /// 绘制资源条。
        /// 未激活时显示预测锁定边界；已激活时显示正式锁定段与可用段。
        /// </summary>
        private void DrawBar(Rect barRect)
        {
            Widgets.DrawBoxSolid(barRect, BorderColor);

            Rect fillRect = barRect.ContractedBy(1f);
            Widgets.DrawBoxSolid(fillRect, BarBackgroundColor);

            float max = Mathf.Max(0.01f, reader.Max);
            float curWidth = fillRect.width * Mathf.Clamp01(reader.Cur / max);
            float allocatedWidth = fillRect.width * Mathf.Clamp01(reader.Allocated / max);
            float predictedBoundaryWidth = fillRect.width * Mathf.Clamp01(reader.Reserved / max);

            if (reader.Allocated > 0f)
            {
                float visibleAllocatedWidth = Mathf.Min(curWidth, allocatedWidth);
                if (visibleAllocatedWidth > 0f)
                {
                    Rect allocatedRect = new Rect(fillRect.x, fillRect.y, visibleAllocatedWidth, fillRect.height);
                    Widgets.DrawBoxSolid(allocatedRect, AllocatedColor);
                }

                if (curWidth > visibleAllocatedWidth)
                {
                    Rect availableRect = new Rect(fillRect.x + visibleAllocatedWidth, fillRect.y, curWidth - visibleAllocatedWidth, fillRect.height);
                    Widgets.DrawBoxSolid(availableRect, AvailableColor);
                }

                DrawSoftDivider(fillRect, Mathf.Max(visibleAllocatedWidth, 0f), AllocatedDividerColor);
                return;
            }

            if (curWidth > 0f)
            {
                Rect availableRect = new Rect(fillRect.x, fillRect.y, curWidth, fillRect.height);
                Widgets.DrawBoxSolid(availableRect, AvailableColor);
            }

            if (reader.Reserved > 0f)
            {
                DrawSoftDivider(fillRect, predictedBoundaryWidth, ReservedDividerColor);
            }
        }

        /// <summary>
        /// 绘制更保守的细分界。
        /// 仅在中段画一条细暗分隔，避免高亮刻痕破坏主条观感。
        /// </summary>
        private void DrawSoftDivider(Rect fillRect, float width, Color color)
        {
            float dividerX = Mathf.Clamp(Mathf.Round(fillRect.x + width), fillRect.x, fillRect.xMax - 1f);
            Color oldColor = GUI.color;
            GUI.color = color;
            Widgets.DrawBoxSolid(
                new Rect(dividerX, fillRect.y + DividerInset, DividerWidth, Mathf.Max(0f, fillRect.height - DividerInset * 2f)),
                color);
            GUI.color = oldColor;
        }

        /// <summary>
        /// 收集当前可见徽标。
        /// 先加入 Trion 自身状态，再加入外部系统扩展徽标。
        /// </summary>
        private List<TrionGizmoExtensionBadge> CollectBadges()
        {
            List<TrionGizmoExtensionBadge> badges = new List<TrionGizmoExtensionBadge>();

            Rect dummyRect = new Rect(0f, 0f, BaseCardWidth, BottomHeight);
            TrionGizmoExtensionContext context = new TrionGizmoExtensionContext(owner, reader, dummyRect);

            foreach (TrionGizmoExtensionBadge badge in TrionGizmoExtensionRegistry.GetBadges(context))
            {
                if (badge == null)
                {
                    continue;
                }

                badges.Add(badge);
                if (badges.Count >= MaxVisibleBadges)
                {
                    break;
                }
            }

            return badges;
        }

        /// <summary>
        /// 构建恢复冻结徽标。
        /// </summary>
        private TrionGizmoExtensionBadge CreateFrozenBadge()
        {
            return new TrionGizmoExtensionBadge(
                icon: null,
                tooltip: reader.Frozen
                    ? "BDP_Trion_Status_FrozenOn".Translate()
                    : "BDP_Trion_Status_FrozenOff".Translate(),
                tint: reader.Frozen ? FrozenBadgeColor : FrozenBadgeDimColor,
                glyphKey: "frozen");
        }

        /// <summary>
        /// 计算徽标行宽度。
        /// </summary>
        private float GetBadgeRowWidth(int badgeCount)
        {
            int clampedCount = Mathf.Min(badgeCount, MaxVisibleBadges);
            if (clampedCount <= 0)
            {
                return 0f;
            }

            return clampedCount * BadgeSize + (clampedCount - 1) * BadgeSpacing;
        }

        /// <summary>
        /// 绘制底部右侧徽标行。
        /// </summary>
        private void DrawBadgeRow(Rect badgeRowRect, List<TrionGizmoExtensionBadge> badges)
        {
            int badgeCount = Mathf.Min(badges.Count, MaxVisibleBadges);
            float cursorX = badgeRowRect.xMax - BadgeSize;

            for (int i = 0; i < badgeCount; i++)
            {
                Rect badgeRect = new Rect(cursorX, badgeRowRect.y, BadgeSize, BadgeSize);
                DrawBadge(badgeRect, badges[i], i);
                cursorX -= BadgeSize + BadgeSpacing;
            }
        }

        /// <summary>
        /// 绘制单个徽标。
        /// </summary>
        private void DrawBadge(Rect badgeRect, TrionGizmoExtensionBadge badge, int index)
        {
            Widgets.DrawBoxSolid(badgeRect, BadgeBorderColor);

            Rect innerRect = badgeRect.ContractedBy(1f);
            Widgets.DrawBoxSolid(innerRect, BadgeSlotColor);

            Rect glyphRect = innerRect.ContractedBy(2f);
            if (badge.Icon != null)
            {
                Color oldColor = GUI.color;
                GUI.color = badge.Tint;
                GUI.DrawTexture(glyphRect, badge.Icon);
                GUI.color = oldColor;
            }
            else
            {
                DrawBadgeGlyph(glyphRect, badge.GlyphKey ?? badge.Text, badge.Tint);
            }

            if (!string.IsNullOrEmpty(badge.Tooltip))
            {
                TooltipHandler.TipRegion(badgeRect, new TipSignal(badge.Tooltip, BuildTooltipId(8500 + index)));
            }
        }

        /// <summary>
        /// 按徽标类型绘制示意图形。
        /// </summary>
        private void DrawBadgeGlyph(Rect glyphRect, string glyphKey, Color tint)
        {
            switch (glyphKey)
            {
                case "combatbody":
                    DrawCombatBodyGlyph(glyphRect, tint);
                    return;
                case "frozen":
                    DrawFrozenGlyph(glyphRect, tint);
                    return;
                default:
                    DrawDefaultGlyph(glyphRect, tint);
                    return;
            }
        }

        /// <summary>
        /// 绘制战斗体状态图形。
        /// 统一图形，用颜色高亮/灰暗表达不同状态。
        /// </summary>
        private void DrawCombatBodyGlyph(Rect glyphRect, Color tint)
        {
            float centerX = Mathf.Round(glyphRect.x + glyphRect.width * 0.5f - 1f);
            Widgets.DrawBoxSolid(new Rect(centerX, glyphRect.y + 1f, 2f, glyphRect.height - 2f), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.x + 1f, glyphRect.y + 1f, glyphRect.width - 2f, 2f), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.x + 2f, glyphRect.yMax - 3f, glyphRect.width - 4f, 2f), tint);
        }

        /// <summary>
        /// 绘制恢复冻结图形。
        /// 采用“十字 + 对角点”的雪花示意。
        /// </summary>
        private void DrawFrozenGlyph(Rect glyphRect, Color tint)
        {
            float centerX = Mathf.Round(glyphRect.x + glyphRect.width * 0.5f - 1f);
            float centerY = Mathf.Round(glyphRect.y + glyphRect.height * 0.5f - 1f);
            Widgets.DrawBoxSolid(new Rect(centerX, glyphRect.y, 2f, glyphRect.height), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.x, centerY, glyphRect.width, 2f), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.x + 1f, glyphRect.y + 1f, 2f, 2f), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.xMax - 3f, glyphRect.y + 1f, 2f, 2f), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.x + 1f, glyphRect.yMax - 3f, 2f, 2f), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.xMax - 3f, glyphRect.yMax - 3f, 2f, 2f), tint);
        }

        /// <summary>
        /// 绘制默认图形。
        /// 用于未知徽标类型的兜底表现。
        /// </summary>
        private void DrawDefaultGlyph(Rect glyphRect, Color tint)
        {
            Widgets.DrawBoxSolid(new Rect(glyphRect.x + 1f, glyphRect.y + 1f, glyphRect.width - 2f, 2f), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.x + 1f, glyphRect.yMax - 3f, glyphRect.width - 2f, 2f), tint);
            Widgets.DrawBoxSolid(new Rect(glyphRect.x + glyphRect.width * 0.5f - 1f, glyphRect.y + 2f, 2f, glyphRect.height - 4f), tint);
        }

        /// <summary>
        /// 构建悬浮提示。
        /// </summary>
        private string BuildTooltip()
        {
            IReadOnlyDictionary<TrionDrainKey, float> snapshot = reader.GetDrainSnapshot();
            if (snapshot == null || snapshot.Count == 0)
            {
                return "BDP_Trion_Status_DrainDetailEmpty".Translate();
            }

            StringBuilder builder = new StringBuilder("BDP_Trion_Status_DrainDetailHeader".Translate().ToString());
            bool hasPositiveDrain = false;

            foreach (KeyValuePair<TrionDrainKey, float> pair in snapshot)
            {
                if (pair.Value <= 0f)
                {
                    continue;
                }

                hasPositiveDrain = true;
                builder.Append("\n");
                builder.Append(BuildDrainSourceLine(pair.Key, pair.Value));
            }

            return hasPositiveDrain
                ? builder.ToString()
                : "BDP_Trion_Status_DrainDetailEmpty".Translate().ToString();
        }

        /// <summary>
        /// 构建单条持续流失来源说明。
        /// 暂时逐条列清，方便排查 drain 注册、注销和数值问题。
        /// </summary>
        private static string BuildDrainSourceLine(TrionDrainKey key, float perSecond)
        {
            return "BDP_Trion_Status_DrainLine".Translate(
                ResolveDrainSourceLabel(key),
                perSecond.ToString("F1"));
        }

        /// <summary>
        /// 把 Trion 账本键翻译成玩家可读的中文来源说明。
        /// key 原始字段只作为调试 ID 附带显示，不直接当作主文案。
        /// </summary>
        private static string ResolveDrainSourceLabel(TrionDrainKey key)
        {
            if (key.Domain == "CombatBody" && key.Channel == "Wound")
            {
                return "BDP_Trion_DrainSource_CombatBodyWound".Translate(key.Tag).ToString();
            }

            if (key.Domain == "CombatBody" && key.Channel == "Maintenance")
            {
                return "BDP_Trion_DrainSource_CombatBodyMaintenance".Translate();
            }

            if (key.Domain == "Expression")
            {
                return ResolveExpressionDrainSourceLabel(key);
            }

            return "BDP_Trion_DrainSource_Unknown".Translate(key.ToString()).ToString();
        }

        /// <summary>
        /// 构建最终表达持续消耗来源说明。
        /// </summary>
        private static string ResolveExpressionDrainSourceLabel(TrionDrainKey key)
        {
            switch (key.Channel)
            {
                case "Hediff":
                    return "BDP_Trion_DrainSource_Hediff".Translate(key.Tag).ToString();
                case "Ability":
                    return "BDP_Trion_DrainSource_Ability".Translate(key.Tag).ToString();
                case "Passive":
                    return "BDP_Trion_DrainSource_Passive".Translate(key.Tag).ToString();
                case "Verb":
                    return "BDP_Trion_DrainSource_Verb".Translate(key.Tag).ToString();
                default:
                    return "BDP_Trion_DrainSource_Expression".Translate(key.Tag).ToString();
            }
        }

        /// <summary>
        /// 构建跨帧稳定的 tooltip 唯一 ID。
        /// Gizmo 本身可能每帧重建，不能用 Gizmo 实例哈希作为 tooltip 计时身份。
        /// </summary>
        private int BuildTooltipId(int salt)
        {
            int ownerId = owner != null ? owner.thingIDNumber : 0;
            return Gen.HashCombineInt(ownerId, salt);
        }
    }
}
