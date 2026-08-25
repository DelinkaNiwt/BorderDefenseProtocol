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
    /// <summary>由芯片制造台命令打开的三栏制造窗口。</summary>
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

        /// <summary>左栏武装型与动作条目的统一紧凑行高。</summary>
        private const float PresetRowHeight = 30f;

        /// <summary>左栏始终为动作列表保留的最小高度。</summary>
        private const float MinimumActionSectionHeight = 120f;

        /// <summary>右栏始终为队列标题与至少一部分内容保留的最小高度。</summary>
        private const float MinimumQueueSectionHeight = 120f;

        /// <summary>打开窗口时绑定的芯片制造台。</summary>
        private readonly Building_ChipFabricator building;

        /// <summary>当前是否停留在制造台首页总览。</summary>
        private bool isOverview = true;

        /// <summary>一轮打开期间的草稿会话。</summary>
        private readonly ChipManufacturingEditorState editorState =
            new ChipManufacturingEditorState();

        /// <summary>武装型列表滚动位置。</summary>
        private Vector2 armamentFormScroll;

        /// <summary>动作列表滚动位置。</summary>
        private Vector2 actionScroll;

        /// <summary>中栏规格与动作属性滚动位置。</summary>
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
            if (!isOverview && editorState.CurrentDraft == null)
            {
                OpenOverview();
                return;
            }

            if (!isOverview && IsWeapon(editorState.CurrentCategory))
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

            DrawLeftColumn(left);
            DrawMiddleColumn(middle);
            DrawRightColumn(right);
        }

        /// <summary>绘制制造总览与五个固定顺序主分类按钮。</summary>
        private float DrawCategoryTabs(Rect rect)
        {
            List<ChipCategoryDef> categories = ChipManufacturingListModel.GetCategories();
            int tabCount = categories.Count + 1;
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
            for (int index = 0; index < categories.Count; index++)
            {
                ChipCategoryDef category = categories[index];
                DrawNavigationTab(
                    new Rect(
                        rect.x + width * (index + 1),
                        rect.y,
                        width - 3f,
                        rect.height),
                    category.LabelCap,
                    !isOverview && editorState.CurrentCategory == category,
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

        /// <summary>绘制武装分类的职业路径，并返回占用高度。</summary>
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
                    profession.LabelCap,
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
            ChipProfessionDef profession = IsWeapon(category)
                ? professions.Contains(editorState.CurrentProfession)
                    ? editorState.CurrentProfession
                    : FirstOrNull(professions)
                : null;
            editorState.Switch(category, profession);
            isOverview = false;
        }

        /// <summary>回到总览页；当前编辑草稿仍由本轮会话保留。</summary>
        private void OpenOverview()
        {
            isOverview = true;
        }

        /// <summary>仅在成功载入并建立当前草稿后离开总览页。</summary>
        private void OpenLoadedConfiguration()
        {
            if (editorState.CurrentDraft != null)
            {
                isOverview = false;
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

        /// <summary>绘制左栏；存在可见武装型时将其放在动作列表上方。</summary>
        private void DrawLeftColumn(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(8f);
            float y = inner.y;
            List<ChipArmamentFormDef> forms =
                ChipManufacturingListModel.GetArmamentForms(
                    editorState.CurrentProfession,
                    ResolveSelectedActions());
            if (forms.Count > 0)
            {
                float desiredArmamentFormHeight = 26f + forms.Count * PresetRowHeight;
                float availableArmamentFormHeight = Mathf.Max(
                    0f,
                    inner.height - MinimumActionSectionHeight - 8f);
                float armamentFormHeight = Mathf.Min(
                    desiredArmamentFormHeight,
                    availableArmamentFormHeight);
                if (armamentFormHeight >= 24f)
                {
                    DrawArmamentFormSection(
                        new Rect(inner.x, y, inner.width, armamentFormHeight),
                        forms);
                    y += armamentFormHeight + 8f;
                }
            }

            DrawActionSection(new Rect(
                inner.x,
                y,
                inner.width,
                Mathf.Max(0f, inner.yMax - y)));
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
                Mathf.Max(outRect.height, forms.Count * PresetRowHeight));
            Widgets.BeginScrollView(outRect, ref armamentFormScroll, viewRect);
            for (int index = 0; index < forms.Count; index++)
            {
                ChipArmamentFormDef form = forms[index];
                bool selected = editorState.CurrentDraft.Record.ArmamentFormDefName == form.defName;
                DrawPresetRow(
                    new Rect(0f, index * PresetRowHeight, viewRect.width, PresetRowHeight - 2f),
                    form,
                    selected,
                    true,
                    null,
                    () => editorState.CurrentDraft.SelectArmamentForm(selected ? null : form));
            }
            Widgets.EndScrollView();
        }

        /// <summary>绘制动作标题、交换按钮和可滚动动作列表。</summary>
        private void DrawActionSection(Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Widgets.Label(new Rect(rect.x, rect.y, rect.width - 90f, 24f),
                "BDP_ChipManufacturing_ActionSection".Translate());
            if (editorState.CurrentDraft?.Record?.OrderedActionPresetDefNames?.Count == 2
                && Widgets.ButtonText(new Rect(rect.xMax - 86f, rect.y, 86f, 26f),
                    "BDP_ChipManufacturing_SwapActions".Translate()))
            {
                editorState.CurrentDraft.SwapActions();
            }

            Rect outRect = new Rect(rect.x, rect.y + 26f, rect.width, rect.height - 26f);
            if (outRect.height <= 0f)
            {
                return;
            }

            List<ChipActionPresetDef> actions = ChipManufacturingListModel.GetActions(
                editorState.CurrentCategory,
                editorState.CurrentProfession,
                ChipManufacturingDefLookup.FindArmamentForm(
                    editorState.CurrentDraft.Record.ArmamentFormDefName));
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f,
                Mathf.Max(outRect.height, actions.Count * PresetRowHeight));
            Widgets.BeginScrollView(outRect, ref actionScroll, viewRect);
            for (int index = 0; index < actions.Count; index++)
            {
                ChipActionPresetDef action = actions[index];
                int selectedIndex = editorState.CurrentDraft.Record.OrderedActionPresetDefNames.IndexOf(action.defName);
                string failureCode = null;
                bool enabled = selectedIndex >= 0 || ChipManufacturingListModel.CanSelectAction(
                    editorState.CurrentDraft,
                    editorState.CurrentProfession,
                    action,
                    out failureCode);
                DrawPresetRow(
                    new Rect(0f, index * PresetRowHeight, viewRect.width, PresetRowHeight - 2f),
                    action,
                    selectedIndex >= 0,
                    enabled,
                    failureCode,
                    () =>
                    {
                        if (selectedIndex >= 0)
                        {
                            editorState.CurrentDraft.RemoveActionAt(selectedIndex);
                        }
                        else
                        {
                            editorState.CurrentDraft.TrySelectAction(
                                editorState.CurrentProfession,
                                action,
                                out _);
                        }
                    });
            }
            Widgets.EndScrollView();
        }

        /// <summary>按草稿顺序解析当前已选动作，供构型适用范围筛选复用。</summary>
        private List<ChipActionPresetDef> ResolveSelectedActions()
        {
            List<ChipActionPresetDef> result = new List<ChipActionPresetDef>();
            IList<string> names = editorState.CurrentDraft?.Record?.OrderedActionPresetDefNames;
            if (names == null)
            {
                return result;
            }

            for (int index = 0; index < names.Count; index++)
            {
                ChipActionPresetDef action =
                    ChipManufacturingDefLookup.FindAction(names[index]);
                if (action != null)
                {
                    result.Add(action);
                }
            }

            return result;
        }

        /// <summary>绘制预设行；行本体选择，i 按钮只打开信息卡。</summary>
        private static void DrawPresetRow(
            Rect rect,
            Def preset,
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
                ChipPresetLabelResolver.Resolve(preset));
            if (enabled && Widgets.ButtonInvisible(bodyRect))
            {
                onClicked();
            }

            GUI.color = oldColor;
            if (Widgets.ButtonText(infoRect, "i"))
            {
                Find.WindowStack.Add(new Window_ChipPresetInfo(preset));
            }

            if (!enabled && !failureCode.NullOrEmpty())
            {
                TooltipHandler.TipRegion(
                    rect,
                    ("BDP_ChipManufacturing_SelectFailure_" + failureCode).Translate());
            }
        }

        /// <summary>从统一解析结果绘制芯片规格、武装型修正和动作属性。</summary>
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

        /// <summary>判断当前分类是否为唯一使用职业筛选的武装分类。</summary>
        private static bool IsWeapon(ChipCategoryDef category)
        {
            return category?.defName == "BDP_ChipCategory_Weapon";
        }

        /// <summary>读取列表首项或 null。</summary>
        private static T FirstOrNull<T>(IList<T> source)
            where T : class
        {
            return source != null && source.Count > 0 ? source[0] : null;
        }
    }
}
