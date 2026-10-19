using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.UI;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Core.Chips;
using RimWorld;
using UnityEngine;
using Verse;
using VerseUI = Verse.UI;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>由触发器制造台命令打开的三栏制造窗口。</summary>
    public sealed class Window_ChipManufacturing : Window
    {
        /// <summary>目标宽度；实际尺寸不会超过当前 UI 屏幕。</summary>
        private const float TargetWidth = 1080f;

        /// <summary>目标高度；实际尺寸不会超过当前 UI 屏幕。</summary>
        private const float TargetHeight = 680f;

        /// <summary>屏幕边缘与窗口之间保留的安全空隙。</summary>
        private const float ScreenMargin = 80f;

        /// <summary>顶部主分类按钮高度。</summary>
        private const float CategoryTabsHeight = 32f;

        /// <summary>职业路径按钮高度。</summary>
        private const float ProfessionTabsHeight = 28f;

        /// <summary>三栏之间的固定空隙。</summary>
        private const float ColumnGap = 8f;

        /// <summary>左栏占主体宽度的比例。</summary>
        private const float LeftColumnRatio = 0.25f;

        /// <summary>右栏占主体宽度的比例。</summary>
        private const float RightColumnRatio = 0.25f;

        /// <summary>左栏武装型与芯片功能模板条目的统一紧凑行高。</summary>
        private const float ManufacturingDefRowHeight = 30f;

        /// <summary>左栏始终为芯片功能模板列表保留的最小高度。</summary>
        private const float MinimumFunctionSectionHeight = 120f;

        /// <summary>右栏始终为队列标题与至少一部分内容保留的最小高度。</summary>
        private const float MinimumQueueSectionHeight = 120f;

        /// <summary>打开窗口时绑定的芯片制造台。</summary>
        private readonly Building_ChipFabricator building;

        /// <summary>当前是否停留在制造台首页总览。</summary>
        private bool isOverview = true;

        /// <summary>当前是否在独立触发体制造页，不要求存在芯片草稿。</summary>
        private bool isTriggerBodyPage;

        /// <summary>触发体制造页的选择和数量仅在本次窗口会话中保留。</summary>
        private readonly TriggerBodyManufacturingPanel triggerBodyPanel =
            new TriggerBodyManufacturingPanel();

        /// <summary>一轮打开期间的草稿会话。</summary>
        private readonly ChipManufacturingEditorState editorState =
            new ChipManufacturingEditorState();

        /// <summary>武装型列表滚动位置。</summary>
        private Vector2 armamentFormScroll;

        /// <summary>芯片功能模板列表滚动位置。</summary>
        private Vector2 functionScroll;

        /// <summary>中栏规格与功能属性滚动位置。</summary>
        private Vector2 previewScroll;

        /// <summary>右栏订单提交面板。</summary>
        private readonly ChipManufacturingOrderPanel orderPanel =
            new ChipManufacturingOrderPanel();

        /// <summary>右栏真实账单队列面板。</summary>
        private readonly ChipManufacturingQueuePanel queuePanel =
            new ChipManufacturingQueuePanel();

        /// <summary>首页总览面板。</summary>
        private readonly ChipManufacturingOverviewPanel overviewPanel =
            new ChipManufacturingOverviewPanel();

        /// <summary>建立制造窗口并初始化本轮空草稿。</summary>
        public Window_ChipManufacturing(Building_ChipFabricator building)
        {
            this.building = building;
            forcePause = true;
            draggable = true;
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            editorState.Clear();
            isOverview = true;
        }

        /// <summary>按当前 UI 屏幕限制窗口尺寸，由原版窗口栈负责居中。</summary>
        public override Vector2 InitialSize => new Vector2(
            Mathf.Min(TargetWidth, Mathf.Max(1f, VerseUI.screenWidth - ScreenMargin)),
            Mathf.Min(TargetHeight, Mathf.Max(1f, VerseUI.screenHeight - ScreenMargin)));

        /// <summary>关闭窗口时丢弃全部未入队草稿。</summary>
        public override void PostClose()
        {
            editorState.Clear();
            base.PostClose();
        }

        /// <summary>绘制顶部筛选和稳定三栏主体。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            Rect root = inRect;
            float y = DrawCategoryTabs(new Rect(root.x, root.y, root.width, CategoryTabsHeight));
            if (!isOverview && !isTriggerBodyPage && editorState.CurrentDraft == null)
            {
                OpenOverview();
                return;
            }

            if (!isOverview && !isTriggerBodyPage)
            {
                y += DrawProfessionTabs(new Rect(
                    root.x,
                    y,
                    root.width,
                    ProfessionTabsHeight));
            }

            Rect columnsRect = new Rect(
                root.x,
                y + 6f,
                root.width,
                root.yMax - y - 6f);
            float leftWidth = Mathf.Floor(columnsRect.width * LeftColumnRatio);
            float rightWidth = Mathf.Floor(columnsRect.width * RightColumnRatio);
            float middleWidth = columnsRect.width - leftWidth - rightWidth - ColumnGap * 2f;
            Rect left = new Rect(columnsRect.x, columnsRect.y, leftWidth, columnsRect.height);
            Rect middle = new Rect(left.xMax + ColumnGap, columnsRect.y, middleWidth, columnsRect.height);
            Rect right = new Rect(middle.xMax + ColumnGap, columnsRect.y, rightWidth, columnsRect.height);

            if (isOverview)
            {
                overviewPanel.Draw(
                    columnsRect,
                    building,
                    editorState,
                    ChipManufacturingListModel.GetCategories(),
                    OpenCategory,
                    queuePanel,
                    OpenLoadedConfiguration);
                return;
            }

            if (isTriggerBodyPage)
            {
                triggerBodyPanel.Draw(left, middle, right, building, editorState, queuePanel, OpenLoadedConfiguration);
                return;
            }

            DrawLeftColumn(left);
            DrawMiddleColumn(middle);
            DrawRightColumn(right);
        }

        /// <summary>绘制总览、触发体制造及芯片主分类按钮。</summary>
        private float DrawCategoryTabs(Rect rect)
        {
            List<ChipCategoryDef> categories = ChipManufacturingListModel.GetCategories();
            int tabCount = categories.Count + 2;
            if (tabCount <= 0)
            {
                return CategoryTabsHeight;
            }

            float width = rect.width / tabCount;
            DrawNavigationTab(
                new Rect(rect.x, rect.y, width - 3f, rect.height),
                "BDP_ChipManufacturing_OverviewTab".Translate(),
                isOverview,
                OpenOverview);
            DrawNavigationTab(
                new Rect(rect.x + width, rect.y, width - 3f, rect.height),
                "BDP_TriggerBodyManufacturing_Tab".Translate(),
                isTriggerBodyPage,
                OpenTriggerBodyPage);
            for (int index = 0; index < categories.Count; index++)
            {
                ChipCategoryDef category = categories[index];
                DrawNavigationTab(
                    new Rect(
                        rect.x + width * (index + 2),
                        rect.y,
                        width - 3f,
                        rect.height),
                    category.LabelCap,
                    !isOverview && !isTriggerBodyPage && editorState.CurrentCategory == category,
                    () => OpenCategory(category));
            }

            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
            return CategoryTabsHeight;
        }

        /// <summary>绘制总览或主分类导航中的单个页签。</summary>
        private static void DrawNavigationTab(
            Rect button,
            string label,
            bool selected,
            Action onSelected)
        {
            if (selected)
            {
                Widgets.DrawHighlightSelected(button);
            }
            else if (Mouse.IsOver(button))
            {
                Widgets.DrawHighlight(button);
            }

            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(button.ContractedBy(3f, 1f), label);
            Text.Anchor = oldAnchor;
            if (Widgets.ButtonInvisible(button))
            {
                onSelected?.Invoke();
            }
        }

        /// <summary>绘制所有主分类共用的职业路径，并返回占用高度。</summary>
        private float DrawProfessionTabs(Rect rect)
        {
            List<ChipProfessionDef> professions = ChipManufacturingListModel.GetProfessions();
            int tabCount = professions.Count;
            if (tabCount <= 0)
            {
                return 0f;
            }

            float width = rect.width / tabCount;
            for (int index = 0; index < professions.Count; index++)
            {
                ChipProfessionDef profession = professions[index];
                DrawNavigationTab(
                    new Rect(rect.x + width * index, rect.y, width - 3f, rect.height),
                    profession.defName == "BDP_ChipProfession_Shooter"
                        ? "BDP_ChipManufacturing_RangedProfessionTab".Translate().ToString()
                        : profession.LabelCap.ToString(),
                    editorState.CurrentProfession == profession,
                    () => editorState.Switch(editorState.CurrentCategory, profession));
            }

            return ProfessionTabsHeight;
        }

        /// <summary>从总览或主分类页签进入具体制造配置路径。</summary>
        private void OpenCategory(ChipCategoryDef category)
        {
            if (category == null)
            {
                return;
            }

            List<ChipProfessionDef> professions = ChipManufacturingListModel.GetProfessions();
            ChipProfessionDef profession = professions.Contains(editorState.CurrentProfession)
                ? editorState.CurrentProfession
                : FirstOrNull(professions);
            editorState.Switch(category, profession);
            isOverview = false;
            isTriggerBodyPage = false;
        }

        /// <summary>进入触发体页；不清除已编辑的芯片草稿。</summary>
        private void OpenTriggerBodyPage()
        {
            isOverview = false;
            isTriggerBodyPage = true;
        }

        /// <summary>回到总览页；当前编辑草稿仍由本轮会话保留。</summary>
        private void OpenOverview()
        {
            isOverview = true;
            isTriggerBodyPage = false;
        }

        /// <summary>仅在成功载入并建立当前草稿后离开总览页。</summary>
        private void OpenLoadedConfiguration()
        {
            if (editorState.CurrentDraft != null)
            {
                isOverview = false;
                isTriggerBodyPage = false;
            }
        }

        /// <summary>绘制一组等宽 Def 页签按钮。</summary>
        private static void DrawDefTabs<T>(
            Rect rect,
            IList<T> defs,
            T selected,
            Action<T> onSelected)
            where T : Def
        {
            if (defs == null || defs.Count == 0)
            {
                return;
            }

            float width = rect.width / defs.Count;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            for (int index = 0; index < defs.Count; index++)
            {
                Rect button = new Rect(rect.x + width * index, rect.y, width - 3f, rect.height);
                if (defs[index] == selected)
                {
                    Widgets.DrawHighlightSelected(button);
                }
                else if (Mouse.IsOver(button))
                {
                    Widgets.DrawHighlight(button);
                }

                Widgets.Label(button.ContractedBy(3f, 1f), defs[index].LabelCap);
                if (Widgets.ButtonInvisible(button))
                {
                    onSelected(defs[index]);
                }
            }

            Text.Anchor = oldAnchor;
            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
        }

        /// <summary>左栏先显示功能；选中功能后，在其下方内联展开合法构型列表。</summary>
        private void DrawLeftColumn(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(8f);
            float y = inner.y;
            List<ChipArmamentFormDef> forms =
                ChipManufacturingListModel.GetArmamentForms(
                    editorState.CurrentCategory,
                    editorState.CurrentProfession,
                    ResolveSelectedFunctions());
            float armamentFormHeight = 0f;
            if (forms.Count > 0)
            {
                float desiredArmamentFormHeight = 26f + forms.Count * ManufacturingDefRowHeight;
                float availableArmamentFormHeight = Mathf.Max(
                    0f,
                    inner.height - MinimumFunctionSectionHeight - 8f);
                armamentFormHeight = Mathf.Min(
                    desiredArmamentFormHeight,
                    availableArmamentFormHeight);
                if (armamentFormHeight < 24f)
                {
                    armamentFormHeight = 0f;
                }
            }

            float functionHeight = Mathf.Max(0f, inner.height - armamentFormHeight
                - (armamentFormHeight > 0f ? 8f : 0f));
            if (armamentFormHeight > 0f)
            {
                // 构型紧接功能列表，不让功能列表的空白把第二步推到栏底。
                functionHeight = Mathf.Min(functionHeight,
                    26f + ResolveFunctionDefs().Count * ManufacturingDefRowHeight);
            }
            DrawFunctionSection(new Rect(
                inner.x,
                y,
                inner.width,
                functionHeight));
            if (armamentFormHeight >= 24f)
            {
                DrawArmamentFormSection(
                    new Rect(inner.x, y + functionHeight + 8f, inner.width, armamentFormHeight),
                    forms);
            }
        }

        /// <summary>绘制武装型标题与可滚动条目。</summary>
        private void DrawArmamentFormSection(
            Rect rect,
            List<ChipArmamentFormDef> forms)
        {
            if (rect.width <= 0f || rect.height < 24f)
            {
                return;
            }

            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f),
                "BDP_ChipManufacturing_ArmamentFormSection".Translate());
            Rect outRect = new Rect(rect.x, rect.y + 24f, rect.width, rect.height - 24f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f,
                Mathf.Max(outRect.height, forms.Count * ManufacturingDefRowHeight));
            Widgets.BeginScrollView(outRect, ref armamentFormScroll, viewRect);
            for (int index = 0; index < forms.Count; index++)
            {
                ChipArmamentFormDef form = forms[index];
                bool selected = editorState.CurrentDraft.Record.ArmamentFormDefName == form.defName;
                DrawManufacturingDefRow(
                    new Rect(0f, index * ManufacturingDefRowHeight, viewRect.width, ManufacturingDefRowHeight - 2f),
                    form,
                    selected,
                    true,
                    null,
                    () => editorState.CurrentDraft.SelectArmamentForm(selected ? null : form));
            }
            Widgets.EndScrollView();
        }

        /// <summary>绘制芯片功能模板标题、交换按钮和可滚动功能模板列表。</summary>
        private void DrawFunctionSection(Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Widgets.Label(new Rect(rect.x, rect.y, rect.width - 90f, 24f),
                "BDP_ChipManufacturing_FunctionSection".Translate());
            if (editorState.CurrentDraft?.Record?.OrderedChipFunctionDefNames?.Count == 2
                && Widgets.ButtonText(new Rect(rect.xMax - 86f, rect.y, 86f, 26f),
                    "BDP_ChipManufacturing_SwapFunctions".Translate()))
            {
                editorState.CurrentDraft.SwapFunctions();
            }

            Rect outRect = new Rect(rect.x, rect.y + 26f, rect.width, rect.height - 26f);
            if (outRect.height <= 0f)
            {
                return;
            }

            ChipProfessionDef productProfession = ChipManufacturingDefLookup.FindProfession(
                editorState.CurrentDraft.Record.ProfessionDefName);
            List<ChipManufacturingPresetDef> functionDefs = ResolveFunctionDefs();
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f,
                Mathf.Max(outRect.height, functionDefs.Count * ManufacturingDefRowHeight));
            Widgets.BeginScrollView(outRect, ref functionScroll, viewRect);
            for (int index = 0; index < functionDefs.Count; index++)
            {
                ChipManufacturingPresetDef function = functionDefs[index];
                int selectedIndex = editorState.CurrentDraft.Record.OrderedChipFunctionDefNames.IndexOf(function.defName);
                string failureCode = null;
                bool enabled = selectedIndex >= 0 || ChipManufacturingListModel.CanSelectFunction(
                    editorState.CurrentDraft,
                    productProfession,
                    function,
                    out failureCode);
                DrawManufacturingDefRow(
                    new Rect(0f, index * ManufacturingDefRowHeight, viewRect.width, ManufacturingDefRowHeight - 2f),
                    function,
                    selectedIndex >= 0,
                    enabled,
                    failureCode,
                    () =>
                    {
                        if (selectedIndex >= 0)
                        {
                            editorState.CurrentDraft.RemoveFunctionAt(selectedIndex);
                        }
                        else
                        {
                            editorState.CurrentDraft.TrySelectFunction(
                                productProfession,
                                function,
                                out _);
                        }
                    });
            }
            Widgets.EndScrollView();
        }

        /// <summary>功能列表按草稿真实职业与已选构型过滤，不把合页身份当成品职业。</summary>
        private List<ChipManufacturingPresetDef> ResolveFunctionDefs()
        {
            ChipCombinationRecord record = editorState.CurrentDraft.Record;
            return ChipManufacturingListModel.GetChipFunctionDefs(
                editorState.CurrentCategory,
                ChipManufacturingDefLookup.FindProfession(record.ProfessionDefName),
                ChipManufacturingDefLookup.FindArmamentForm(record.ArmamentFormDefName));
        }

        /// <summary>按草稿顺序解析当前已选芯片功能模板，供构型适用范围筛选复用。</summary>
        private List<ChipManufacturingPresetDef> ResolveSelectedFunctions()
        {
            List<ChipManufacturingPresetDef> result = new List<ChipManufacturingPresetDef>();
            IList<string> names = editorState.CurrentDraft?.Record?.OrderedChipFunctionDefNames;
            if (names == null)
            {
                return result;
            }

            for (int index = 0; index < names.Count; index++)
            {
                ChipManufacturingPresetDef function =
                    ChipManufacturingDefLookup.FindChipFunctionDef(names[index]);
                if (function != null)
                {
                    result.Add(function);
                }
            }

            return result;
        }

        /// <summary>绘制制造定义行；行本体选择，i 按钮只打开信息卡。</summary>
        private static void DrawManufacturingDefRow(
            Rect rect,
            Def definition,
            bool selected,
            bool enabled,
            string failureCode,
            Action onClicked)
        {
            if (selected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Color oldColor = GUI.color;
            if (!enabled)
            {
                GUI.color = Color.gray;
            }

            Rect infoRect = new Rect(rect.xMax - 28f, rect.y + 3f, 26f, 26f);
            Rect bodyRect = new Rect(rect.x, rect.y, rect.width - 32f, rect.height);
            Widgets.Label(
                bodyRect.ContractedBy(6f, 2f),
                ChipManufacturingDefLabelResolver.Resolve(definition));
            if (enabled && Widgets.ButtonInvisible(bodyRect))
            {
                onClicked();
            }

            GUI.color = oldColor;
            if (Widgets.ButtonText(infoRect, "i"))
            {
                Find.WindowStack.Add(new Window_ChipManufacturingDefInfo(definition));
            }

            if (!enabled && !failureCode.NullOrEmpty())
            {
                TooltipHandler.TipRegion(
                    rect,
                    ("BDP_ChipManufacturing_SelectFailure_" + failureCode).Translate());
            }
        }

        /// <summary>从统一解析结果绘制芯片规格、武装型修正和功能属性。</summary>
        private void DrawMiddleColumn(Rect rect)
        {
            ChipCombinationResolution resolution = editorState.CurrentDraft != null
                ? new ChipCombinationResolver().Resolve(editorState.CurrentDraft.Record)
                : null;
            ChipManufacturingPreviewModel model =
                ChipManufacturingPreviewBuilder.Build(resolution);
            ChipManufacturingPreviewPanel.Draw(rect, model, ref previewScroll);
        }

        /// <summary>右栏上半提交订单，下半直接操作制造台真实账单队列。</summary>
        private void DrawRightColumn(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(8f);
            if (inner.width <= 0f || inner.height <= 0f)
            {
                return;
            }

            ChipCombinationResolution resolution = editorState.CurrentDraft != null
                ? new ChipCombinationResolver().Resolve(editorState.CurrentDraft.Record)
                : null;
            float maximumOrderHeight = Mathf.Max(
                0f,
                inner.height - MinimumQueueSectionHeight - 13f);
            float orderHeight = orderPanel.Draw(
                new Rect(inner.x, inner.y, inner.width, maximumOrderHeight),
                building,
                editorState.CurrentDraft,
                resolution);
            float separatorY = inner.y + orderHeight + 6f;
            Widgets.DrawLineHorizontal(inner.x, separatorY, inner.width);
            float queueY = separatorY + 7f;
            Rect queueRect = new Rect(
                inner.x,
                queueY,
                inner.width,
                Mathf.Max(0f, inner.yMax - queueY));
            queuePanel.Draw(queueRect, building, editorState);
        }

        /// <summary>读取列表首项或 null。</summary>
        private static T FirstOrNull<T>(IList<T> source)
            where T : class
        {
            return source != null && source.Count > 0 ? source[0] : null;
        }
    }
}
