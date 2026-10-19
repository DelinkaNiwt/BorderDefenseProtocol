using System;
using System.Collections.Generic;
using System.Globalization;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Core.Chips;
using BDP.Core.Combos;
using BDP.Core.Expressions;
using BDP.Content.Information;
using BDP.Content.Information.Combos;
using BDP.Content.Information.Features.Rendering;
using BDP.Core.Information;
using BDP.Core.UI.Charts;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配台芯片详情渲染器。
    /// 正式功能区域直接读取统一信息节点，不按技术块名称猜测功能。
    /// </summary>
    internal static partial class ChipInformationDetailRenderer
    {
        /// <summary>攻击图表在当前可用宽度下的统一排版结果。</summary>
        private struct AttackChartLayout
        {
            /// <summary>雷达图视口宽度。</summary>
            public float RadarWidth;

            /// <summary>参数卡片区域宽度。</summary>
            public float StatsWidth;

            /// <summary>参数卡片是否与雷达图并排。</summary>
            public bool StatsBesideRadar;
        }

        /// <summary>语义功能展示器注册表。</summary>
        private static readonly ChipFeatureRendererRegistry FeatureRendererRegistry =
            ChipFeatureRendererRegistry.CreateDefault();

        /// <summary>当前详情面板中被用户展开的 ComboDef 声明语义行；未加入集合的行默认折叠。</summary>
        private static readonly HashSet<string> ExpandedDeclaredComboSemantics =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>组合声明缓存当前结果节点及其宿主准入状态。</summary>
        private static readonly Dictionary<string, DeclaredComboProjection> DeclaredComboProjections =
            new Dictionary<string, DeclaredComboProjection>(StringComparer.Ordinal);

        /// <summary>组合节点缓存所属的成品比较上下文。</summary>
        private static ChipInformationComparisonModel DeclaredComboOwner;

        /// <summary>组合技图表使用的当前结果节点、宿主比较模型和准入结果。</summary>
        private sealed class DeclaredComboProjection
        {
            /// <summary>应用组合构型修正后的功能图表节点。</summary>
            internal readonly IReadOnlyList<ChipInformationNode> CurrentFeatureNodes;

            /// <summary>仅供组合图表读取宿主功能结果的比较模型。</summary>
            internal readonly ChipInformationComparisonModel Comparison;

            /// <summary>当前展示宿主是否满足组合定义的准入条件。</summary>
            internal readonly bool IsAdmitted;

            /// <summary>创建单个组合定义的图表投影。</summary>
            internal DeclaredComboProjection(
                IReadOnlyList<ChipInformationNode> currentFeatureNodes,
                ChipInformationComparisonModel comparison,
                bool isAdmitted)
            {
                CurrentFeatureNodes = currentFeatureNodes
                    ?? new List<ChipInformationNode>().AsReadOnly();
                Comparison = comparison;
                IsAdmitted = isAdmitted;
            }
        }

        /// <summary>卡片间距。</summary>
        private const float CardGap = 8f;

        /// <summary>统计图表组标题高度。</summary>
        private const float StatisticsHeaderHeight = 23f;

        /// <summary>雷达卡片高度；去除标题后保留原绘图区尺寸。</summary>
        private const float RadarChartHeight = 244f;

        /// <summary>旧版雷达绘图区高度。</summary>
        private const float RadarPlotHeight = 190f;

        /// <summary>雷达主体之外供上下轴标签使用的额外高度。</summary>
        private const float RadarBottomPadding = RadarChartHeight - RadarPlotHeight;

        /// <summary>旧版雷达图绘图区宽度；与左侧栏同宽，避免两侧额外留白。</summary>
        private const float RadarChartColumnWidth = 238f;

        /// <summary>雷达图与右侧参数卡片之间的间距。</summary>
        private const float RadarStatsColumnGap = 6f;

        /// <summary>参数纵列的最小可读宽度，容纳原生中文小字号的配对标签。</summary>
        private const float MinimumAttackStatsWidth = 120f;

        /// <summary>雷达下方参数卡片与图表之间的间距。</summary>
        private const float RadarStatsBelowGap = 6f;

        /// <summary>参数纵列的卡片间距。</summary>
        private const float AttackStatTileGap = 4f;

        /// <summary>紧凑参数卡片的最小高度。</summary>
        private const float AttackStatTileMinimumHeight = 42f;

        /// <summary>精度卡片总高度，含标题和底部留白；四锚点曲线不需要雷达图同等纵向空间。</summary>
        private const float AccuracyChartHeight = 165f;

        /// <summary>精度绘图区高度。</summary>
        private const float AccuracyPlotHeight = 134f;

        /// <summary>精度卡片标题到绘图区的距离，对齐统一图表标题区高度。</summary>
        private const float AccuracyTitleHeight = 23f;

        /// <summary>范围示意图高度。</summary>
        private const float RangeDiagramHeight = 112f;

        /// <summary>技能距离短弧图卡片高度；弧区约 30px，下方一行数值标签。</summary>
        private const float CastRangeArcHeight = 48f;

        /// <summary>距离短弧图中轴所在高度，原点与所有弧顶点都在这条线上。</summary>
        private const float CastRangeArcAxisY = 17f;

        /// <summary>距离短弧图左侧原点（使用者）的横坐标。</summary>
        private const float CastRangeArcOriginX = 14f;

        /// <summary>距离短弧图标签宽度；最大弧右侧需预留半个标签宽度。</summary>
        private const float CastRangeArcLabelWidth = 60f;

        /// <summary>护盾角度图沿用旧版方形图框尺寸。</summary>
        private const float ShieldCoverageInset = 9f;

        /// <summary>护盾角度图沿用旧版区块标题后的垂直间距。</summary>
        private const float ShieldCoverageTopInset = 10f;

        /// <summary>护盾角度图绘图区底部的旧版留白。</summary>
        private const float ShieldCoverageBottomPadding = 5f;

        /// <summary>旧版图表当前值青色。</summary>
        private static readonly Color AccentColor = new Color(0.30f, 0.84f, 0.82f, 1f);

        /// <summary>旧版图表基准线和次要文字颜色。</summary>
        private static readonly Color BaseColor = new Color(0.43f, 0.61f, 0.63f, 0.9f);

        /// <summary>旧版图表比较变化强调色。</summary>
        private static readonly Color IncreaseColor = new Color(1f, 0.70f, 0.30f, 1f);

        /// <summary>旧版图表坐标文字颜色。</summary>
        private static readonly Color DecreaseColor = new Color(0.42f, 0.53f, 0.55f, 1f);

        /// <summary>旧版雷达图超量程警示色。</summary>
        private static readonly Color WarningColor = new Color(1f, 0.40f, 0.25f, 1f);

        /// <summary>旧版图表卡片底色。</summary>
        private static readonly Color ChartBackgroundColor = new Color(0.03f, 0.09f, 0.11f, 0.95f);

        /// <summary>旧版图表卡片边框色。</summary>
        private static readonly Color ChartBorderColor = new Color(0.32f, 0.50f, 0.53f, 0.72f);

        /// <summary>旧版图表网格色。</summary>
        private static readonly Color ChartGridColor = new Color(0.29f, 0.59f, 0.61f, 0.50f);

        /// <summary>雷达参数名称与正文同等重要，使用明亮浅灰，独立于基准线颜色。</summary>
        private static readonly Color RadarLabelColor = new Color(0.80f, 0.88f, 0.87f, 1f);

        /// <summary>身份区图标行高度；同时决定芯片图标边长。</summary>
        private const float IdentityRowHeight = 44f;

        /// <summary>嵌套层级引导线的固定横向缩进。</summary>
        private const float NestingGuideIndent = 12f;

        /// <summary>嵌套层级引导线的线宽。</summary>
        private const float NestingGuideWidth = 2f;

        /// <summary>语义功能左侧色标的固定宽度。</summary>
        private const float FeatureAccentBarWidth = 3f;

        /// <summary>各语义功能的强调色；未登记的功能沿用通用青色。</summary>
        private static readonly Dictionary<string, Color> FeatureAccentColors =
            new Dictionary<string, Color>(StringComparer.Ordinal)
            {
                { "BDP_Feature_RangedAttack", new Color(0.30f, 0.84f, 0.82f, 1f) },
                { "BDP_Feature_MeleeAttack", new Color(0.85f, 0.55f, 0.25f, 1f) },
                { "BDP_Feature_Shield", new Color(0.42f, 0.58f, 0.92f, 1f) },
                { "BDP_Feature_Movement", new Color(0.45f, 0.80f, 0.45f, 1f) },
                { "BDP_Feature_GenericAbility", new Color(0.72f, 0.48f, 0.85f, 1f) }
            };

        /// <summary>按功能定义键查找强调色；未登记的功能沿用远程攻击的通用青色。</summary>
        private static Color GetFeatureAccentColor(string featureDefinitionKey)
        {
            Color accent;
            return !string.IsNullOrWhiteSpace(featureDefinitionKey)
                && FeatureAccentColors.TryGetValue(featureDefinitionKey, out accent)
                ? accent
                : FeatureAccentColors["BDP_Feature_RangedAttack"];
        }

        /// <summary>芯片详情身份区图标来源；装配台优先使用真实实例，制造台草稿回退到成品定义。</summary>
        internal sealed class ChipDetailIconSource
        {
            /// <summary>装配台等场景下的真实芯片实例；不存在时为空。</summary>
            internal Thing Chip { get; }

            /// <summary>制造台草稿等无实例场景下的回退成品定义；不存在时为空。</summary>
            internal ThingDef FallbackDef { get; }

            /// <summary>按真实芯片实例创建图标来源。</summary>
            internal ChipDetailIconSource(Thing chip)
            {
                Chip = chip;
            }

            /// <summary>按回退成品定义创建图标来源。</summary>
            internal ChipDetailIconSource(ThingDef fallbackDef)
            {
                FallbackDef = fallbackDef;
            }
        }

        /// <summary>绘制芯片详情。</summary>
        internal static void DrawContent(
            Rect contentRect,
            string chipLabel,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect,
            ChipDetailIconSource iconSource = null)
        {
            ChipInformationModel model = comparison?.Current;
            if (model == null)
            {
                return;
            }

            IReadOnlyList<ChipInformationNode> contexts = ReadDirectContexts(model.Root);
            IReadOnlyList<ChipInformationNode> relations = ReadDeclaredRelations(model.Root);
            IReadOnlyList<ChipInformationNode> informationNodes =
                ChipInformationNodeQuery.ReadInformationNodes(model.Root);
            string sourceProfessionKey = GetSourceProfessionKey(comparison);
            float cursorY = contentRect.y;
            DrawGeneralIdentity(
                new Rect(contentRect.x, cursorY, contentRect.width, MeasureIdentityHeight(chipLabel, model.Root, contentRect.width)),
                chipLabel,
                iconSource,
                model.Root);
            cursorY += MeasureIdentityHeight(chipLabel, model.Root, contentRect.width) + CardGap;

            float overviewHeight = MeasureGeneralBlocks(
                model.Root,
                informationNodes,
                contentRect.width);
            if (overviewHeight > 0f)
            {
                DrawGeneralBlocks(
                    new Rect(contentRect.x, cursorY, contentRect.width, overviewHeight),
                    model.Root,
                    informationNodes);
                cursorY += overviewHeight + CardGap;
            }

            IReadOnlyList<ChipInformationNode> rootFeatures =
                ChipInformationNodeQuery.ReadFeatures(model.Root);
            IReadOnlyList<ChipFeatureRenderResult> renderedRootFeatures = FeatureRendererRegistry.RenderAll(
                rootFeatures,
                null);
            if (renderedRootFeatures.Count > 0)
            {
                float featureHeight = MeasureFeatureSection(
                    renderedRootFeatures,
                    contentRect.width,
                    sourceProfessionKey,
                    comparison);
                DrawFeatureSection(
                    new Rect(contentRect.x, cursorY, contentRect.width, featureHeight),
                    renderedRootFeatures,
                    contentRect.width,
                    comparison,
                    visibleContentRect);
                cursorY += featureHeight + CardGap;
            }

            if (contexts.Count > 0)
            {
                float contextHeight = MeasureContexts(
                    contexts,
                    contentRect.width,
                    comparison,
                    model.Root?.Key);
                DrawContexts(
                    new Rect(contentRect.x, cursorY, contentRect.width, contextHeight),
                    contexts,
                    contentRect.width,
                    comparison,
                    visibleContentRect,
                    model.Root?.Key);
                cursorY += contextHeight + CardGap;
            }

            if (relations.Count > 0)
            {
                float relationHeight = MeasureRelationSection(
                    relations,
                    contentRect.width,
                    comparison,
                    model.Root?.Key);
                DrawRelationSection(
                    new Rect(contentRect.x, cursorY, contentRect.width, relationHeight),
                    relations,
                    contentRect.width,
                    comparison,
                    visibleContentRect,
                    model.Root?.Key);
            }
        }

        /// <summary>测量详情内容高度。</summary>
        internal static float MeasureContentHeight(
            string chipLabel,
            ChipInformationComparisonModel comparison,
            float width)
        {
            ChipInformationModel model = comparison?.Current;
            if (model == null)
            {
                return 0f;
            }

            IReadOnlyList<ChipInformationNode> contexts = ReadDirectContexts(model.Root);
            IReadOnlyList<ChipInformationNode> relations = ReadDeclaredRelations(model.Root);
            IReadOnlyList<ChipInformationNode> informationNodes =
                ChipInformationNodeQuery.ReadInformationNodes(model.Root);
            string sourceProfessionKey = GetSourceProfessionKey(comparison);
            float height = MeasureIdentityHeight(chipLabel, model.Root, width) + CardGap;
            float overviewHeight = MeasureGeneralBlocks(
                model.Root,
                informationNodes,
                width);
            if (overviewHeight > 0f)
            {
                height += overviewHeight + CardGap;
            }

            IReadOnlyList<ChipFeatureRenderResult> renderedFeatures = FeatureRendererRegistry.RenderAll(
                ChipInformationNodeQuery.ReadFeatures(model.Root),
                null);
            if (renderedFeatures.Count > 0)
            {
                height += MeasureFeatureSection(renderedFeatures, width, sourceProfessionKey, comparison) + CardGap;
            }

            if (contexts.Count > 0)
            {
                height += MeasureContexts(
                    contexts,
                    width,
                    comparison,
                    model.Root?.Key);
            }

            if (relations.Count > 0)
            {
                height += MeasureRelationSection(
                    relations,
                    width,
                    comparison,
                    model.Root?.Key) + CardGap;
            }

            return Mathf.Max(height + 10f, 140f);
        }

        /// <summary>名称与职业同处身份行；长名称按实际字体测量，不截断或缩小字号。</summary>
        private static void DrawGeneralIdentity(Rect rect, string chipLabel, ChipDetailIconSource iconSource, ChipInformationNode root)
        {
            float professionWidth = GetIdentityProfessionWidth(root, rect.width);
            DrawIdentityIcon(new Rect(rect.x, rect.y, IdentityRowHeight, IdentityRowHeight), iconSource);
            DrawText(new Rect(rect.x + IdentityRowHeight + 10f, rect.y,
                rect.width - IdentityRowHeight - 10f - professionWidth, rect.height),
                chipLabel, GameFont.Medium, Color.white, TextAnchor.MiddleLeft);
            if (professionWidth > 0f)
            {
                DrawText(new Rect(rect.xMax - professionWidth + 8f, rect.y, professionWidth - 8f, rect.height),
                    FormatOverviewInline(new OverviewLine("BDP_Info_Field_Profession".Translate().ToString(), GetProfessionLabel(root))),
                    GameFont.Small, Color.white, TextAnchor.MiddleRight);
            }
        }

        /// <summary>读取信息模型已经确定的最终职业，不从功能名推测。</summary>
        private static string GetProfessionLabel(ChipInformationNode root)
        {
            return ChipManufacturingDefLookup.FindProfession(FindProperty(root, "identity.source_profession")?.Value?.EnumKey)?.LabelCap.ToString() ?? string.Empty;
        }

        /// <summary>职业占用实际文字宽度，名称保留其余空间。</summary>
        private static float GetIdentityProfessionWidth(ChipInformationNode root, float width)
        {
            string profession = GetProfessionLabel(root);
            if (profession.NullOrEmpty()) return 0f;
            GameFont previous = Text.Font;
            try
            {
                Text.Font = GameFont.Small;
                return Mathf.Min(width * 0.34f, Text.CalcSize("BDP_Info_Field_Profession".Translate() + "：" + profession).x + 16f);
            }
            finally { Text.Font = previous; }
        }

        /// <summary>身份行测量与绘制共用同一职业宽度，防止长名称侵入下一行。</summary>
        private static float MeasureIdentityHeight(string label, ChipInformationNode root, float width)
        {
            return Mathf.Max(IdentityRowHeight, MeasureOverviewText(label,
                width - IdentityRowHeight - 10f - GetIdentityProfessionWidth(root, width), GameFont.Medium));
        }

        /// <summary>绘制身份区图标；优先真实芯片实例，否则回退到成品定义外观。</summary>
        private static void DrawIdentityIcon(Rect rect, ChipDetailIconSource iconSource)
        {
            if (iconSource?.Chip != null)
            {
                Widgets.ThingIcon(rect, iconSource.Chip);
                return;
            }

            if (iconSource?.FallbackDef != null)
            {
                Widgets.DefIcon(rect, iconSource.FallbackDef);
            }
        }

        /// <summary>绘制根级形态/姿态节点，并保留其嵌套层级。</summary>
        private static void DrawContexts(
            Rect rect,
            IReadOnlyList<ChipInformationNode> contexts,
            float width,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect,
            string expansionScopeKey)
        {
            DrawCaption(rect, "BDP_Info_Block_Contexts");
            float y = rect.y + 23f;
            if (contexts == null)
            {
                return;
            }

            for (int index = 0; index < contexts.Count; index++)
            {
                ChipInformationNode context = contexts[index];
                if (context == null)
                {
                    continue;
                }

                float contextHeight = MeasureContextTree(
                    context,
                    width - 18f,
                    comparison,
                    expansionScopeKey);
                DrawContextTree(
                    new Rect(rect.x + 9f, y, rect.width - 18f, contextHeight),
                    context,
                    width - 18f,
                    comparison,
                    visibleContentRect,
                    expansionScopeKey);
                y += contextHeight + 6f;
            }
        }

        /// <summary>递归绘制一个形态或姿态节点及其直接功能和上下文子项。</summary>
        private static void DrawContextTree(
            Rect rect,
            ChipInformationNode context,
            float width,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect,
            string expansionScopeKey,
            int depth = 0)
        {
            DrawNestingGuide(rect, depth);
            float contentX = rect.x + NestingGuideIndent;
            float contentWidth = width - NestingGuideIndent;
            DrawText(
                new Rect(contentX, rect.y, contentWidth, 20f),
                GetContextLabel(context),
                GameFont.Small,
                new Color(0.77f, 0.90f, 0.88f, 1f));

            float y = rect.y + 24f;
            IReadOnlyList<ChipFeatureRenderResult> features = FeatureRendererRegistry.RenderAll(
                ChipInformationNodeQuery.ReadFeatures(context),
                context?.Key);
            if (features.Count > 0)
            {
                float featureHeight = MeasureFeatureSection(
                    features,
                    contentWidth,
                    GetSourceProfessionKey(comparison),
                    comparison);
                DrawFeatureSection(
                    new Rect(contentX, y, contentWidth, featureHeight),
                    features,
                    contentWidth,
                    comparison,
                    visibleContentRect);
                y += featureHeight + 5f;
            }

            IReadOnlyList<ChipInformationNode> childContexts = ReadDirectContexts(context);
            IReadOnlyList<ChipInformationNode> relations = ReadDeclaredRelations(context);
            for (int index = 0; index < childContexts.Count; index++)
            {
                float childHeight = MeasureContextTree(
                    childContexts[index],
                    contentWidth,
                    comparison,
                    expansionScopeKey);
                DrawContextTree(
                    new Rect(contentX, y, contentWidth, childHeight),
                    childContexts[index],
                    contentWidth,
                    comparison,
                    visibleContentRect,
                    expansionScopeKey,
                    depth + 1);
                y += childHeight + 5f;
            }

            if (relations.Count > 0)
            {
                float relationHeight = MeasureRelationSection(
                    relations,
                    contentWidth,
                    comparison,
                    AppendExpansionScope(expansionScopeKey, context?.Key));
                DrawRelationSection(
                    new Rect(contentX, y, contentWidth, relationHeight),
                    relations,
                    contentWidth,
                    comparison,
                    visibleContentRect,
                    AppendExpansionScope(expansionScopeKey, context?.Key));
            }
        }

        /// <summary>绘制一条随深度加深的柔化左侧引导线，替代逐层套框表达嵌套层级。</summary>
        private static void DrawNestingGuide(Rect rect, int depth)
        {
            float alpha = Mathf.Min(0.85f, 0.32f + depth * 0.16f);
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y + 2f, NestingGuideWidth, Mathf.Max(0f, rect.height - 4f)),
                new Color(0.30f, 0.84f, 0.82f, alpha));
        }

        /// <summary>绘制语义功能区。</summary>
        private static void DrawFeatureSection(
            Rect rect,
            IReadOnlyList<ChipFeatureRenderResult> results,
            float width,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect)
        {
            float y = rect.y;
            for (int index = 0; index < results.Count; index++)
            {
                float height = MeasureFeature(
                    results[index],
                    width - 18f,
                    GetSourceProfessionKey(comparison),
                    comparison);
                DrawFeature(
                    new Rect(rect.x + 9f, y, rect.width - 18f, height),
                    results[index],
                    width - 18f,
                    comparison,
                    visibleContentRect);
                y += height + CardGap;
            }
        }

        /// <summary>绘制一项语义功能及其子功能。</summary>
        private static void DrawFeature(
            Rect rect,
            ChipFeatureRenderResult result,
            float width,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect)
        {
            if (result?.Feature == null)
            {
                return;
            }

            Color accent = GetFeatureAccentColor(result.Feature.DefinitionKey);
            Widgets.DrawBoxSolidWithOutline(
                rect,
                new Color(0.05f, 0.12f, 0.13f, 0.92f),
                new Color(0.24f, 0.47f, 0.49f, 0.68f));
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y, FeatureAccentBarWidth, rect.height),
                accent);
            string featureLabel = GetFeatureLabel(result.Feature);
            bool methodGroupsOwnFeatureTitle = HasMeleeAttackMethodGroup(result);
            if (!methodGroupsOwnFeatureTitle)
                DrawFeatureHeading(new Rect(rect.x + 8f, rect.y, rect.width - 16f, 27f), featureLabel, accent, true);
            float y = rect.y + (methodGroupsOwnFeatureTitle ? 0f : FeatureHeadingHeight(true));
            y += DrawFeatureBody(new Rect(rect.x + 8f, y, rect.width - 16f, 0f), result.Feature,
                result.Lines, result.Feature.DefinitionKey, comparison, visibleContentRect);

            DrawFeatureGroups(
                rect,
                ref y,
                result.Groups,
                width - 16f,
                result.Feature.DefinitionKey,
                featureLabel,
                comparison,
                visibleContentRect);
            DrawFeatureGroups(
                rect,
                ref y,
                result.Subgroups,
                width - 16f,
                result.Feature.DefinitionKey,
                featureLabel,
                comparison,
                visibleContentRect);

            if (result.Children != null)
            {
                for (int index = 0; index < result.Children.Count; index++)
                {
                    float childHeight = MeasureFeature(
                        result.Children[index],
                        width - 24f,
                        GetSourceProfessionKey(comparison),
                        comparison);
                    DrawFeature(
                        new Rect(rect.x + 12f, y + 5f, rect.width - 24f, childHeight),
                        result.Children[index],
                        width - 24f,
                        comparison,
                        visibleContentRect);
                    y += childHeight + 5f;
                }
            }
        }

        /// <summary>绘制功能或信息组拥有的一层信息组/子组列表。</summary>
        private static void DrawFeatureGroups(
            Rect ownerRect,
            ref float y,
            IReadOnlyList<ChipFeatureGroupRenderResult> groups,
            float width,
            string ownerFeatureDefinitionKey,
            string ownerFeatureLabel,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect)
        {
            if (groups == null)
            {
                return;
            }

            for (int index = 0; index < groups.Count; index++)
            {
                ChipFeatureGroupRenderResult group = groups[index];
                float height = MeasureFeatureGroup(
                    group,
                    width,
                    ownerFeatureDefinitionKey,
                    GetSourceProfessionKey(comparison),
                    comparison);
                DrawFeatureGroup(
                    new Rect(ownerRect.x + 8f, y + (IsFeatureTitleGroup(group, ownerFeatureDefinitionKey) ? 0f : 3f), ownerRect.width - 16f, height),
                    group,
                    width,
                    ownerFeatureDefinitionKey,
                    ownerFeatureLabel,
                    comparison,
                    visibleContentRect);
                y += height + (IsFeatureTitleGroup(group, ownerFeatureDefinitionKey) ? 0f : 6f);
            }
        }

        /// <summary>绘制一个信息组或额外效果子组及其属性。</summary>
        private static void DrawFeatureGroup(
            Rect rect,
            ChipFeatureGroupRenderResult group,
            float width,
            string ownerFeatureDefinitionKey,
            string ownerFeatureLabel,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect)
        {
            if (group == null)
            {
                return;
            }

            bool primary = IsFeatureTitleGroup(group, ownerFeatureDefinitionKey);
            Color labelColor = primary ? GetFeatureAccentColor(ownerFeatureDefinitionKey)
                : group.IsSubgroup ? new Color(0.72f, 0.80f, 0.78f, 1f) : new Color(0.42f, 0.75f, 0.72f, 1f);
            DrawFeatureHeading(rect, GetFeatureGroupDisplayLabel(group, ownerFeatureDefinitionKey, ownerFeatureLabel),
                labelColor, primary);
            float y = rect.y + FeatureHeadingHeight(primary);
            float inset = primary ? 0f : 8f;
            y += DrawFeatureBody(new Rect(rect.x + inset, y, rect.width - inset * 2f, 0f), group.Node,
                group.Lines, ownerFeatureDefinitionKey, comparison, visibleContentRect);

            DrawFeatureGroups(
                rect,
                ref y,
                group.Subgroups,
                width - 16f,
                ownerFeatureDefinitionKey,
                ownerFeatureLabel,
                comparison,
                visibleContentRect);
        }

        /// <summary>功能主标题与普通组标题使用同一入口；层级决定视觉权重。</summary>
        private static void DrawFeatureHeading(Rect rect, string title, Color color, bool primary)
        {
            DrawText(new Rect(rect.x + (primary ? 3f : 0f), rect.y + (primary ? 4f : 0f),
                rect.width - (primary ? 6f : 0f), primary ? 20f : 18f), title,
                primary ? GameFont.Small : GameFont.Tiny, color);
        }

        /// <summary>主标题统一占用高度，普通次级信息组保留次级行高。</summary>
        private static float FeatureHeadingHeight(bool primary) => primary ? 27f : 18f;

        /// <summary>识别已经承担所属功能主标题的语义组，不因数据层级改变标题样式。</summary>
        private static bool IsFeatureTitleGroup(ChipFeatureGroupRenderResult group, string ownerFeatureDefinitionKey)
        {
            return ownerFeatureDefinitionKey == "BDP_Feature_MeleeAttack"
                && group?.Node?.DefinitionKey == "BDP_FeatureGroup_MeleeAttackMethod";
        }

        /// <summary>测量或绘制同一份功能正文；功能节点和信息组共用图表、文本布局与过滤规则。</summary>
        private static float LayoutFeatureBody(Rect rect, ChipInformationNode node,
            IReadOnlyList<ChipFeatureRenderLine> lines, string ownerKey, string profession,
            ChipInformationComparisonModel comparison, Rect visibleContentRect, bool draw)
        {
            IReadOnlyList<ChipFeatureRenderLine> displayLines = MarkChangedFeatureLines(
                lines, node, comparison);
            float chartHeight = MeasureFeatureCharts(node, displayLines, ownerKey, rect.width,
                profession, comparison);
            bool beside = PlaceShieldParametersBeside(node, rect.width);
            if (draw && chartHeight > 0f)
                DrawFeatureCharts(new Rect(rect.x, rect.y, rect.width, chartHeight), node, displayLines,
                    ownerKey, comparison, visibleContentRect);
            float lineWidth = beside ? rect.width - RangeDiagramHeight - 16f : rect.width;
            float lineX = rect.x + (beside ? RangeDiagramHeight + 16f : 0f);
            float y = beside ? ShieldCoverageTopInset : chartHeight > 0f ? chartHeight + 4f : 0f;
            if (displayLines != null)
            {
                foreach (ChipFeatureRenderLine line in displayLines)
                {
                    if (!ShouldDisplayFeatureLine(line, ownerKey, node, profession)
                        || IsRenderedInAttackStatTiles(line, displayLines, node, ownerKey, profession)) continue;
                    float height = FeatureLineHeight(line, lineWidth);
                    if (draw) DrawText(new Rect(lineX, rect.y + y, lineWidth, height), GetFeatureLineText(line),
                        GameFont.Tiny, Color.white);
                    y += height;
                }
            }
            return beside ? Mathf.Max(chartHeight, y) + 4f : y;
        }

        /// <summary>按同一功能节点的基准属性给展示行标记差异，保持解析值和来源模型不变。</summary>
        private static IReadOnlyList<ChipFeatureRenderLine> MarkChangedFeatureLines(
            IReadOnlyList<ChipFeatureRenderLine> lines,
            ChipInformationNode feature,
            ChipInformationComparisonModel comparison)
        {
            if (lines == null || comparison?.Baseline == null)
            {
                return lines;
            }

            ChipInformationNode baselineFeature = FindComparableFeature(comparison.Baseline, feature);
            if (baselineFeature == null)
            {
                return lines;
            }

            List<ChipFeatureRenderLine> result = new List<ChipFeatureRenderLine>(lines.Count);
            for (int index = 0; index < lines.Count; index++)
            {
                ChipFeatureRenderLine line = lines[index];
                ChipInformationProperty baseline = FindProperty(baselineFeature, line?.Property?.Key);
                result.Add(HasChangedDisplayedValue(line?.Property, baseline)
                    ? new ChipFeatureRenderLine(line.Property, line.Label, true)
                    : line);
            }

            return result;
        }

        /// <summary>仅比较玩家可见的结果值；缺少对应基准值时不标记。</summary>
        private static bool HasChangedDisplayedValue(
            ChipInformationProperty current,
            ChipInformationProperty baseline)
        {
            return current?.Value != null && baseline?.Value != null
                && !string.Equals(
                    ChipInformationTextFormatter.FormatFeatureProperty(current),
                    ChipInformationTextFormatter.FormatFeatureProperty(baseline),
                    StringComparison.Ordinal);
        }

        /// <summary>把差异符号紧贴在属性名称后，不修改属性定义及本地化名称。</summary>
        private static string GetComparedLineLabel(ChipFeatureRenderLine line)
        {
            string label = line?.Label ?? string.Empty;
            return line?.HasChangedBaselineValue == true
                ? label + "BDP_Info_ChangedValueMarker".Translate().ToString()
                : label;
        }

        /// <summary>绘制标准正文并返回其真实高度，避免功能与组各自累加坐标。</summary>
        private static float DrawFeatureBody(Rect rect, ChipInformationNode node,
            IReadOnlyList<ChipFeatureRenderLine> lines, string ownerKey,
            ChipInformationComparisonModel comparison, Rect visibleContentRect)
        {
            return LayoutFeatureBody(rect, node, lines, ownerKey, GetSourceProfessionKey(comparison),
                comparison, visibleContentRect, true);
        }

        /// <summary>将近战攻击方式名称并入所属功能标题。</summary>
        private static string GetFeatureGroupDisplayLabel(
            ChipFeatureGroupRenderResult group,
            string ownerFeatureDefinitionKey,
            string ownerFeatureLabel)
        {
            string groupLabel = GetFeatureGroupLabel(group);
            if (string.Equals(ownerFeatureDefinitionKey, "BDP_Feature_MeleeAttack", StringComparison.Ordinal)
                && string.Equals(group?.Node?.DefinitionKey, "BDP_FeatureGroup_MeleeAttackMethod", StringComparison.Ordinal)
                && !ownerFeatureLabel.NullOrEmpty()
                && !groupLabel.NullOrEmpty())
            {
                return "BDP_Info_FeatureInstanceTitle".Translate(ownerFeatureLabel, groupLabel).ToString();
            }

            return groupLabel;
        }

        /// <summary>判断近战功能是否以攻击方式组作为各自标题。</summary>
        private static bool HasMeleeAttackMethodGroup(ChipFeatureRenderResult result)
        {
            return HasMeleeAttackMethodGroup(result?.Groups)
                || HasMeleeAttackMethodGroup(result?.Subgroups);
        }

        /// <summary>检查一组直接子项是否包含近战攻击方式组。</summary>
        private static bool HasMeleeAttackMethodGroup(
            IReadOnlyList<ChipFeatureGroupRenderResult> groups)
        {
            if (groups == null)
            {
                return false;
            }

            for (int index = 0; index < groups.Count; index++)
            {
                if (string.Equals(
                    groups[index]?.Node?.DefinitionKey,
                    "BDP_FeatureGroup_MeleeAttackMethod",
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>读取实例覆写标签或信息组 Def 的本地化名称。</summary>
        private static string GetFeatureGroupLabel(ChipFeatureGroupRenderResult group)
        {
            if (group == null)
            {
                return string.Empty;
            }

            if (!group.LabelKey.NullOrEmpty())
            {
                return group.LabelKey.Translate().ToString();
            }

            ChipFeatureGroupDef definition = string.IsNullOrWhiteSpace(group.Node?.DefinitionKey)
                ? null
                : DefDatabase<ChipFeatureGroupDef>.GetNamedSilentFail(group.Node.DefinitionKey);
            string label = definition?.LabelCap.ToString();
            return !label.NullOrEmpty() ? label : string.Empty;
        }

        /// <summary>读取上下文节点下直接存在的形态或姿态，不跨层扁平化。</summary>
        private static IReadOnlyList<ChipInformationNode> ReadDirectContexts(
            ChipInformationNode parent)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (parent?.Children == null)
            {
                return result.AsReadOnly();
            }

            for (int index = 0; index < parent.Children.Count; index++)
            {
                ChipInformationNode child = parent.Children[index];
                if (string.Equals(child?.KindKey, "context", StringComparison.Ordinal))
                {
                    result.Add(child);
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>过滤已明确不展示的属性，并隐藏缺少玩家本地化名称的原始键。</summary>
        private static bool ShouldDisplayFeatureLine(
            ChipFeatureRenderLine line,
            string ownerFeatureDefinitionKey,
            ChipInformationNode chartNode,
            string sourceProfessionKey)
        {
            string key = line?.Property?.Key;
            if (string.IsNullOrWhiteSpace(key)
                || string.IsNullOrWhiteSpace(line.Label)
                || string.Equals(line.Label, key, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.Equals(key, "BDP_Property_TargetType", StringComparison.Ordinal)
                // 投射物名称已呈现在远程攻击标题，不重复生成参数卡片。
                || string.Equals(key, "BDP_Property_Projectile", StringComparison.Ordinal)
                || string.Equals(key, "BDP_Property_RequiresLineOfSight", StringComparison.Ordinal)
                || string.Equals(key, "BDP_Property_ProjectileFlightFeatures", StringComparison.Ordinal))
            {
                return false;
            }

            if (IsPropertyShownInFeatureChart(chartNode, key, sourceProfessionKey))
            {
                return false;
            }

            bool isAttack = string.Equals(
                ownerFeatureDefinitionKey,
                "BDP_Feature_RangedAttack",
                StringComparison.Ordinal)
                || string.Equals(
                    ownerFeatureDefinitionKey,
                    "BDP_Feature_MeleeAttack",
                    StringComparison.Ordinal);
            if (!isAttack)
            {
                return true;
            }

            return !string.Equals(key, "BDP_Property_AttackBurstInterval", StringComparison.Ordinal);
        }

        /// <summary>判断属性是否已经由该节点实际绘制的图表或几何图完整呈现。</summary>
        private static bool IsPropertyShownInFeatureChart(
            ChipInformationNode feature,
            string propertyKey,
            string sourceProfessionKey)
        {
            if (feature == null || string.IsNullOrWhiteSpace(propertyKey))
            {
                return false;
            }

            bool ranged = string.Equals(
                feature.DefinitionKey,
                "BDP_Feature_RangedAttack",
                StringComparison.Ordinal);
            bool melee = string.Equals(
                    feature.DefinitionKey,
                    "BDP_Feature_MeleeAttack",
                    StringComparison.Ordinal)
                || string.Equals(
                    feature.DefinitionKey,
                    "BDP_FeatureGroup_MeleeAttackMethod",
                    StringComparison.Ordinal);
            if (ranged || melee)
            {
                RadarMetric[] metrics = GetRadarMetrics(melee, sourceProfessionKey);
                if (HasRadarValues(feature, metrics)
                    && IsRadarProperty(propertyKey, metrics))
                {
                    return true;
                }

                return ranged
                    && HasAccuracy(feature)
                    && IsAccuracyProperty(propertyKey);
            }

            if (HasBlockArc(feature))
            {
                return string.Equals(
                    propertyKey,
                    "BDP_Property_BlockArc",
                    StringComparison.Ordinal);
            }

            // 距离短弧图已完整呈现有效距离与最小距离，不再重复为文本行。
            if (HasCastRangeArc(feature))
            {
                return string.Equals(propertyKey, "BDP_Property_CastRange", StringComparison.Ordinal)
                    || string.Equals(propertyKey, "BDP_Property_MinimumCastRange", StringComparison.Ordinal);
            }

            return false;
        }

        /// <summary>判断属性键是否对应当前完整绘制的雷达图数值轴。</summary>
        private static bool IsRadarProperty(
            string propertyKey,
            IReadOnlyList<RadarMetric> metrics)
        {
            if (metrics == null)
            {
                return false;
            }

            for (int index = 0; index < metrics.Count; index++)
            {
                if (string.Equals(metrics[index]?.Key, propertyKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>判断属性键是否对应当前完整绘制的四距离精度曲线。</summary>
        private static bool IsAccuracyProperty(string propertyKey)
        {
            return string.Equals(propertyKey, "BDP_Property_AccuracyTouch", StringComparison.Ordinal)
                || string.Equals(propertyKey, "BDP_Property_AccuracyShort", StringComparison.Ordinal)
                || string.Equals(propertyKey, "BDP_Property_AccuracyMedium", StringComparison.Ordinal)
                || string.Equals(propertyKey, "BDP_Property_AccuracyLong", StringComparison.Ordinal);
        }

        /// <summary>紧凑身份行之后用全宽描述，再接随内容换行的无框激活条件。</summary>
        private static void DrawGeneralBlocks(Rect rect, ChipInformationNode root, IReadOnlyList<ChipInformationNode> blocks)
        {
            List<OverviewLine> metadata = BuildOverviewLines(blocks, false);
            List<OverviewLine> conditions = BuildOverviewLines(blocks, true);
            List<OverviewDescription> descriptions = BuildOverviewDescriptions(blocks);
            float y = rect.y;
            string identity = BuildOverviewInline(metadata);
            if (!identity.NullOrEmpty())
            {
                float height = MeasureOverviewText(identity, rect.width, GameFont.Small);
                DrawText(new Rect(rect.x, y, rect.width, height), identity, GameFont.Small, Color.white);
                y += height + 12f;
            }
            float descriptionHeight = MeasureOverviewDescriptions(descriptions, rect.width);
            if (descriptionHeight > 0f)
                DrawOverviewDescriptions(new Rect(rect.x, y, rect.width, descriptionHeight), descriptions);
            if (conditions.Count > 0)
            {
                LayoutActivationConditions(new Rect(rect.x,
                    y + (descriptionHeight > 0f ? descriptionHeight + 8f : 0f), rect.width, 0f), conditions, true);
            }
        }

        /// <summary>身份元信息使用一条自然行，允许标签按真实文字宽度换行。</summary>
        private static string BuildOverviewInline(IReadOnlyList<OverviewLine> lines)
        {
            List<string> text = new List<string>();
            foreach (OverviewLine line in lines) text.Add(FormatOverviewInline(line));
            return string.Join("    ", text);
        }

        /// <summary>标签白色、数值青色；文本内容仍由语言包和模型提供。</summary>
        private static string FormatOverviewInline(OverviewLine line)
        {
            return "BDP_Info_InlineField".Translate(line.Label, "<color=#4DD6D1>" + line.Value + "</color>").ToString();
        }

        /// <summary>测量或绘制同一条无框条件带；按完整条件换行，测量与绘制共用布局。</summary>
        private static float LayoutActivationConditions(Rect rect, IReadOnlyList<OverviewLine> conditions, bool draw)
        {
            if (conditions == null || conditions.Count == 0) return 0f;
            string title = "BDP_Info_Block_Activation".Translate().ToString();
            bool titleAbove = rect.width < 180f;
            float titleWidth = titleAbove ? rect.width : MeasureOverviewTextWidth(title, GameFont.Tiny) + 12f;
            float titleHeight = MeasureTextHeight(title, titleWidth, GameFont.Tiny, 14f);
            float inset = titleAbove ? 0f : titleWidth;
            float availableWidth = Mathf.Max(1f, rect.width - inset);
            float y = titleAbove ? titleHeight + 4f : 0f;
            float x = 0f, rowHeight = 0f;
            if (draw) DrawText(new Rect(rect.x, rect.y + (titleAbove ? 0f : 3f), titleWidth, titleHeight),
                title, GameFont.Tiny, BaseColor);
            foreach (OverviewLine line in conditions)
            {
                string text = FormatOverviewInline(line);
                float width = Mathf.Min(availableWidth, MeasureOverviewTextWidth(text, GameFont.Small));
                float height = MeasureTextHeight(text, width, GameFont.Small, 18f);
                float gap = x > 0f ? 16f : 0f;
                if (x > 0f && x + gap + width > availableWidth)
                {
                    y += rowHeight + 4f;
                    x = 0f;
                    rowHeight = 0f;
                    gap = 0f;
                }
                if (draw)
                {
                    if (gap > 0f) DrawText(new Rect(rect.x + inset + x, rect.y + y, gap, height),
                        "BDP_Info_ActivationConditionSeparator".Translate(), GameFont.Tiny, BaseColor, TextAnchor.MiddleCenter);
                    DrawText(new Rect(rect.x + inset + x + gap, rect.y + y, width, height),
                        text, GameFont.Small, BaseColor);
                }
                x += gap + width;
                rowHeight = Mathf.Max(rowHeight, height);
            }
            return Mathf.Max(titleHeight, y + rowHeight);
        }

        /// <summary>使用原生字体测量富文本字段的真实宽度，留出少量字形边距。</summary>
        private static float MeasureOverviewTextWidth(string text, GameFont font)
        {
            GameFont previousFont = Text.Font;
            try
            {
                Text.Font = font;
                return Text.CalcSize(text ?? string.Empty).x + 2f;
            }
            finally { Text.Font = previousFont; }
        }

        /// <summary>分别读取身份元信息和激活条件，避免把条件混入身份表格。</summary>
        private static List<OverviewLine> BuildOverviewLines(IReadOnlyList<ChipInformationNode> blocks, bool activation)
        {
            List<OverviewLine> result = new List<OverviewLine>();
            if (blocks == null) return result;
            foreach (ChipInformationNode block in blocks)
            {
                if (block?.Properties == null) continue;
                if (activation && string.Equals(block.KindKey, "activation", StringComparison.Ordinal))
                {
                    AddActivationRequirementLines(result, block.Properties);
                }
                foreach (ChipInformationProperty field in block.Properties)
                {
                    if (activation && block.KindKey == "loadout" && field?.Key == "activationExclusionGroups")
                        AddOverviewLine(result, field, FormatOverviewValue(field));
                    if (!activation && block.KindKey == "profile" && (field?.Key == "category" || field?.Key == "tags"))
                        AddOverviewLine(result, field.Key == "category" ? "BDP_Info_Field_MainCategory".Translate().ToString()
                            : field.LabelKey.Translate().ToString(), FormatOverviewValue(field));
                }
            }
            return result;
        }

        /// <summary>从结构化描述块读取功能模板名称和说明文本。</summary>
        private static List<OverviewDescription> BuildOverviewDescriptions(
            IReadOnlyList<ChipInformationNode> blocks)
        {
            List<OverviewDescription> result = new List<OverviewDescription>();
            if (blocks == null)
            {
                return result;
            }

            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                ChipInformationNode block = blocks[blockIndex];
                if (!string.Equals(block?.KindKey, "descriptions", StringComparison.Ordinal)
                    || block.Children == null)
                {
                    continue;
                }

                for (int childIndex = 0; childIndex < block.Children.Count; childIndex++)
                {
                    ChipInformationNode child = block.Children[childIndex];
                    if (!string.Equals(child?.KindKey, "description", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string label = FormatOverviewValue(FindProperty(child, "displayLabel"));
                    string text = FormatOverviewValue(FindProperty(child, "description"));
                    if (!text.NullOrEmpty())
                    {
                        result.Add(new OverviewDescription(label, text));
                    }
                }
            }

            return result;
        }

        /// <summary>绘制简洁的描述段落，只有多条时才显示各模板名。</summary>
        private static void DrawOverviewDescriptions(
            Rect rect,
            IReadOnlyList<OverviewDescription> descriptions)
        {
            float innerX = rect.x;
            float innerWidth = Mathf.Max(rect.width, 64f);
            float y = rect.y;
            float headerHeight = MeasureOverviewText(
                "BDP_Info_Field_Description".Translate().ToString(),
                innerWidth,
                GameFont.Tiny);
            DrawText(
                new Rect(innerX, y, innerWidth, headerHeight),
                "BDP_Info_Field_Description".Translate().ToString(),
                GameFont.Tiny,
                BaseColor);
            y += headerHeight + 5f;

            for (int index = 0; index < descriptions.Count; index++)
            {
                OverviewDescription description = descriptions[index];
                if (index > 0)
                {
                    y += 4f;
                    Widgets.DrawLine(
                        new Vector2(innerX, y),
                        new Vector2(innerX + innerWidth, y),
                        ChartBorderColor,
                        1f);
                    y += 5f;
                }

                if (descriptions.Count > 1 && !description.Label.NullOrEmpty())
                {
                    float labelHeight = MeasureOverviewText(
                        description.Label,
                        innerWidth,
                        GameFont.Tiny);
                    DrawText(
                        new Rect(innerX, y, innerWidth, labelHeight),
                        description.Label,
                        GameFont.Tiny,
                        AccentColor);
                    y += labelHeight + 2f;
                }

                float textHeight = MeasureOverviewText(
                    description.Text,
                    innerWidth,
                    GameFont.Small);
                DrawText(
                    new Rect(innerX, y, innerWidth, textHeight),
                    description.Text,
                    GameFont.Small,
                    Color.white);
                y += textHeight;
            }
        }

        /// <summary>测量无外框的描述段落，和绘制时使用相同的间距。</summary>
        private static float MeasureOverviewDescriptions(
            IReadOnlyList<OverviewDescription> descriptions,
            float width)
        {
            if (descriptions == null || descriptions.Count == 0)
            {
                return 0f;
            }

            float innerWidth = Mathf.Max(width, 64f);
            float height = MeasureOverviewText(
                "BDP_Info_Field_Description".Translate().ToString(),
                innerWidth,
                GameFont.Tiny) + 5f;
            for (int index = 0; index < descriptions.Count; index++)
            {
                OverviewDescription description = descriptions[index];
                if (index > 0)
                {
                    height += 10f;
                }

                if (descriptions.Count > 1 && !description.Label.NullOrEmpty())
                {
                    height += MeasureOverviewText(
                        description.Label,
                        innerWidth,
                        GameFont.Tiny) + 2f;
                }

                height += MeasureOverviewText(
                    description.Text,
                    innerWidth,
                    GameFont.Small);
            }

            return height;
        }

        /// <summary>按指定游戏字号测量文本，并恢复绘制器原字号。</summary>
        private static float MeasureOverviewText(
            string text,
            float width,
            GameFont font)
        {
            GameFont previousFont = Text.Font;
            try
            {
                Text.Font = font;
                float minimumHeight = font == GameFont.Tiny ? 14f : 18f;
                return Mathf.Max(
                    minimumHeight,
                    Text.CalcHeight(text ?? string.Empty, Mathf.Max(width, 48f)));
            }
            finally
            {
                Text.Font = previousFont;
            }
        }

        /// <summary>把一条激活条件的名称、引用和值合并成结构化显示行。</summary>
        private static void AddActivationRequirementLines(
            ICollection<OverviewLine> target,
            IReadOnlyList<ChipInformationProperty> properties)
        {
            List<ActivationRequirementLine> requirements = new List<ActivationRequirementLine>();
            Dictionary<string, ActivationRequirementLine> byKey =
                new Dictionary<string, ActivationRequirementLine>(StringComparer.Ordinal);
            if (properties == null)
            {
                return;
            }

            for (int index = 0; index < properties.Count; index++)
            {
                ChipInformationProperty property = properties[index];
                string key = property?.Key;
                if (string.IsNullOrWhiteSpace(key)
                    || !key.StartsWith("condition.", StringComparison.Ordinal))
                {
                    continue;
                }

                int suffixStart = key.LastIndexOf('.');
                if (suffixStart <= "condition.".Length)
                {
                    continue;
                }

                string suffix = key.Substring(suffixStart + 1);
                if (!string.Equals(suffix, "required", StringComparison.Ordinal)
                    && !string.Equals(suffix, "reference", StringComparison.Ordinal))
                {
                    continue;
                }

                string conditionKey = key.Substring(0, suffixStart);
                ActivationRequirementLine line;
                if (!byKey.TryGetValue(conditionKey, out line))
                {
                    line = new ActivationRequirementLine();
                    byKey.Add(conditionKey, line);
                    requirements.Add(line);
                }

                if (line.Label.NullOrEmpty() && !property.LabelKey.NullOrEmpty())
                {
                    line.Label = property.LabelKey.Translate().ToString();
                }

                string value = FormatOverviewValue(property);
                if (string.Equals(suffix, "reference", StringComparison.Ordinal))
                {
                    value = FormatActivationReference(value);
                    if (property.Value?.DisplaySourceKind == ChipInformationValueDisplaySourceKind.DefReference
                        && string.Equals(property.Value.DefinitionTypeKey, typeof(SkillDef).AssemblyQualifiedName, StringComparison.Ordinal))
                    {
                        line.Label = "BDP_Info_ActivationSkill".Translate(value).ToString();
                        line.IsSkill = true;
                    }
                }

                if (string.Equals(suffix, "required", StringComparison.Ordinal))
                {
                    line.Required = value;
                }
                else
                {
                    line.Reference = value;
                }
            }

            List<OverviewLine> groupedRequirements = new List<OverviewLine>();
            Dictionary<string, int> requirementIndexByLabel =
                new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < requirements.Count; index++)
            {
                ActivationRequirementLine requirement = requirements[index];
                if (requirement.Label.NullOrEmpty())
                {
                    continue;
                }

                string value = requirement.IsSkill || requirement.Reference.NullOrEmpty()
                    ? requirement.Required
                    : requirement.Required.NullOrEmpty()
                        ? requirement.Reference
                        : requirement.Reference + " · " + requirement.Required;
                if (!value.NullOrEmpty())
                {
                    int existingIndex;
                    if (requirementIndexByLabel.TryGetValue(requirement.Label, out existingIndex))
                    {
                        OverviewLine existing = groupedRequirements[existingIndex];
                        groupedRequirements[existingIndex] = new OverviewLine(
                            existing.Label,
                            existing.Value + " / " + value);
                    }
                    else
                    {
                        requirementIndexByLabel.Add(requirement.Label, groupedRequirements.Count);
                        groupedRequirements.Add(new OverviewLine(requirement.Label, value));
                    }
                }
            }

            for (int index = 0; index < groupedRequirements.Count; index++)
            {
                target.Add(groupedRequirements[index]);
            }
        }

        /// <summary>将原始技能定义名转为游戏当前语言的技能名。</summary>
        private static string FormatActivationReference(string value)
        {
            if (value.NullOrEmpty())
            {
                return string.Empty;
            }

            SkillDef skill = DefDatabase<SkillDef>.GetNamedSilentFail(value);
            return skill?.LabelCap.ToString() ?? value;
        }

        /// <summary>添加有本地化标签和实际值的概览行。</summary>
        private static void AddOverviewLine(
            ICollection<OverviewLine> target,
            ChipInformationProperty field,
            string value)
        {
            string label = field == null || field.LabelKey.NullOrEmpty()
                ? string.Empty
                : field.LabelKey.Translate().ToString();
            AddOverviewLine(target, label, value);
        }

        /// <summary>添加有本地化标签和值的概览项。</summary>
        private static void AddOverviewLine(
            ICollection<OverviewLine> target,
            string label,
            string value)
        {
            if (target != null && !label.NullOrEmpty() && !value.NullOrEmpty())
            {
                target.Add(new OverviewLine(label, value));
            }
        }

        /// <summary>以统一信息格式化器输出概览字段值。</summary>
        private static string FormatOverviewValue(ChipInformationProperty field)
        {
            if (field?.Value == null)
            {
                return string.Empty;
            }

            return ChipInformationTextFormatter.FormatValueWithUnit(
                field.Value,
                field.ValueKind,
                field.UnitKey,
                CultureInfo.InvariantCulture);
        }

        /// <summary>测量语义功能区高度。</summary>
        private static float MeasureFeatureSection(
            IReadOnlyList<ChipFeatureRenderResult> results,
            float width,
            string sourceProfessionKey,
            ChipInformationComparisonModel comparison)
        {
            float height = 0f;
            for (int index = 0; index < results.Count; index++)
            {
                height += MeasureFeature(results[index], width - 18f, sourceProfessionKey, comparison) + CardGap;
            }

            return height;
        }

        /// <summary>测量一项语义功能及子功能。</summary>
        private static float MeasureFeature(
            ChipFeatureRenderResult result,
            float width,
            string sourceProfessionKey,
            ChipInformationComparisonModel comparison)
        {
            if (result?.Feature == null)
            {
                return 0f;
            }

            bool methodGroupsOwnFeatureTitle = HasMeleeAttackMethodGroup(result);
            float height = methodGroupsOwnFeatureTitle ? 0f : FeatureHeadingHeight(true);
            height += LayoutFeatureBody(new Rect(0f, 0f, width - 16f, 0f), result.Feature,
                result.Lines, result.Feature.DefinitionKey, sourceProfessionKey, comparison, default(Rect), false);

            height += MeasureFeatureGroups(
                result.Groups,
                width - 16f,
                result.Feature.DefinitionKey,
                sourceProfessionKey,
                comparison);
            height += MeasureFeatureGroups(
                result.Subgroups,
                width - 16f,
                result.Feature.DefinitionKey,
                sourceProfessionKey,
                comparison);

            if (result.Children != null)
            {
                for (int index = 0; index < result.Children.Count; index++)
                {
                    height += 5f + MeasureFeature(
                        result.Children[index],
                        width - 24f,
                        sourceProfessionKey,
                        comparison);
                }
            }

            return height + 8f;
        }

        /// <summary>测量功能或信息组拥有的一层信息组/子组列表。</summary>
        private static float MeasureFeatureGroups(
            IReadOnlyList<ChipFeatureGroupRenderResult> groups,
            float width,
            string ownerFeatureDefinitionKey,
            string sourceProfessionKey,
            ChipInformationComparisonModel comparison)
        {
            float height = 0f;
            if (groups != null)
            {
                for (int index = 0; index < groups.Count; index++)
                {
                    height += MeasureFeatureGroup(
                        groups[index],
                        width,
                        ownerFeatureDefinitionKey,
                        sourceProfessionKey,
                        comparison) + (IsFeatureTitleGroup(groups[index], ownerFeatureDefinitionKey) ? 0f : 6f);
                }
            }

            return height;
        }

        /// <summary>测量信息组或额外效果子组内容高度。</summary>
        private static float MeasureFeatureGroup(
            ChipFeatureGroupRenderResult group,
            float width,
            string ownerFeatureDefinitionKey,
            string sourceProfessionKey,
            ChipInformationComparisonModel comparison)
        {
            if (group == null)
            {
                return 0f;
            }

            bool primary = IsFeatureTitleGroup(group, ownerFeatureDefinitionKey);
            float height = FeatureHeadingHeight(primary);
            height += LayoutFeatureBody(new Rect(0f, 0f, width - (primary ? 0f : 16f), 0f), group.Node,
                group.Lines, ownerFeatureDefinitionKey, sourceProfessionKey, comparison, default(Rect), false);

            height += MeasureFeatureGroups(
                group.Subgroups,
                width - 16f,
                ownerFeatureDefinitionKey,
                sourceProfessionKey,
                comparison);
            return height;
        }

        /// <summary>测量全宽描述与其下的无框条件带，条件布局与绘制共用同一入口。</summary>
        private static float MeasureGeneralBlocks(ChipInformationNode root, IReadOnlyList<ChipInformationNode> blocks, float width)
        {
            List<OverviewLine> metadata = BuildOverviewLines(blocks, false);
            List<OverviewLine> conditions = BuildOverviewLines(blocks, true);
            List<OverviewDescription> descriptions = BuildOverviewDescriptions(blocks);
            string identity = BuildOverviewInline(metadata);
            float height = identity.NullOrEmpty() ? 0f : MeasureOverviewText(identity, width, GameFont.Small) + 12f;
            float descriptionHeight = MeasureOverviewDescriptions(descriptions, width);
            float conditionHeight = LayoutActivationConditions(new Rect(0f, 0f, width, 0f), conditions, false);
            return height + descriptionHeight + conditionHeight
                + (descriptionHeight > 0f && conditionHeight > 0f ? 8f : 0f) + 8f;
        }

        /// <summary>测量一般信息块。</summary>
        private static float MeasureGeneralBlock(ChipInformationNode block, float width)
        {
            if (block == null)
            {
                return 0f;
            }

            float height = 20f;
            if (block.Properties != null)
            {
                for (int index = 0; index < block.Properties.Count; index++)
                {
                    height += GeneralFieldHeight(block.Properties[index], width - 12f);
                }
            }

            if (block.Children != null)
            {
                for (int index = 0; index < block.Children.Count; index++)
                {
                    height += MeasureGeneralBlock(block.Children[index], width - 10f);
                }
            }

            return height;
        }

        /// <summary>测量上下文树的总高度。</summary>
        private static float MeasureContexts(
            IReadOnlyList<ChipInformationNode> contexts,
            float width,
            ChipInformationComparisonModel comparison,
            string expansionScopeKey)
        {
            float height = 23f;
            if (contexts == null)
            {
                return height;
            }

            for (int index = 0; index < contexts.Count; index++)
            {
                height += MeasureContextTree(
                    contexts[index],
                    width - 18f,
                    comparison,
                    expansionScopeKey) + 6f;
            }

            return height;
        }

        /// <summary>测量一个形态或姿态节点及其完整子树。</summary>
        private static float MeasureContextTree(
            ChipInformationNode context,
            float width,
            ChipInformationComparisonModel comparison,
            string expansionScopeKey)
        {
            float contentWidth = width - NestingGuideIndent;
            float height = 24f;
            IReadOnlyList<ChipFeatureRenderResult> features = FeatureRendererRegistry.RenderAll(
                ChipInformationNodeQuery.ReadFeatures(context),
                context?.Key);
            if (features.Count > 0)
            {
                height += MeasureFeatureSection(features, contentWidth, GetSourceProfessionKey(comparison), comparison) + 5f;
            }

            IReadOnlyList<ChipInformationNode> childContexts = ReadDirectContexts(context);
            IReadOnlyList<ChipInformationNode> relations = ReadDeclaredRelations(context);
            for (int index = 0; index < childContexts.Count; index++)
            {
                height += MeasureContextTree(
                    childContexts[index],
                    contentWidth,
                    comparison,
                    expansionScopeKey) + 5f;
            }

            if (relations.Count > 0)
            {
                height += MeasureRelationSection(
                    relations,
                    contentWidth,
                    comparison,
                    AppendExpansionScope(expansionScopeKey, context?.Key)) + 5f;
            }

            return height;
        }

        /// <summary>读取当前节点直接声明的静态组合关系，忽略已发布运行结果关系。</summary>
        private static IReadOnlyList<ChipInformationNode> ReadDeclaredRelations(
            ChipInformationNode parent)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (parent?.Children == null)
            {
                return result.AsReadOnly();
            }

            for (int index = 0; index < parent.Children.Count; index++)
            {
                ChipInformationNode child = parent.Children[index];
                if (!string.Equals(child?.KindKey, "relation", StringComparison.Ordinal))
                {
                    continue;
                }

                ChipInformationProperty kind = FindProperty(child, "reference_kind");
                if (string.Equals(ReadText(kind), "definition", StringComparison.Ordinal))
                {
                    result.Add(child);
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>绘制组合关系及其可展开的 ComboDef 声明功能树。</summary>
        private static void DrawRelationSection(
            Rect rect,
            IReadOnlyList<ChipInformationNode> relations,
            float width,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect,
            string expansionScopeKey)
        {
            List<RelationLine> lines = BuildRelationLines(relations, expansionScopeKey, comparison);
            if (lines.Count == 0)
            {
                return;
            }

            DrawCaption(rect, "BDP_Info_Relation_Combination");
            float y = rect.y + 23f;
            for (int index = 0; index < lines.Count; index++)
            {
                RelationLine line = lines[index];
                float titleHeight = MeasureRelationTitle(line, rect.width - 20f);
                Rect lineRect = new Rect(rect.x + 10f, y, rect.width - 20f, titleHeight);
                string lineText = line.Name;
                if (line.Features.Count > 0)
                {
                    bool expanded = ExpandedDeclaredComboSemantics.Contains(line.ExpansionKey);
                    if (Widgets.ButtonInvisible(lineRect))
                    {
                        if (expanded)
                        {
                            ExpandedDeclaredComboSemantics.Remove(line.ExpansionKey);
                            expanded = false;
                        }
                        else
                        {
                            ExpandedDeclaredComboSemantics.Add(line.ExpansionKey);
                            expanded = true;
                        }
                    }

                    DrawText(
                        lineRect,
                        (expanded ? "− " : "+ ") + lineText,
                        GameFont.Small,
                        Color.white);
                }
                else
                {
                    DrawText(lineRect, lineText, GameFont.Small, Color.white);
                }

                y += titleHeight;
                if (line.Features.Count > 0
                    && ExpandedDeclaredComboSemantics.Contains(line.ExpansionKey))
                {
                    y += 4f;
                    float featureHeight = MeasureFeatureSection(
                        line.Features,
                        rect.width - 20f,
                        GetSourceProfessionKey(line.Comparison),
                        line.Comparison);
                    DrawFeatureSection(
                        new Rect(rect.x + 10f, y, rect.width - 20f, featureHeight),
                        line.Features,
                        rect.width - 20f,
                        line.Comparison,
                        visibleContentRect);
                    y += featureHeight;
                }
            }
        }

        /// <summary>测量组合关系和当前展开的声明功能树。</summary>
        private static float MeasureRelationSection(
            IReadOnlyList<ChipInformationNode> relations,
            float width,
            ChipInformationComparisonModel comparison,
            string expansionScopeKey)
        {
            List<RelationLine> lines = BuildRelationLines(relations, expansionScopeKey, comparison);
            if (lines.Count == 0)
            {
                return 0f;
            }

            float height = 23f;
            for (int index = 0; index < lines.Count; index++)
            {
                height += MeasureRelationTitle(lines[index], width - 20f);
                if (lines[index].Features.Count > 0
                    && ExpandedDeclaredComboSemantics.Contains(lines[index].ExpansionKey))
                {
                    height += 4f + MeasureFeatureSection(
                        lines[index].Features,
                        width - 20f,
                        GetSourceProfessionKey(lines[index].Comparison),
                        lines[index].Comparison);
                }
            }

            return height;
        }

        /// <summary>把关系节点解析为组合定义名称、说明和声明功能树。</summary>
        private static List<RelationLine> BuildRelationLines(
            IReadOnlyList<ChipInformationNode> relations,
            string expansionScopeKey,
            ChipInformationComparisonModel comparison)
        {
            List<RelationLine> result = new List<RelationLine>();
            // 每次构造只处理一个根或上下文层，集合因此不会跨层隐藏合法关系。
            HashSet<string> seenComboDefinitions =
                new HashSet<string>(StringComparer.Ordinal);
            if (relations == null)
            {
                return result;
            }

            for (int index = 0; index < relations.Count; index++)
            {
                ChipInformationNode relation = relations[index];
                string referenceKey = ReadText(FindProperty(relation, "reference_key"));
                if (string.IsNullOrWhiteSpace(referenceKey))
                {
                    continue;
                }

                ComboDef combo = DefDatabase<ComboDef>.GetNamedSilentFail(referenceKey);
                if (combo == null
                    || string.IsNullOrWhiteSpace(combo.LabelCap.ToString()))
                {
                    continue;
                }

                DeclaredComboProjection projection = ResolveDeclaredComboFeatures(combo, comparison);
                if (projection == null
                    || !projection.IsAdmitted
                    || !seenComboDefinitions.Add(combo.defName))
                {
                    continue;
                }

                IReadOnlyList<ChipFeatureRenderResult> features = FeatureRendererRegistry.RenderAll(
                    projection?.CurrentFeatureNodes);
                result.Add(new RelationLine(
                    GetComboTitle(combo, features),
                    AppendExpansionScope(expansionScopeKey, relation.Key),
                    features,
                    projection?.Comparison));
            }

            return result;
        }

        /// <summary>详情选择发生变化时清空声明语义展开状态，使新详情默认折叠。</summary>
        internal static void ResetDeclaredComboExpansion()
        {
            ExpandedDeclaredComboSemantics.Clear();
        }

        /// <summary>组合技标题按真实功能类型、组合名称和两个来源模板本地化生成。</summary>
        private static string GetComboTitle(ComboDef combo, IReadOnlyList<ChipFeatureRenderResult> features)
        {
            List<string> types = new List<string>();
            foreach (ChipFeatureRenderResult feature in features)
            {
                ChipFeatureDef definition = DefDatabase<ChipFeatureDef>.GetNamedSilentFail(feature.Feature.DefinitionKey);
                string label = definition?.LabelCap.ToString();
                if (!label.NullOrEmpty() && !types.Contains(label)) types.Add(label);
            }
            string sources = "BDP_Info_ComboSources".Translate(
                GetComboSourceLabel(combo.firstSourceKey), GetComboSourceLabel(combo.secondSourceKey)).ToString();
            string name = "BDP_Info_ComboNameWithSources".Translate(combo.LabelCap, sources).ToString();
            return types.Count == 0 ? name : "BDP_Info_FeatureInstanceTitle".Translate(
                string.Join("BDP_Info_ComboFeatureSeparator".Translate().ToString(), types), name).ToString();
        }

        /// <summary>只显示组合来源模板的本地化名称；自组合保留两个真实参与项。</summary>
        private static string GetComboSourceLabel(string key)
        {
            return ChipManufacturingDefLookup.FindChipFunctionDef(key)?.ResolvedLabel
                ?? "BDP_Window_TriggerAssembly_Unknown".Translate().ToString();
        }

        /// <summary>折叠标题包括开合标记，按实际宽度测量换行高度。</summary>
        private static float MeasureRelationTitle(RelationLine line, float width)
        {
            return MeasureTextHeight("+ " + line.Name, width, GameFont.Small, 20f);
        }

        /// <summary>逐层拼接根节点、上下文和关系键，隔离不同关系实例的折叠状态。</summary>
        private static string AppendExpansionScope(string parentKey, string childKey)
        {
            if (string.IsNullOrWhiteSpace(childKey))
            {
                return parentKey ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(parentKey)
                ? childKey
                : parentKey + "/" + childKey;
        }

        /// <summary>按当前展示宿主筛查组合准入，再读取组合结果功能树。</summary>
        private static DeclaredComboProjection ResolveDeclaredComboFeatures(
            ComboDef combo,
            ChipInformationComparisonModel comparison)
        {
            if (combo == null || comparison == null)
            {
                return new DeclaredComboProjection(
                    new List<ChipInformationNode>().AsReadOnly(),
                    null,
                    false);
            }

            // 缓存属于当前成品比较上下文，不能把同职业同构型的不同复合成品混用。
            if (!ReferenceEquals(DeclaredComboOwner, comparison))
            {
                DeclaredComboProjections.Clear();
                DeclaredComboOwner = comparison;
            }
            string cacheKey = combo.defName;
            if (!DeclaredComboProjections.TryGetValue(cacheKey, out DeclaredComboProjection projection))
            {
                // 此准入只决定组合条目是否显示，图表基准始终由宿主功能结果单独提供。
                if (!TryResolveComboSide(combo.firstSourceKey, comparison,
                        out ChipDefinitionConfig first, out ChipSourceReferenceSnapshot firstReference)
                    || !TryResolveComboSide(combo.secondSourceKey, comparison,
                        out ChipDefinitionConfig second, out ChipSourceReferenceSnapshot secondReference)
                    || !ComboDeclarationResolver.IsPairAllowed(combo, first, firstReference, second, secondReference))
                {
                    projection = new DeclaredComboProjection(
                        new List<ChipInformationNode>().AsReadOnly(),
                        null,
                        false);
                    DeclaredComboProjections[cacheKey] = projection;
                    return projection;
                }

                if (combo.Features == null || combo.Features.Count == 0)
                {
                    projection = new DeclaredComboProjection(
                        new List<ChipInformationNode>().AsReadOnly(),
                        CreateComboChartComparison(comparison),
                        true);
                    DeclaredComboProjections[cacheKey] = projection;
                    return projection;
                }

                ChipExpressionConfig declaredEntries =
                    ComboDeclarationResolver.ResolveEntriesWithoutVariantModifiers(
                        combo,
                        first,
                        second,
                        firstReference,
                        secondReference);
                ChipExpressionConfig resultEntries = ComboDeclarationResolver.ResolveEntries(
                    combo,
                    first,
                    second,
                    firstReference,
                    secondReference);
                ChipDefinitionConfig declaredConfig = new ChipDefinitionConfig
                {
                    Expression = declaredEntries,
                    Features = combo.Features
                };
                ChipDefinitionConfig resultConfig = new ChipDefinitionConfig
                {
                    Expression = resultEntries,
                    Features = combo.Features
                };
                ChipInformationIdentity identity = new ChipInformationIdentity(
                    null,
                    combo.defName,
                    null,
                    GetSourceProfessionKey(comparison));
                IReadOnlyList<ChipInformationNode> currentNodes =
                    new ChipFeatureResolver().Resolve(combo.Features, new ChipFeatureReadContext(
                        identity,
                        declaredConfig,
                        resultConfig,
                        new ChipInformationRuntimeState())).Features;
                projection = new DeclaredComboProjection(
                    currentNodes,
                    CreateComboChartComparison(comparison),
                    true);
                DeclaredComboProjections[cacheKey] = projection;
            }
            return projection;
        }

        /// <summary>组合图表基准指向当前宿主模型，由同语义功能节点提供轴值。</summary>
        private static ChipInformationComparisonModel CreateComboChartComparison(
            ChipInformationComparisonModel source)
        {
            return source == null
                ? null
                : ChipInformationComparisonModel.Create(
                    source.Current,
                    source.Current,
                    null,
                    ChipInformationComparisonBaselineMode.HostFunctionResult,
                    source.SourceConfig,
                    source.SourceReference);
        }

        /// <summary>当前侧复用展示宿主配置，另一侧从声明模板构造并沿用宿主构型修正。</summary>
        private static bool TryResolveComboSide(string key, ChipInformationComparisonModel comparison,
            out ChipDefinitionConfig config, out ChipSourceReferenceSnapshot reference)
        {
            reference = comparison?.SourceReference;
            if (reference?.OrderedSourceKeys?.Count > 0
                && string.Equals(reference.OrderedSourceKeys[0], key, StringComparison.OrdinalIgnoreCase))
            {
                config = comparison.SourceConfig;
                return config != null;
            }
            return ComboDeclaredSourceResolver.TryResolve(key, reference?.SourceProfessionKey,
                reference?.SourceVariantKey, out config, out reference);
        }

        /// <summary>读取文本值，不向玩家暴露关系节点内部键。</summary>
        private static string ReadText(ChipInformationProperty property)
        {
            return property?.Value == null
                ? string.Empty
                : property.Value.Kind == ChipInformationValueKind.Text
                    ? property.Value.Text ?? string.Empty
                    : string.Empty;
        }

        /// <summary>从现有信息模型根节点读取芯片最终职业键。</summary>
        private static string GetSourceProfessionKey(ChipInformationComparisonModel comparison)
        {
            ChipInformationProperty property = FindProperty(
                comparison?.Current?.Root,
                "identity.source_profession");
            return property?.Value?.EnumKey ?? string.Empty;
        }

        /// <summary>狙击手原有的雷达轴边界值预设；暂不启用，便于之后恢复。</summary>
        private static RadarMetric[] GetSniperRadarMetricPreset()
        {
            return new[]
            {
                new RadarMetric("BDP_Property_CastRange", "BDP_Info_Field_Range", 80f),
                new RadarMetric("BDP_Property_DamageAmount", "BDP_Info_Field_ProjectileDamage", 60f),
                new RadarMetric("BDP_Property_ArmorPenetration", "BDP_Info_Field_ProjectileArmorPenetration", 0.5f),
                new RadarMetric("BDP_Property_ProjectileSpeed", "BDP_Info_Field_ProjectileSpeed", 200f),
                new RadarMetric("BDP_Property_StoppingPower", "BDP_Info_Field_ProjectileStoppingPower", 4f),
                new RadarMetric("RPM", "BDP_Window_TriggerAssembly_RpmAxis", 180f)
            };
        }

        /// <summary>按职业选雷达轴边界值；狙击手暂用射手与枪手的预设。</summary>
        private static RadarMetric[] GetRadarMetrics(bool melee, string sourceProfessionKey)
        {
            if (melee)
            {
                return string.Equals(
                    sourceProfessionKey,
                    "BDP_ChipProfession_Attacker",
                    StringComparison.Ordinal)
                    ? new[]
                    {
                        new RadarMetric("BDP_Property_CastRange", "BDP_Info_Field_MeleeAttackRange", 3f),
                        new RadarMetric("BDP_Property_DamageAmount", "BDP_Info_Field_AttackDamage", 30f),
                        new RadarMetric("BDP_Property_ArmorPenetration", "BDP_Info_Field_ProjectileArmorPenetration", 0.5f),
                        new RadarMetric("APM", "BDP_Window_TriggerAssembly_ApmAxis", 100f)
                    }
                    : null;
            }

            if (string.Equals(sourceProfessionKey, "BDP_ChipProfession_Shooter", StringComparison.Ordinal)
                || string.Equals(sourceProfessionKey, "BDP_ChipProfession_Gunner", StringComparison.Ordinal)
                || string.Equals(sourceProfessionKey, "BDP_ChipProfession_Sniper", StringComparison.Ordinal))
            {
                return new[]
                {
                    new RadarMetric("BDP_Property_CastRange", "BDP_Info_Field_Range", 40f),
                    new RadarMetric("BDP_Property_DamageAmount", "BDP_Info_Field_ProjectileDamage", 30f),
                    new RadarMetric("BDP_Property_ArmorPenetration", "BDP_Info_Field_ProjectileArmorPenetration", 0.5f),
                    new RadarMetric("BDP_Property_ProjectileSpeed", "BDP_Info_Field_ProjectileSpeed", 150f),
                    new RadarMetric("BDP_Property_StoppingPower", "BDP_Info_Field_ProjectileStoppingPower", 4f),
                    new RadarMetric("RPM", "BDP_Window_TriggerAssembly_RpmAxis", 180f)
                };
            }

            return null;
        }

        /// <summary>确认旧版要求的每个图轴都有真实数值来源。</summary>
        private static bool HasRadarValues(ChipInformationNode feature, IReadOnlyList<RadarMetric> metrics)
        {
            if (feature == null || metrics == null || metrics.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < metrics.Count; index++)
            {
                if (!HasRadarMetricValue(feature, metrics[index]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>判断旧版雷达某一轴是否有可绘制的当前值或基准值。</summary>
        private static bool HasRadarMetricValue(ChipInformationNode feature, RadarMetric metric)
        {
            if (feature == null || metric == null)
            {
                return false;
            }

            return metric.Key == "RPM" || metric.Key == "APM"
                ? GetMetricValue(feature, metric) > 0f
                : HasNumber(feature, metric.Key);
        }

        /// <summary>护盾角度图保留原尺寸；右侧至少容纳一行参数，窄区沿用上下排列。</summary>
        private static bool PlaceShieldParametersBeside(ChipInformationNode feature, float width)
        {
            return HasBlockArc(feature) && width >= RangeDiagramHeight + 16f + 160f;
        }

        /// <summary>测量功能所属的统计图、参数卡片和几何示意图。</summary>
        private static float MeasureFeatureCharts(
            ChipInformationNode feature,
            IReadOnlyList<ChipFeatureRenderLine> lines,
            string ownerFeatureDefinitionKey,
            float width,
            string sourceProfessionKey,
            ChipInformationComparisonModel comparison)
        {
            if (feature == null)
            {
                return 0f;
            }

            bool ranged = string.Equals(
                feature.DefinitionKey,
                "BDP_Feature_RangedAttack",
                StringComparison.Ordinal);
            bool melee = string.Equals(
                    feature.DefinitionKey,
                    "BDP_Feature_MeleeAttack",
                    StringComparison.Ordinal)
                || string.Equals(
                    feature.DefinitionKey,
                    "BDP_FeatureGroup_MeleeAttackMethod",
                    StringComparison.Ordinal);
            if (ranged || melee)
            {
                float height = 0f;
                RadarMetric[] metrics = GetRadarMetrics(melee, sourceProfessionKey);
                if (HasRadarValues(feature, metrics))
                {
                    IReadOnlyList<ChipFeatureRenderLine> statLines = GetAttackStatLines(
                        lines,
                        feature,
                        ownerFeatureDefinitionKey,
                        sourceProfessionKey);
                    AttackChartLayout layout = CalculateAttackChartLayout(width,
                        melee ? Mathf.Max(4, statLines.Count) : statLines.Count,
                        feature, FindComparableFeature(comparison?.Baseline, feature), metrics);
                    float rowHeight = RadarChartHeight;
                    if (statLines.Count > 0 || melee)
                    {
                        float statsHeight = MeasureAttackStatTilesHeight(
                            statLines,
                            layout.StatsWidth,
                            ranged);
                        if (layout.StatsBesideRadar)
                        {
                            rowHeight = Mathf.Max(rowHeight, statsHeight);
                        }
                        else
                        {
                            rowHeight += RadarStatsBelowGap
                                + statsHeight;
                        }
                    }

                    height += rowHeight + 7f;
                }

                if (ranged && HasAccuracy(feature))
                {
                    height += AccuracyChartHeight + 7f;
                }

                return height;
            }

            if (HasBlockArc(feature))
            {
                return ShieldCoverageTopInset
                    + RangeDiagramHeight
                    + ShieldCoverageBottomPadding;
            }

            if (HasCastRangeArc(feature))
            {
                return CastRangeArcHeight;
            }

            return 0f;
        }

        /// <summary>绘制攻击图表和属性卡片；范围、护盾图仍使用各自既有图形。</summary>
        private static void DrawFeatureCharts(
            Rect rect,
            ChipInformationNode feature,
            IReadOnlyList<ChipFeatureRenderLine> lines,
            string ownerFeatureDefinitionKey,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect)
        {
            float y = rect.y;
            string sourceProfessionKey = GetSourceProfessionKey(comparison);
            bool ranged = string.Equals(
                feature.DefinitionKey,
                "BDP_Feature_RangedAttack",
                StringComparison.Ordinal);
            bool melee = string.Equals(
                    feature.DefinitionKey,
                    "BDP_Feature_MeleeAttack",
                    StringComparison.Ordinal)
                || string.Equals(
                    feature.DefinitionKey,
                    "BDP_FeatureGroup_MeleeAttackMethod",
                    StringComparison.Ordinal);
            if (ranged || melee)
            {
                RadarMetric[] metrics = GetRadarMetrics(melee, sourceProfessionKey);
                if (HasRadarValues(feature, metrics))
                {
                    IReadOnlyList<ChipFeatureRenderLine> statLines = GetAttackStatLines(
                        lines,
                        feature,
                        ownerFeatureDefinitionKey,
                        sourceProfessionKey);
                    AttackChartLayout layout = CalculateAttackChartLayout(rect.width,
                        melee ? Mathf.Max(4, statLines.Count) : statLines.Count,
                        feature, FindComparableFeature(comparison?.Baseline, feature), metrics);
                    float statsHeight = statLines.Count > 0 || melee
                        ? MeasureAttackStatTilesHeight(statLines, layout.StatsWidth, ranged)
                        : 0f;
                    float rowHeight = RadarChartHeight;
                    if (statLines.Count > 0 || melee)
                    {
                        rowHeight = layout.StatsBesideRadar
                            ? Mathf.Max(rowHeight, statsHeight)
                            : rowHeight + RadarStatsBelowGap + statsHeight;
                    }

                    // 雷达和参数卡片共用外框，表达为一组完整的攻击统计图。
                    DrawChartFrame(new Rect(rect.x, y, rect.width, rowHeight));
                    float radarY = layout.StatsBesideRadar
                        ? y + (rowHeight - RadarChartHeight) * 0.5f
                        : y;
                    DrawRadarChart(
                        new Rect(
                            rect.x,
                            radarY,
                            layout.RadarWidth,
                            RadarPlotHeight + RadarBottomPadding),
                        feature,
                        comparison,
                        metrics,
                        visibleContentRect);

                    if (statLines.Count > 0 || melee)
                    {
                        if (layout.StatsBesideRadar)
                        {
                            DrawAttackStatTiles(
                                new Rect(
                                    rect.x + layout.RadarWidth + RadarStatsColumnGap,
                                    y + (rowHeight - statsHeight) * 0.5f,
                                    layout.StatsWidth,
                                    statsHeight),
                                statLines,
                                GetFeatureAccentColor(ownerFeatureDefinitionKey), ranged);
                        }
                        else
                        {
                            float statsY = y + RadarChartHeight + RadarStatsBelowGap;
                            DrawAttackStatTiles(
                                new Rect(rect.x, statsY, layout.StatsWidth, statsHeight),
                                statLines,
                                GetFeatureAccentColor(ownerFeatureDefinitionKey), ranged);
                        }
                    }

                    y += rowHeight + 7f;
                }

                if (ranged && HasAccuracy(feature))
                {
                    DrawChartFrame(
                        new Rect(rect.x, y, rect.width, AccuracyChartHeight),
                        "BDP_Window_TriggerAssembly_AccuracyCurve");
                    DrawAccuracyChart(
                        new Rect(
                            rect.x + 9f,
                            y + AccuracyTitleHeight,
                            rect.width - 18f,
                            AccuracyPlotHeight),
                        feature,
                        comparison,
                        visibleContentRect);
                }

                return;
            }

            if (HasBlockArc(feature))
            {
                DrawBlockArc(
                    new Rect(
                        rect.x + 1f,
                        y + ShieldCoverageTopInset,
                        RangeDiagramHeight,
                        RangeDiagramHeight),
                    feature,
                    comparison,
                    visibleContentRect);
                return;
            }

            if (HasCastRangeArc(feature))
            {
                DrawCastRangeArc(
                    new Rect(rect.x, y, rect.width, CastRangeArcHeight),
                    feature,
                    comparison,
                    visibleContentRect);
            }
        }

        /// <summary>取得需要以独立卡片显示的、未被攻击图表覆盖的属性。</summary>
        private static IReadOnlyList<ChipFeatureRenderLine> GetAttackStatLines(
            IReadOnlyList<ChipFeatureRenderLine> lines,
            ChipInformationNode feature,
            string ownerFeatureDefinitionKey,
            string sourceProfessionKey)
        {
            List<ChipFeatureRenderLine> result = new List<ChipFeatureRenderLine>();
            if (lines == null || feature == null
                || !HasRadarValues(feature, GetRadarMetrics(
                    string.Equals(feature.DefinitionKey, "BDP_Feature_MeleeAttack", StringComparison.Ordinal)
                        || string.Equals(feature.DefinitionKey, "BDP_FeatureGroup_MeleeAttackMethod", StringComparison.Ordinal),
                    sourceProfessionKey)))
            {
                return result.AsReadOnly();
            }

            for (int index = 0; index < lines.Count; index++)
            {
                if (ShouldDisplayFeatureLine(
                    lines[index],
                    ownerFeatureDefinitionKey,
                    feature,
                    sourceProfessionKey))
                {
                    result.Add(lines[index]);
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>判断属性行是否已转移到攻击雷达旁的卡片区。</summary>
        private static bool IsRenderedInAttackStatTiles(
            ChipFeatureRenderLine line,
            IReadOnlyList<ChipFeatureRenderLine> lines,
            ChipInformationNode feature,
            string ownerFeatureDefinitionKey,
            string sourceProfessionKey)
        {
            if (line == null)
            {
                return false;
            }

            IReadOnlyList<ChipFeatureRenderLine> statLines = GetAttackStatLines(
                lines,
                feature,
                ownerFeatureDefinitionKey,
                sourceProfessionKey);
            for (int index = 0; index < statLines.Count; index++)
            {
                if (ReferenceEquals(statLines[index], line))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>右侧保留紧凑纵列，其余宽度归雷达视口；几何半径继续由既有高度限制。</summary>
        private static AttackChartLayout CalculateAttackChartLayout(float availableWidth, int statCount,
            ChipInformationNode feature, ChipInformationNode baselineFeature, RadarMetric[] metrics)
        {
            float width = Mathf.Max(0f, availableWidth);
            float statsWidth = Mathf.Clamp(width * 0.32f, MinimumAttackStatsWidth, 144f);
            List<RadarChartAxis> axes = new List<RadarChartAxis>(metrics.Length);
            foreach (RadarMetric metric in metrics)
                axes.Add(new RadarChartAxis(GetComparedRadarMetricLabel(feature, baselineFeature, metric), 0f, metric.Maximum));
            float radarWidth = RadarChartRenderer.MeasureOutsideAxisWidth(axes, GameFont.Small, RadarPlotHeight);
            // 窄区优先从参数列的可伸缩余量中让出名称空间，不改变主体大小或已有并排门槛。
            statsWidth = Mathf.Min(statsWidth, Mathf.Max(MinimumAttackStatsWidth, width - RadarStatsColumnGap - radarWidth));
            bool beside = statCount > 0 && width >= RadarChartColumnWidth + RadarStatsColumnGap + MinimumAttackStatsWidth;
            return new AttackChartLayout
            {
                RadarWidth = beside ? width - statsWidth - RadarStatsColumnGap : width,
                StatsWidth = beside ? statsWidth : width,
                StatsBesideRadar = beside
            };
        }

        /// <summary>按语义关系组合参数；近战不足四项时补空卡，远程仍只显示真实属性。</summary>
        private static List<AttackStatCard> BuildAttackStatCards(IReadOnlyList<ChipFeatureRenderLine> lines, bool ranged)
        {
            List<AttackStatCard> result = new List<AttackStatCard>();
            HashSet<ChipFeatureRenderLine> used = new HashSet<ChipFeatureRenderLine>();
            AddAttackStatCard(result, used, lines, "BDP_Property_DamageType", "BDP_Property_EffectRadius");
            AddAttackStatCard(result, used, lines, "BDP_Property_AttackBurstCount", "BDP_Property_RangedShotRhythm");
            if (ranged)
                AddAttackStatCard(result, used, lines, "BDP_Property_WarmupDuration", "BDP_Property_ActionCooldownDuration");
            else
                AddAttackStatCard(result, used, lines, "BDP_Property_ActionCooldownDuration", "BDP_Property_WarmupDuration");
            AddAttackStatCard(result, used, lines, "BDP_Property_UseCost", "BDP_Property_SustainCost");
            foreach (ChipFeatureRenderLine line in lines)
                if (used.Add(line)) result.Add(new AttackStatCard { Primary = line });
            if (!ranged)
            {
                while (result.Count < 4)
                {
                    result.Add(new AttackStatCard());
                }
            }
            foreach (AttackStatCard card in result) card.IsRanged = ranged;
            return result;
        }

        /// <summary>只按明确的属性键配对，不按顺序或本地化后的文字猜测关系。</summary>
        private static void AddAttackStatCard(List<AttackStatCard> cards, HashSet<ChipFeatureRenderLine> used,
            IReadOnlyList<ChipFeatureRenderLine> lines, string primaryKey, string secondaryKey)
        {
            ChipFeatureRenderLine primary = null, secondary = null;
            foreach (ChipFeatureRenderLine line in lines)
            {
                if (line.Property.Key == primaryKey) primary = line;
                if (secondaryKey != null && line.Property.Key == secondaryKey) secondary = line;
            }
            if (primary == null && secondary == null) return;
            if (primary == null) { primary = secondary; secondary = null; }
            used.Add(primary);
            if (secondary != null) used.Add(secondary);
            cards.Add(new AttackStatCard { Primary = primary, Secondary = secondary });
        }

        /// <summary>攻击参数的一个展示单元；不改变信息模型。</summary>
        private sealed class AttackStatCard
        {
            /// <summary>左侧主参数；近战占位卡允许为空。</summary>
            internal ChipFeatureRenderLine Primary;
            /// <summary>右侧相关参数；无值时不绘制分隔。</summary>
            internal ChipFeatureRenderLine Secondary;
            /// <summary>所属攻击类型；远程数量用“发”，近战保持攻击次数。</summary>
            internal bool IsRanged;
        }

        /// <summary>按卡片真实文字测量，成对参数左右错位但保持可读宽度。</summary>
        private static float MeasureAttackStatTileHeight(AttackStatCard card, float width)
        {
            if (card.Primary == null)
            {
                return AttackStatTileMinimumHeight;
            }

            float primaryWidth = card.Secondary == null ? width - 15f : (width - 18f) * 0.5f - 3f;
            float height = MeasureAttackStatCell(card.Primary, primaryWidth, GameFont.Small, card.IsRanged);
            if (card.Secondary != null)
                height = Mathf.Max(height, MeasureAttackStatCell(card.Secondary, (width - 18f) * 0.5f, GameFont.Tiny, card.IsRanged) + 6f);
            return Mathf.Max(AttackStatTileMinimumHeight, height + 8f);
        }

        /// <summary>测量同一参数标签和值的总高度。</summary>
        private static float MeasureAttackStatCell(ChipFeatureRenderLine line, float width, GameFont valueFont, bool ranged)
        {
            return MeasureTextHeight(GetAttackStatLabel(line, ranged), width, GameFont.Tiny, 12f) + 2f
                + MeasureTextHeight(GetAttackStatValue(line, ranged), width, valueFont, 16f);
        }

        /// <summary>卡片使用简短玩家标签，完整语义值仍由统一格式化器提供。</summary>
        private static string GetAttackStatLabel(ChipFeatureRenderLine line, bool ranged)
        {
            string label;
            if (ranged && line.Property.Key == "BDP_Property_AttackBurstCount")
                label = "BDP_Info_ProjectileCount".Translate().ToString();
            else
                label = line.Property.Key == "BDP_Property_ActionCooldownDuration"
                    ? "BDP_Info_AttackCooldown".Translate().ToString() : line.Label;
            return line.HasChangedBaselineValue
                ? label + "BDP_Info_ChangedValueMarker".Translate().ToString()
                : label;
        }

        /// <summary>发射数量在远程卡片中标明“发”；其余参数仍由原格式化器提供单位。</summary>
        private static string GetAttackStatValue(ChipFeatureRenderLine line, bool ranged)
        {
            string value = ChipInformationTextFormatter.FormatFeatureProperty(line.Property);
            return ranged && line.Property.Key == "BDP_Property_AttackBurstCount"
                ? "BDP_Info_ProjectileCountValue".Translate(value).ToString() : value;
        }

        /// <summary>在临时字体状态下测量文字并恢复游戏原有字体。</summary>
        private static float MeasureTextHeight(
            string text,
            float width,
            GameFont font,
            float minimumHeight)
        {
            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = font;
                return Mathf.Max(minimumHeight, Text.CalcHeight(text ?? string.Empty, Mathf.Max(width, 1f)));
            }
            finally
            {
                Text.Font = oldFont;
            }
        }

        /// <summary>测量一列语义配对参数卡片的总高度。</summary>
        private static float MeasureAttackStatTilesHeight(IReadOnlyList<ChipFeatureRenderLine> lines, float width, bool ranged)
        {
            if (ranged && (lines == null || lines.Count == 0)) return 0f;
            List<AttackStatCard> cards = BuildAttackStatCards(lines ?? new List<ChipFeatureRenderLine>(), ranged);
            return cards.Count * MeasureAttackStatRowHeight(cards, width) + (cards.Count - 1) * AttackStatTileGap;
        }

        /// <summary>同列以最大真实文字高度为统一行高，副卡错位不再造成卡片间的高度差。</summary>
        private static float MeasureAttackStatRowHeight(IReadOnlyList<AttackStatCard> cards, float width)
        {
            float height = AttackStatTileMinimumHeight;
            foreach (AttackStatCard card in cards) height = Mathf.Max(height, MeasureAttackStatTileHeight(card, width));
            return height;
        }

        /// <summary>右侧参数统一纵列，在共享统计框内形成稳定的阅读次序。</summary>
        private static void DrawAttackStatTiles(Rect rect, IReadOnlyList<ChipFeatureRenderLine> lines, Color accent, bool ranged)
        {
            if (ranged && (lines == null || lines.Count == 0)) return;
            float y = rect.y;
            List<AttackStatCard> cards = BuildAttackStatCards(lines ?? new List<ChipFeatureRenderLine>(), ranged);
            float height = MeasureAttackStatRowHeight(cards, rect.width);
            foreach (AttackStatCard card in cards)
            {
                DrawAttackStatTile(new Rect(rect.x, y, rect.width, height), card, accent);
                y += height + AttackStatTileGap;
            }
        }

        /// <summary>主参数在左上、相关参数在右下，用细斜线连接同一动作的两个侧面。</summary>
        private static void DrawAttackStatTile(Rect rect, AttackStatCard card, Color accent)
        {
            // 斜线和边框使用同一不透明混合色，交点重复覆盖也不会形成深色接缝。
            Color background = new Color(0.04f, 0.11f, 0.12f, 1f);
            Color border = Color.Lerp(background, new Color(accent.r, accent.g, accent.b, 1f), 0.44f);
            Widgets.DrawBoxSolidWithOutline(rect, background, border);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 2f, rect.height), new Color(accent.r, accent.g, accent.b, 0.82f));
            if (card.Primary == null)
            {
                DrawText(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 20f, 22f),
                    "BDP_Info_EmptyAttackStat".Translate().ToString(), GameFont.Small, accent);
                return;
            }
            float primaryWidth = card.Secondary == null ? rect.width - 15f : (rect.width - 18f) * 0.5f - 3f;
            DrawAttackStatCell(new Rect(rect.x + 9f, rect.y + 4f, primaryWidth, rect.height - 8f), card.Primary, GameFont.Small, accent, card.IsRanged);
            if (card.Secondary != null)
            {
                float secondaryWidth = (rect.width - 18f) * 0.5f;
                float cellHeight = MeasureAttackStatCell(card.Secondary, secondaryWidth, GameFont.Tiny, card.IsRanged);
                float rightX = rect.xMax - secondaryWidth - 5f;
                // 固定 60°，端点落在上下边框的中心线上；相关文字靠右避让斜线。
                float horizontalRun = (rect.height - 1f) / Mathf.Sqrt(3f);
                float dividerCenter = rect.x + rect.width * 0.49f;
                Widgets.DrawLine(new Vector2(dividerCenter - horizontalRun * 0.5f, rect.yMax - 0.5f),
                    new Vector2(dividerCenter + horizontalRun * 0.5f, rect.y + 0.5f), border, 1f);
                DrawAttackStatCell(new Rect(rightX, rect.yMax - cellHeight - 4f, secondaryWidth, cellHeight),
                    card.Secondary, GameFont.Tiny, accent, card.IsRanged, TextAnchor.UpperRight);
            }
        }

        /// <summary>卡片字段保持标签弱、数值强；字体尺寸与测量完全一致。</summary>
        private static void DrawAttackStatCell(Rect rect, ChipFeatureRenderLine line, GameFont valueFont, Color accent, bool ranged,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            float labelHeight = MeasureTextHeight(GetAttackStatLabel(line, ranged), rect.width, GameFont.Tiny, 12f);
            DrawText(new Rect(rect.x, rect.y, rect.width, labelHeight), GetAttackStatLabel(line, ranged), GameFont.Tiny, BaseColor, anchor);
            DrawText(new Rect(rect.x, rect.y + labelHeight + 2f, rect.width, rect.height - labelHeight - 2f),
                GetAttackStatValue(line, ranged), valueFont, accent, anchor);
        }

        /// <summary>绘制包含标题的常规图表卡片。</summary>
        private static void DrawChartFrame(Rect rect, string titleKey)
        {
            DrawChartFrame(rect);
            DrawText(
                new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, StatisticsHeaderHeight - 2f),
                titleKey.Translate().ToString(),
                GameFont.Tiny,
                new Color(0.72f, 0.90f, 0.88f, 1f));
        }

        /// <summary>绘制无标题的雷达图卡片框。</summary>
        private static void DrawChartFrame(Rect rect)
        {
            Widgets.DrawBoxSolidWithOutline(rect, ChartBackgroundColor, ChartBorderColor);
        }

        /// <summary>按旧版轴序、固定标尺和青色双层填充绘制雷达图。</summary>
        private static void DrawRadarChart(
            Rect rect,
            ChipInformationNode feature,
            ChipInformationComparisonModel comparison,
            RadarMetric[] metrics,
            Rect visibleContentRect)
        {
            ChipInformationNode baselineFeature = FindComparableFeature(comparison?.Baseline, feature);
            List<RadarChartAxis> axes = new List<RadarChartAxis>(metrics.Length);
            List<float> currentValues = new List<float>(metrics.Length);
            List<float> baselineValues = new List<float>(metrics.Length);
            List<bool> baselineValueAvailability = new List<bool>(metrics.Length);
            bool hasAnyBaselineValue = false;
            for (int index = 0; index < metrics.Length; index++)
            {
                RadarMetric metric = metrics[index];
                axes.Add(new RadarChartAxis(
                    GetComparedRadarMetricLabel(feature, baselineFeature, metric),
                    0f,
                    metric.Maximum));
                currentValues.Add(GetMetricValue(feature, metric));
                baselineValues.Add(GetMetricValue(baselineFeature, metric));
                bool hasBaselineValue = HasRadarMetricValue(baselineFeature, metric);
                baselineValueAvailability.Add(hasBaselineValue);
                hasAnyBaselineValue |= hasBaselineValue;
            }

            List<RadarChartSeries> series = new List<RadarChartSeries>(3);
            if (hasAnyBaselineValue)
            {
                ChartSeriesStyle baselineStyle = CreateBaselineChartSeriesStyle(true);
                baselineStyle.LineColor = new Color(BaseColor.r, BaseColor.g, BaseColor.b, 0.55f);
                baselineStyle.FillOpacity = 0.07f;
                series.Add(new RadarChartSeries(
                    string.Empty,
                    baselineValues,
                    baselineStyle,
                    baselineValueAvailability));
            }

            List<float> matteValues = new List<float>(currentValues.Count);
            for (int index = 0; index < currentValues.Count; index++)
            {
                matteValues.Add(currentValues[index] * 0.94f);
            }

            series.Add(new RadarChartSeries(
                string.Empty,
                matteValues,
                new ChartSeriesStyle
                {
                    LineWidth = 0f,
                    Filled = true,
                    FillColor = new Color(0.34f, 0.68f, 0.67f, 1f),
                    FillOpacity = 0.05f
                }));
            ChartSeriesStyle currentStyle = CreateCurrentChartSeriesStyle(true);
            currentStyle.FillOpacity = 0.18f;
            series.Add(new RadarChartSeries(
                string.Empty,
                currentValues,
                currentStyle));

            ChartStyle style = CreateDetailChartStyle();
            style.RadarAxisValuesBelowLabels = true;
            style.RadarAxisLabelValueGap = -3f;
            style.RadarGeometryHeight = RadarPlotHeight;
            style.ShowRadarDecreaseSegments = true;
            style.RadarDecreaseColor = new Color(DecreaseColor.r, DecreaseColor.g, DecreaseColor.b, 0.55f);
            style.GridColor = new Color(ChartGridColor.r, ChartGridColor.g, ChartGridColor.b, 0.32f);
            style.LabelFont = GameFont.Small;
            style.LabelColor = RadarLabelColor;
            style.RadarAxisValueFormatter = (index, value) => FormatRadarMetricValue(metrics[index], value);
            style.RadarAxisValueColorProvider = (index, value) =>
                value > metrics[index].Maximum ? WarningColor : AccentColor;
            RadarChartRenderer.Draw(rect, visibleContentRect, new RadarChartData(axes, series), style);
        }

        /// <summary>按旧版距离轴、插值曲线和锚点标记绘制精度折线。</summary>
        private static void DrawAccuracyChart(
            Rect rect,
            ChipInformationNode feature,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect)
        {
            string[] keys =
            {
                "BDP_Property_AccuracyTouch",
                "BDP_Property_AccuracyShort",
                "BDP_Property_AccuracyMedium",
                "BDP_Property_AccuracyLong"
            };
            float[] anchorDistances = new[] { 3f, 12f, 25f, 40f };
            float[] xTicks = new[] { 0f, 3f, 12f, 25f, 40f, 50f };
            ChipInformationNode baselineFeature = FindComparableFeature(comparison?.Baseline, feature);
            List<float> currentAnchors = new List<float>(keys.Length);
            List<float> baselineAnchors = new List<float>(keys.Length);
            List<bool> baselineAnchorsPresent = new List<bool>(keys.Length);
            for (int index = 0; index < keys.Length; index++)
            {
                currentAnchors.Add(Mathf.Clamp01(NormalizePercentageRatio(GetNumber(feature, keys[index]))));
                baselineAnchors.Add(Mathf.Clamp01(NormalizePercentageRatio(GetNumber(baselineFeature, keys[index]))));
                baselineAnchorsPresent.Add(HasNumber(baselineFeature, keys[index]));
            }

            List<LineChartSeries> series = new List<LineChartSeries>(2);
            if (HasAccuracy(baselineFeature))
            {
                series.Add(new LineChartSeries(
                    string.Empty,
                    BuildAccuracyCurve(baselineAnchors, anchorDistances),
                    CreateAccuracySeriesStyle(true)));
            }

            series.Add(new LineChartSeries(
                string.Empty,
                BuildAccuracyCurve(currentAnchors, anchorDistances),
                CreateAccuracySeriesStyle(false)));
            LineChartData data = new LineChartData(
                new ChartRange(0f, 50f),
                new ChartRange(0f, 1f),
                series,
                xTicks,
                new[] { 0f, 0.5f, 1f });
            ChartStyle style = CreateDetailChartStyle();
            style.ValueFormatter = value => (value * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
            style.XValueFormatter = value => value.ToString("0", CultureInfo.InvariantCulture);
            style.PlotTop = 14f;
            style.PlotRight = 16f;
            style.PlotBottom = 22f;
            style.LineChartBottomOverflow = 1f;
            style.ShowXGridLines = false;
            style.XTickLabelWidth = 24f;
            style.XTickLabelHeight = 16f;
            style.YTickLabelWidth = 34f;
            style.YTickLabelHeight = 16f;
            LineChartRenderer.Draw(
                rect,
                visibleContentRect,
                data,
                style);
            DrawAccuracyAnchorLabels(
                rect,
                currentAnchors,
                anchorDistances,
                baselineAnchorsPresent,
                HasAccuracy(baselineFeature) ? baselineAnchors : null,
                style,
                visibleContentRect);
        }

        /// <summary>沿距离轴生成旧版 0 到 50 格的 51 个线性插值点。</summary>
        private static List<ChartPoint> BuildAccuracyCurve(
            IReadOnlyList<float> anchors,
            IReadOnlyList<float> distances)
        {
            List<ChartPoint> points = new List<ChartPoint>(51);
            for (int distance = 0; distance <= 50; distance++)
            {
                points.Add(new ChartPoint(distance, InterpolateAccuracy(anchors, distances, distance)));
            }

            return points;
        }

        /// <summary>按 3、12、25、40 格锚点分段插值并在两端保持平台。</summary>
        private static float InterpolateAccuracy(
            IReadOnlyList<float> anchors,
            IReadOnlyList<float> distances,
            float distance)
        {
            if (distance <= distances[0])
            {
                return anchors[0];
            }

            for (int index = 1; index < distances.Count; index++)
            {
                if (distance <= distances[index])
                {
                    float ratio = (distance - distances[index - 1])
                        / (distances[index] - distances[index - 1]);
                    return Mathf.Lerp(anchors[index - 1], anchors[index], ratio);
                }
            }

            return anchors[anchors.Count - 1];
        }

        /// <summary>在原版距离锚点绘制当前精度圆点和百分比文字。</summary>
        private static void DrawAccuracyAnchorLabels(
            Rect rect,
            IReadOnlyList<float> values,
            IReadOnlyList<float> distances,
            IReadOnlyList<bool> baselinePresent,
            IReadOnlyList<float> baselineValues,
            ChartStyle style,
            Rect visibleContentRect)
        {
            Rect plotRect = new Rect(
                style.PlotLeft,
                style.PlotTop,
                Mathf.Max(1f, rect.width - style.PlotLeft - style.PlotRight),
                Mathf.Max(1f, rect.height - style.PlotTop - style.PlotBottom));
            ChartViewport viewport = new ChartViewport(
                plotRect,
                new ChartRange(0f, 50f),
                new ChartRange(0f, 1f));
            new ChartCanvas(rect, visibleContentRect).Draw(context =>
            {
                for (int index = 0; index < values.Count; index++)
                {
                    Vector2 point;
                    if (!viewport.TryMap(new ChartPoint(distances[index], values[index]), out point))
                    {
                        continue;
                    }

                    Color color = baselinePresent[index] ? AccentColor : BaseColor;
                    context.DrawCircle(point, 2.8f, color);
                    // 标签贴近自己的锚点，并避开当前线、基准线和水平刻度线。
                    string label = (values[index] * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    Rect labelRect = FindAccuracyLabelRect(point, label, values, baselineValues, distances, viewport);
                    context.DrawText(
                        labelRect,
                        label,
                        GameFont.Tiny,
                        color,
                        TextAnchor.MiddleCenter);
                }
            });
        }

        /// <summary>优先在锚点上下的最近位置放百分比；不盖住曲线，也不挤入横坐标刻度区。</summary>
        private static Rect FindAccuracyLabelRect(Vector2 point, string label, IReadOnlyList<float> values,
            IReadOnlyList<float> baselineValues, IReadOnlyList<float> distances, ChartViewport viewport)
        {
            Rect plot = viewport.PlotRect;
            Vector2 size;
            GameFont previousFont = Text.Font;
            try
            {
                Text.Font = GameFont.Tiny;
                size = Text.CalcSize(label) + new Vector2(4f, 2f);
            }
            finally { Text.Font = previousFont; }
            Rect fallback = new Rect(point.x - size.x * 0.5f,
                Mathf.Clamp(point.y - size.y - 5f, 2f, plot.yMax - size.y - 3f), size.x, size.y);
            for (int step = 0; step < 8; step++)
            {
                for (int side = 0; side < 2; side++)
                {
                    float y = side == 0 ? point.y - size.y - 5f - step * 4f : point.y + 7f + step * 4f;
                    Rect candidate = new Rect(point.x - size.x * 0.5f, y, size.x, size.y);
                    if (candidate.y < 2f || candidate.yMax > plot.yMax - 3f
                        || AccuracyLabelTouchesCurve(candidate, values, distances, viewport)
                        || AccuracyLabelTouchesCurve(candidate, baselineValues, distances, viewport)) continue;
                    bool touchesGrid = false;
                    for (int tick = 0; tick <= 2; tick++)
                    {
                        float gridY = Mathf.Lerp(plot.yMax, plot.y, tick * 0.5f);
                        touchesGrid |= gridY >= candidate.y - 2f && gridY <= candidate.yMax + 2f;
                    }
                    if (!touchesGrid) return candidate;
                }
            }
            return fallback;
        }

        /// <summary>按精度曲线的真实线性分段检查文字矩形，包含两端的平台。</summary>
        private static bool AccuracyLabelTouchesCurve(Rect label, IReadOnlyList<float> values,
            IReadOnlyList<float> distances, ChartViewport viewport)
        {
            if (values == null) return false;
            Rect padded = label.ExpandedBy(2f);
            for (int segment = 0; segment <= values.Count; segment++)
            {
                int start = Mathf.Clamp(segment - 1, 0, values.Count - 1);
                int end = Mathf.Clamp(segment, 0, values.Count - 1);
                Vector2 a, b;
                viewport.TryMap(new ChartPoint(segment == 0 ? 0f : distances[start], values[start]), out a);
                viewport.TryMap(new ChartPoint(segment == values.Count ? 50f : distances[end], values[end]), out b);
                float left = Mathf.Max(a.x, padded.x), right = Mathf.Min(b.x, padded.xMax);
                if (left > right || b.x <= a.x) continue;
                float y1 = Mathf.Lerp(a.y, b.y, (left - a.x) / (b.x - a.x));
                float y2 = Mathf.Lerp(a.y, b.y, (right - a.x) / (b.x - a.x));
                if (Mathf.Max(y1, y2) >= padded.y && Mathf.Min(y1, y2) <= padded.yMax) return true;
            }
            return false;
        }

        /// <summary>创建旧版远程精度主线或基准虚线样式。</summary>
        private static ChartSeriesStyle CreateAccuracySeriesStyle(bool baseline)
        {
            return new ChartSeriesStyle
            {
                LineColor = baseline ? BaseColor : AccentColor,
                LineWidth = baseline ? 0.9f : 1.2f,
                Dashed = baseline,
                ShowPoints = false,
                ShowValues = false
            };
        }

        /// <summary>创建详情面板使用的通用图表样式。</summary>
        private static ChartStyle CreateDetailChartStyle()
        {
            return new ChartStyle
            {
                BackgroundColor = new Color(0f, 0f, 0f, 0f),
                BorderColor = new Color(0f, 0f, 0f, 0f),
                GridColor = ChartGridColor,
                AxisColor = ChartGridColor,
                LabelColor = DecreaseColor,
                ValueColor = AccentColor,
                LabelFont = GameFont.Tiny,
                LabelAnchor = TextAnchor.MiddleCenter,
                GridLevels = 4
            };
        }

        /// <summary>创建当前值序列的统一样式。</summary>
        private static ChartSeriesStyle CreateCurrentChartSeriesStyle(bool filled)
        {
            return new ChartSeriesStyle
            {
                LineColor = AccentColor,
                LineWidth = 1.2f,
                Filled = filled,
                FillColor = new Color(0.18f, 0.55f, 0.56f, 1f),
                FillOpacity = 0.24f,
                ShowPoints = false,
                PointRadius = 2.8f,
                PointColor = AccentColor,
                ShowValues = true,
                ValueColor = AccentColor
            };
        }

        /// <summary>创建基准值序列的统一样式。</summary>
        private static ChartSeriesStyle CreateBaselineChartSeriesStyle(bool filled)
        {
            return new ChartSeriesStyle
            {
                LineColor = BaseColor,
                LineWidth = 0.9f,
                Dashed = true,
                Filled = filled,
                FillColor = new Color(0.28f, 0.46f, 0.47f, 1f),
                FillOpacity = 0.14f,
                ShowPoints = false,
                ShowValues = false,
                ValueColor = BaseColor
            };
        }

        /// <summary>使用公共圆轮廓和扇区入口绘制护盾覆盖示意，不在业务页重复采样几何。</summary>
        private static void DrawBlockArc(
            Rect rect,
            ChipInformationNode feature,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect)
        {
            ChipInformationNode baseline = FindComparableFeature(comparison?.Baseline, feature);
            float current = Mathf.Clamp(GetNumber(feature, "BDP_Property_BlockArc"), 0f, 360f);
            float baselineValue = Mathf.Clamp(GetNumber(baseline, "BDP_Property_BlockArc"), 0f, 360f);
            bool changed = baseline != null && !Mathf.Approximately(current, baselineValue);
            Color coverageColor = changed ? IncreaseColor : AccentColor;
            Vector2 center = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            float radius = Mathf.Min(rect.width, rect.height) * 0.38f;
            ChipInformationProperty angleProperty = FindProperty(feature, "BDP_Property_BlockArc");
            string angleValue = angleProperty == null
                ? "BDP_Info_Chart_BlockArcValue".Translate(current).ToString()
                : ChipInformationTextFormatter.FormatFeatureProperty(angleProperty);
            string angleLabel = GetPropertyLabel(feature, "BDP_Property_BlockArc");
            if (HasChangedDisplayedValue(angleProperty, FindProperty(baseline, "BDP_Property_BlockArc")))
            {
                angleLabel += "BDP_Info_ChangedValueMarker".Translate();
            }

            Widgets.DrawBoxSolidWithOutline(rect, ChartBackgroundColor, ChartBorderColor);

            new ChartCanvas(rect, visibleContentRect).Draw(context =>
            {
                context.DrawCircleOutline(center, radius, ChartGridColor, 1f, 36);
                context.DrawSector(center, radius, -90f - current * 0.5f, -90f + current * 0.5f,
                    new Color(coverageColor.r, coverageColor.g, coverageColor.b, 0.22f));
                context.DrawLine(
                    center,
                    CoveragePoint(center, radius, -current * 0.5f),
                    coverageColor,
                    1.5f);
                context.DrawLine(
                    center,
                    CoveragePoint(center, radius, current * 0.5f),
                    coverageColor,
                    1.5f);
                context.DrawText(
                    new Rect(6f, center.y - 18f, rect.width - 12f, 20f),
                    angleValue,
                    GameFont.Small,
                    Color.white,
                    TextAnchor.MiddleCenter);
                context.DrawText(
                    new Rect(6f, center.y + 5f, rect.width - 12f, 16f),
                    angleLabel,
                    GameFont.Tiny,
                    BaseColor,
                    TextAnchor.MiddleCenter);
            });
        }

        /// <summary>把正北为零度的角度映射为图表局部坐标点。</summary>
        private static Vector2 CoveragePoint(Vector2 center, float radius, float degrees)
        {
            float radians = (-90f + degrees) * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        /// <summary>
        /// 绘制技能距离短弧图：左侧原点代表使用者，向右外凸的短弧标出最小/最大距离。
        /// 辅助中轴保持极淡，只有主弧是强调元素；弧越远越高，形成温和的信号波层次。
        /// </summary>
        private static void DrawCastRangeArc(
            Rect rect,
            ChipInformationNode feature,
            ChipInformationComparisonModel comparison,
            Rect visibleContentRect)
        {
            ChipInformationNode baseline = FindComparableFeature(comparison?.Baseline, feature);
            ChipInformationProperty maxProperty = FindProperty(feature, "BDP_Property_CastRange");
            ChipInformationProperty minProperty = FindProperty(feature, "BDP_Property_MinimumCastRange");
            float currentMax = GetNumber(feature, "BDP_Property_CastRange");
            float currentMin = Mathf.Clamp(GetNumber(feature, "BDP_Property_MinimumCastRange"), 0f, currentMax);
            bool hasBaseMax = HasNumber(baseline, "BDP_Property_CastRange");
            float baseMax = GetNumber(baseline, "BDP_Property_CastRange");
            float baseMin = GetNumber(baseline, "BDP_Property_MinimumCastRange");
            bool maxChanged = hasBaseMax && !Mathf.Approximately(currentMax, baseMax);
            bool minChanged = baseline != null && !Mathf.Approximately(currentMin, baseMin);

            // 横向比例：较大的那条最大弧顶点贴近右侧，留出半个标签宽度。
            float plotWidth = Mathf.Max(40f, rect.width - CastRangeArcOriginX - CastRangeArcLabelWidth * 0.5f - 4f);
            float scaleMax = Mathf.Max(currentMax, hasBaseMax ? baseMax : 0f, 0.01f);
            // 最小弧离原点至少 18px，避免短距离时贴住原点。
            float ToX(float distance) => distance <= 0f
                ? CastRangeArcOriginX
                : CastRangeArcOriginX + Mathf.Max(18f, distance / scaleMax * plotWidth);

            DrawChartFrame(rect);
            new ChartCanvas(rect, visibleContentRect).Draw(context =>
            {
                Vector2 origin = new Vector2(CastRangeArcOriginX, CastRangeArcAxisY);
                float maxX = ToX(currentMax);
                float minX = currentMin > 0f ? ToX(currentMin) : CastRangeArcOriginX;

                // 1. 极淡发丝中轴：全长几乎隐没，有效区间稍亮一点。
                context.DrawLine(origin, new Vector2(rect.width - 8f, CastRangeArcAxisY), WithAlpha(AccentColor, 0.10f), 0.6f);
                context.DrawLine(new Vector2(minX, CastRangeArcAxisY), new Vector2(maxX, CastRangeArcAxisY), WithAlpha(AccentColor, 0.32f), 0.6f);

                // 2. 原点（使用者）。
                context.DrawCircle(origin, 2.2f, WithAlpha(Color.white, 0.85f));

                // 3. 对比基准：灰色虚线弧先画，避免压住当前弧。
                if (maxChanged)
                {
                    DrawRangeArcStroke(context, ToX(baseMax), plotWidth, WithAlpha(DecreaseColor, 0.8f), 1f, true);
                }

                if (minChanged && baseMin > 0f)
                {
                    DrawRangeArcStroke(context, ToX(baseMin), plotWidth, WithAlpha(DecreaseColor, 0.8f), 1f, true);
                }

                // 4. 当前主弧：与基准不同时使用增减强调色。
                if (currentMin > 0f)
                {
                    DrawRangeArcStroke(context, minX, plotWidth, minChanged ? IncreaseColor : AccentColor, 1.5f, false);
                }

                DrawRangeArcStroke(context, maxX, plotWidth, maxChanged ? IncreaseColor : AccentColor, 1.6f, false);

                // 5. 数值标签：放在弧顶点正下方；重叠时向左让位，基准标签放不下则省略。
                float labelY = rect.height - 17f;
                Rect maxLabel = RangeArcLabelRect(maxX, labelY, rect.width);
                context.DrawText(maxLabel, ChipInformationTextFormatter.FormatFeatureProperty(maxProperty),
                    GameFont.Tiny, Color.white, TextAnchor.MiddleCenter);
                List<Rect> placed = new List<Rect> { maxLabel };
                if (currentMin > 0f)
                {
                    Rect minLabel = RangeArcLabelRect(minX, labelY, rect.width);
                    if (minLabel.xMax > maxLabel.x - 2f)
                    {
                        minLabel.x = maxLabel.x - 2f - minLabel.width;
                    }

                    context.DrawText(minLabel, ChipInformationTextFormatter.FormatFeatureProperty(minProperty),
                        GameFont.Tiny, Color.white, TextAnchor.MiddleCenter);
                    placed.Add(minLabel);
                }

                if (maxChanged)
                {
                    Rect baseLabel = RangeArcLabelRect(ToX(baseMax), labelY, rect.width);
                    if (!placed.Exists(other => other.Overlaps(baseLabel)))
                    {
                        context.DrawText(baseLabel,
                            ChipInformationTextFormatter.FormatFeatureProperty(FindProperty(baseline, "BDP_Property_CastRange")),
                            GameFont.Tiny, WithAlpha(DecreaseColor, 0.9f), TextAnchor.MiddleCenter);
                    }
                }
            });

            // 文本行已被图形取代，悬停提示保留属性名称，玩家仍能读到“有效距离/最小距离”。
            string tooltip = GetFeatureLineText(new ChipFeatureRenderLine(maxProperty, GetPropertyLabel(feature, "BDP_Property_CastRange"), maxChanged));
            if (currentMin > 0f)
            {
                tooltip += "\n" + GetFeatureLineText(new ChipFeatureRenderLine(minProperty, GetPropertyLabel(feature, "BDP_Property_MinimumCastRange"), minChanged));
            }

            TooltipHandler.TipRegion(rect, tooltip);
        }

        /// <summary>
        /// 绘制一段顶点落在 tipX 的外凸短弧。按顶点在绘图宽度中的占比温和放大：
        /// 半弦高 7→12px、弓高 1.8→2.8px，越远越高，但不形成大扇形。
        /// </summary>
        private static void DrawRangeArcStroke(
            ChartDrawingContext context,
            float tipX,
            float plotWidth,
            Color color,
            float width,
            bool dashed)
        {
            float t = Mathf.Clamp01((tipX - CastRangeArcOriginX) / plotWidth);
            float halfChord = 7f + 5f * t;
            float sagitta = 1.8f + 1.0f * t;
            float radius = (halfChord * halfChord + sagitta * sagitta) / (2f * sagitta);
            float halfAngle = Mathf.Asin(Mathf.Clamp01(halfChord / radius)) * Mathf.Rad2Deg;
            context.DrawArc(
                new Vector2(tipX - radius, CastRangeArcAxisY),
                radius,
                -halfAngle,
                halfAngle,
                color,
                width,
                dashed);
        }

        /// <summary>以弧顶点为中心计算标签框，并限制在卡片内。</summary>
        private static Rect RangeArcLabelRect(float centerX, float y, float cardWidth)
        {
            float x = Mathf.Clamp(centerX - CastRangeArcLabelWidth * 0.5f, 2f, cardWidth - CastRangeArcLabelWidth - 2f);
            return new Rect(x, y, CastRangeArcLabelWidth, 16f);
        }

        /// <summary>返回替换透明度后的颜色。</summary>
        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>把雷达百分比指标格式化成旧版百分比文本。</summary>
        private static string FormatRadarMetricValue(RadarMetric metric, float value)
        {
            float displayValue = metric?.Key == "BDP_Property_ArmorPenetration"
                ? value * 100f
                : value;
            string formatted = displayValue.ToString("0.#", CultureInfo.InvariantCulture);
            return metric?.Key == "BDP_Property_ArmorPenetration"
                ? formatted + "%"
                : formatted;
        }

        /// <summary>固定轴的雷达指标定义。</summary>
        private sealed class RadarMetric
        {
            internal readonly string Key;
            internal readonly string LabelKey;
            internal readonly float Maximum;

            internal RadarMetric(string key, string labelKey, float maximum)
            {
                Key = key;
                LabelKey = labelKey;
                Maximum = maximum;
            }
        }

        /// <summary>概览显示行。</summary>
        private sealed class OverviewLine
        {
            /// <summary>玩家向字段标签。</summary>
            internal readonly string Label;
            /// <summary>已经本地化、带单位的字段值。</summary>
            internal readonly string Value;

            /// <summary>创建详情显示行。</summary>
            internal OverviewLine(string label, string value)
            {
                Label = label;
                Value = value;
            }
        }

        /// <summary>一条功能模板的本地化描述。</summary>
        private sealed class OverviewDescription
        {
            /// <summary>功能模板名称，单条描述时省略重复显示。</summary>
            internal readonly string Label;
            /// <summary>功能模板提供的本地化正文。</summary>
            internal readonly string Text;

            /// <summary>创建功能描述。</summary>
            internal OverviewDescription(string label, string text)
            {
                Label = label ?? string.Empty;
                Text = text ?? string.Empty;
            }
        }

        /// <summary>一条可合并的激活要求。</summary>
        private sealed class ActivationRequirementLine
        {
            /// <summary>条件的玩家标签。</summary>
            internal string Label;
            /// <summary>条件引用的本地化对象名称。</summary>
            internal string Reference;
            /// <summary>激活门槛及其单位。</summary>
            internal string Required;
            /// <summary>技能名称已放入标签，无需在数值中再重复。</summary>
            internal bool IsSkill;
        }

        /// <summary>组合关系的玩家向声明行。</summary>
        private sealed class RelationLine
        {
            internal readonly string Name;
            internal readonly string ExpansionKey;
            internal readonly IReadOnlyList<ChipFeatureRenderResult> Features;
            internal readonly ChipInformationComparisonModel Comparison;

            internal RelationLine(
                string name,
                string expansionKey,
                IReadOnlyList<ChipFeatureRenderResult> features,
                ChipInformationComparisonModel comparison)
            {
                Name = name;
                ExpansionKey = expansionKey ?? string.Empty;
                Features = features ?? new List<ChipFeatureRenderResult>().AsReadOnly();
                Comparison = comparison;
            }
        }

        /// <summary>读取当前功能是否有四距离精度值。</summary>
        private static bool HasAccuracy(ChipInformationNode feature)
        {
            return HasNumber(feature, "BDP_Property_AccuracyTouch")
                && HasNumber(feature, "BDP_Property_AccuracyShort")
                && HasNumber(feature, "BDP_Property_AccuracyMedium")
                && HasNumber(feature, "BDP_Property_AccuracyLong");
        }

        /// <summary>
        /// 判断节点是否绘制技能距离短弧图：有正的有效距离即可。
        /// 远程攻击与近战攻击已有雷达/精度图，调用方先行分流，不会走到这里。
        /// </summary>
        private static bool HasCastRangeArc(ChipInformationNode feature)
        {
            return HasNumber(feature, "BDP_Property_CastRange")
                && GetNumber(feature, "BDP_Property_CastRange") > 0f;
        }

        /// <summary>读取当前功能是否声明护盾抵挡角度。</summary>
        private static bool HasBlockArc(ChipInformationNode feature)
        {
            return HasNumber(feature, "BDP_Property_BlockArc");
        }

        /// <summary>读取同一功能在比较模型中的对应节点。</summary>
        private static ChipInformationNode FindComparableFeature(
            ChipInformationModel baseline,
            ChipInformationNode current)
        {
            if (baseline?.Root == null || current == null)
            {
                return null;
            }

            ChipInformationNode result = FindNodeByKey(baseline.Root, current.Key);
            if (result != null)
            {
                return result;
            }

            return FindNodeByDefinition(baseline.Root, current.DefinitionKey, current.LabelKey);
        }

        /// <summary>按稳定节点键递归查找信息节点。</summary>
        private static ChipInformationNode FindNodeByKey(
            ChipInformationNode node,
            string key)
        {
            if (node == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (string.Equals(node.Key, key, StringComparison.Ordinal))
            {
                return node;
            }

            if (node.Children == null)
            {
                return null;
            }

            for (int index = 0; index < node.Children.Count; index++)
            {
                ChipInformationNode result = FindNodeByKey(node.Children[index], key);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>按定义及显示标签递归查找信息节点。</summary>
        private static ChipInformationNode FindNodeByDefinition(
            ChipInformationNode node,
            string definitionKey,
            string labelKey)
        {
            if (node == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(definitionKey)
                && string.Equals(node.DefinitionKey, definitionKey, StringComparison.Ordinal)
                && (string.IsNullOrWhiteSpace(labelKey)
                    || string.Equals(node.LabelKey, labelKey, StringComparison.Ordinal)))
            {
                return node;
            }

            if (node.Children == null)
            {
                return null;
            }

            for (int index = 0; index < node.Children.Count; index++)
            {
                ChipInformationNode result = FindNodeByDefinition(node.Children[index], definitionKey, labelKey);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>读取功能直属数值属性。</summary>
        private static float GetNumber(ChipInformationNode feature, string key)
        {
            ChipInformationProperty property = FindProperty(feature, key);
            if (property?.Value == null)
            {
            return 0f;
        }

            switch (property.ValueKind)
            {
                case ChipInformationValueKind.Integer:
                    return property.Value.Integer;
                case ChipInformationValueKind.Number:
                case ChipInformationValueKind.Percentage:
                case ChipInformationValueKind.Duration:
                    return property.Value.Number;
                default:
                    return 0f;
            }
        }

        /// <summary>判断功能是否存在可用于图形的数值属性。</summary>
        private static bool HasNumber(ChipInformationNode feature, string key)
        {
            ChipInformationProperty property = FindProperty(feature, key);
            return property?.Value != null
                && (property.ValueKind == ChipInformationValueKind.Integer
                    || property.ValueKind == ChipInformationValueKind.Number
                    || property.ValueKind == ChipInformationValueKind.Percentage
                    || property.ValueKind == ChipInformationValueKind.Duration);
        }

        /// <summary>读取功能直属属性。</summary>
        private static ChipInformationProperty FindProperty(
            ChipInformationNode feature,
            string key)
        {
            if (feature?.Properties == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            for (int index = 0; index < feature.Properties.Count; index++)
            {
                ChipInformationProperty property = feature.Properties[index];
                if (string.Equals(property?.Key, key, StringComparison.Ordinal))
                {
                    return property;
                }
            }

            return null;
        }

        /// <summary>读取功能属性的本地化标签，缺少标签时返回空字符串。</summary>
        private static string GetPropertyLabel(ChipInformationNode feature, string key)
        {
            ChipInformationProperty property = FindProperty(feature, key);
            ChipFeaturePropertyDef definition = key.NullOrEmpty()
                ? null
                : DefDatabase<ChipFeaturePropertyDef>.GetNamedSilentFail(key);
            string definitionLabel = definition?.LabelCap.ToString();
            if (!definitionLabel.NullOrEmpty())
            {
                return definitionLabel;
            }

            if (property != null && !property.LabelKey.NullOrEmpty())
            {
                return property.LabelKey.Translate().ToString();
            }

            return key == "RPM" || key == "APM"
                ? key.Translate().ToString()
                : string.Empty;
        }

        /// <summary>雷达轴在测量和绘制时使用同一个差异标记标签。</summary>
        private static string GetComparedRadarMetricLabel(
            ChipInformationNode feature,
            ChipInformationNode baselineFeature,
            RadarMetric metric)
        {
            string label = metric?.LabelKey.NullOrEmpty() == false
                ? metric.LabelKey.Translate().ToString()
                : string.Empty;
            if (baselineFeature != null && HasRadarMetricValue(baselineFeature, metric)
                && !string.Equals(
                    FormatRadarMetricValue(metric, GetMetricValue(feature, metric)),
                    FormatRadarMetricValue(metric, GetMetricValue(baselineFeature, metric)),
                    StringComparison.Ordinal))
            {
                label += "BDP_Info_ChangedValueMarker".Translate();
            }

            return label;
        }

        /// <summary>读取雷达指标的原始值。</summary>
        private static float GetMetricValue(ChipInformationNode feature, RadarMetric metric)
        {
            if (metric == null)
            {
                return 0f;
            }

            if (metric.Key == "RPM")
            {
                return GetAttackRate(feature, false);
            }

            if (metric.Key == "APM")
            {
                return GetAttackRate(feature, true);
            }

            float value = GetNumber(feature, metric.Key);
            return metric.Key == "BDP_Property_ArmorPenetration"
                ? NormalizePercentageRatio(value)
                : value;
        }

        /// <summary>把 0 到 1 比例或 0 到 100 百分数统一为比例。</summary>
        private static float NormalizePercentageRatio(float value)
        {
            return Mathf.Abs(value) <= 1.5f ? value : value / 100f;
        }

        /// <summary>读取远程或近战的理论频率。</summary>
        private static float GetAttackRate(ChipInformationNode feature, bool melee)
        {
            float burstCount = GetNumber(feature, "BDP_Property_AttackBurstCount");
            if (burstCount <= 0f)
            {
                burstCount = 1f;
            }

            float warmup = GetNumber(feature, "BDP_Property_WarmupDuration");
            float cooldown = GetNumber(feature, "BDP_Property_ActionCooldownDuration");
            float interval = GetNumber(feature, "BDP_Property_AttackBurstInterval");
            bool simultaneous = IsSimultaneousRhythm(feature);
            float cycle = warmup + cooldown + (simultaneous ? 0f : Mathf.Max(0f, burstCount - 1f) * interval);
            if (cycle <= 0f)
            {
                return 0f;
            }

            return burstCount * 60f / cycle;
        }

        /// <summary>判断齐射节奏；没有明确齐射值时按连续间隔处理。</summary>
        private static bool IsSimultaneousRhythm(ChipInformationNode feature)
        {
            ChipInformationProperty property = FindProperty(feature, "BDP_Property_RangedShotRhythm");
            string value = property?.Value?.EnumKey ?? property?.Value?.Text;
            return !string.IsNullOrWhiteSpace(value)
                && (value.IndexOf("simultaneous", StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("齐射", StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("volley", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>测量语义属性行。</summary>
        private static float FeatureLineHeight(ChipFeatureRenderLine line, float width)
        {
            return Mathf.Max(18f, Text.CalcHeight(GetFeatureLineText(line), Mathf.Max(width, 80f)));
        }

        /// <summary>测量一般字段行。</summary>
        private static float GeneralFieldHeight(ChipInformationProperty field, float width)
        {
            return Mathf.Max(18f, Text.CalcHeight(GetFieldText(field), Mathf.Max(width, 80f)));
        }

        /// <summary>格式化语义属性行。</summary>
        private static string GetFeatureLineText(ChipFeatureRenderLine line)
        {
            string value = ChipInformationTextFormatter.FormatFeatureProperty(line?.Property);
            return GetComparedLineLabel(line)
                + "："
                + (value.NullOrEmpty() ? value : value.Colorize(AccentColor));
        }

        /// <summary>格式化一般字段行。</summary>
        private static string GetFieldText(ChipInformationProperty field)
        {
            if (field == null)
            {
                return string.Empty;
            }

            string label = field.LabelKey.NullOrEmpty()
                ? string.Empty
                : field.LabelKey.Translate().ToString();
            if (field.Value == null || label.NullOrEmpty())
            {
                return string.Empty;
            }

            string value = ChipInformationTextFormatter.FormatValueWithUnit(
                field.Value,
                field.ValueKind,
                field.UnitKey,
                CultureInfo.InvariantCulture);

            return label + "：" + (value.NullOrEmpty() ? value : value.Colorize(AccentColor));
        }

        /// <summary>读取上下文玩家向名称。</summary>
        private static string GetContextLabel(ChipInformationNode context)
        {
            return context == null
                ? string.Empty
                : context.LabelKey.NullOrEmpty()
                    ? string.Empty
                    : context.LabelKey.Translate().ToString();
        }

        /// <summary>读取功能玩家向名称。</summary>
        private static string GetFeatureLabel(ChipInformationNode feature)
        {
            ChipFeatureDef definition = string.IsNullOrWhiteSpace(feature?.DefinitionKey)
                ? null
                : DefDatabase<ChipFeatureDef>.GetNamedSilentFail(feature.DefinitionKey);
            string label = definition?.LabelCap.ToString();
            if (feature?.DefinitionKey == "BDP_Feature_RangedAttack" && !label.NullOrEmpty())
            {
                // 标题只读模型发布的投射物身份，名称沿用定义本地化，不从伤害等参数猜测。
                ChipInformationProperty projectile = FindProperty(feature, "BDP_Property_Projectile");
                string projectileLabel = ChipInformationTextFormatter.FormatFeatureProperty(projectile);
                if (!projectileLabel.NullOrEmpty())
                {
                    return "BDP_Info_FeatureInstanceTitle".Translate(label, projectileLabel).ToString();
                }
            }
            if (!string.IsNullOrWhiteSpace(feature?.LabelKey))
            {
                string instanceLabel = feature.LabelKey.Translate().ToString();
                // 只消费节点已经声明的具体名称；没有名称时保留功能名，不从属性值猜投射物类型。
                return feature.DefinitionKey == "BDP_Feature_RangedAttack" && !label.NullOrEmpty()
                    && !string.Equals(label, instanceLabel, StringComparison.Ordinal)
                    ? "BDP_Info_FeatureInstanceTitle".Translate(label, instanceLabel).ToString()
                    : instanceLabel;
            }
            return !label.NullOrEmpty() ? label : string.Empty;
        }

        /// <summary>绘制无框轻量小标题；用于区分区块但不与功能卡片争夺视觉权重。</summary>
        private static void DrawCaption(Rect rect, string titleKey)
        {
            DrawText(
                new Rect(rect.x, rect.y, rect.width, 18f),
                titleKey.NullOrEmpty() ? string.Empty : titleKey.Translate().ToString(),
                GameFont.Tiny,
                new Color(0.55f, 0.72f, 0.70f, 1f));
        }

        /// <summary>统一绘制文字并恢复全局 GUI 状态。</summary>
        private static void DrawText(
            Rect rect,
            string text,
            GameFont font,
            Color color,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                Text.Font = font;
                GUI.color = color;
                Text.Anchor = anchor;
                Widgets.Label(rect, text ?? string.Empty);
            }
            finally
            {
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
                Text.Font = oldFont;
            }
        }
    }
}
