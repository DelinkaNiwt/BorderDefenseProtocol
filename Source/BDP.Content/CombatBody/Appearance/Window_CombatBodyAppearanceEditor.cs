using System;
using BDP.Core.CombatBody;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 战斗体编辑台三栏个人服装模板窗口。
    /// </summary>
    public sealed class Window_CombatBodyAppearanceEditor : Window
    {
        /// <summary>窗口目标宽度。</summary>
        private const float TargetWidth = 1120f;

        /// <summary>窗口目标高度。</summary>
        private const float TargetHeight = 720f;

        /// <summary>窗口屏幕边缘安全空隙。</summary>
        private const float ScreenMargin = 80f;

        /// <summary>当前操作的小人。</summary>
        private readonly Pawn pawn;

        /// <summary>当前交互的编辑台。</summary>
        private readonly Building_CombatBodyEditor editor;

        /// <summary>模板管理面板。</summary>
        private readonly Panel_CombatBodyAppearanceTemplates templatePanel =
            new Panel_CombatBodyAppearanceTemplates();

        /// <summary>模板上传服务。</summary>
        private readonly CombatBodyAppearanceTemplateService templateService =
            new CombatBodyAppearanceTemplateService();

        /// <summary>临时服装预览服务。</summary>
        private readonly CombatBodyAppearancePreviewService previewService =
            new CombatBodyAppearancePreviewService();

        /// <summary>当前预览资源。</summary>
        private CombatBodyAppearancePreviewState previewState;

        /// <summary>当前选中的稳定模板编号。</summary>
        private int selectedTemplateId = CombatBodyAppearanceProfile.NoTemplateId;

        /// <summary>预览缓存对应的模式。</summary>
        private CombatBodyFrontMode previewMode = CombatBodyFrontMode.MirrorOriginal;

        /// <summary>预览缓存是否已经建立。</summary>
        private bool previewResolved;

        /// <summary>建立编辑台窗口并读取已有选择，不为没有档案的 Pawn 强行创建档案。</summary>
        public Window_CombatBodyAppearanceEditor(
            Pawn pawn,
            Building_CombatBodyEditor editor)
        {
            this.pawn = pawn;
            this.editor = editor;
            if (CombatBodyAppearanceLedgerService.TryGetProfile(
                pawn,
                out CombatBodyAppearanceProfile profile))
            {
                selectedTemplateId = profile.SelectedTemplateId;
            }

            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            draggable = true;
        }

        /// <summary>按当前屏幕限制窗口尺寸。</summary>
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(
                    Mathf.Min(TargetWidth, Mathf.Max(1f, Verse.UI.screenWidth - ScreenMargin)),
                    Mathf.Min(TargetHeight, Mathf.Max(1f, Verse.UI.screenHeight - ScreenMargin)));
            }
        }

        /// <summary>绘制顶部模式栏、中间预览、左右模板面板和底部操作栏。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            CombatBodyAppearanceProfile profile = ResolveProfile();
            CombatBodyFrontMode mode = profile != null
                ? profile.Mode
                : CombatBodyFrontMode.MirrorOriginal;

            float topHeight = 42f;
            float bottomHeight = 34f;
            DrawTopBar(new Rect(inRect.x, inRect.y, inRect.width, topHeight), profile, mode);
            DrawThreeColumns(
                new Rect(
                    inRect.x,
                    inRect.y + topHeight + 6f,
                    inRect.width,
                    inRect.height - topHeight - bottomHeight - 12f),
                profile,
                mode);
            DrawBottomBar(
                new Rect(
                    inRect.x,
                    inRect.yMax - bottomHeight,
                    inRect.width,
                    bottomHeight),
                profile);
        }

        /// <summary>窗口关闭时释放当前预览纹理。</summary>
        public override void PostClose()
        {
            ReleasePreview();
            base.PostClose();
        }

        /// <summary>绘制顶部标题与镜像/预设切换按钮。</summary>
        private void DrawTopBar(
            Rect rect,
            CombatBodyAppearanceProfile profile,
            CombatBodyFrontMode mode)
        {
            Widgets.DrawMenuSection(rect);
            string pawnLabel = pawn != null ? pawn.LabelShortCap : string.Empty;
            Widgets.Label(
                new Rect(rect.x + 10f, rect.y + 4f, rect.width * 0.40f, 32f),
                "BDP_Window_CombatBodyAppearance_Title".Translate(pawnLabel));

            float buttonWidth = 86f;
            float buttonY = rect.y + 8f;
            float right = rect.xMax - 10f;
            Rect presetRect = new Rect(right - buttonWidth, buttonY, buttonWidth, 26f);
            Rect mirrorRect = new Rect(presetRect.x - buttonWidth - 6f, buttonY, buttonWidth, 26f);
            if (Widgets.ButtonText(mirrorRect, "BDP_Window_CombatBodyAppearance_Mirror".Translate()))
            {
                SetMode(CombatBodyFrontMode.MirrorOriginal);
            }

            if (Widgets.ButtonText(presetRect, "BDP_Window_CombatBodyAppearance_Preset".Translate()))
            {
                SetMode(CombatBodyFrontMode.Preset);
            }

            Widgets.Label(
                new Rect(mirrorRect.x - 220f, buttonY, 210f, 26f),
                "BDP_Window_CombatBodyAppearance_CurrentMode"
                    .Translate(mode == CombatBodyFrontMode.Preset
                        ? "BDP_Window_CombatBodyAppearance_Preset".Translate()
                        : "BDP_Window_CombatBodyAppearance_Mirror".Translate()));
        }

        /// <summary>绘制左侧模板列表、中间 Pawn 预览和右侧模板详情。</summary>
        private void DrawThreeColumns(
            Rect rect,
            CombatBodyAppearanceProfile profile,
            CombatBodyFrontMode mode)
        {
            const float gap = 8f;
            const float leftWidth = 250f;
            const float rightWidth = 300f;
            float middleWidth = rect.width - leftWidth - rightWidth - gap * 2f;
            Rect left = new Rect(rect.x, rect.y, leftWidth, rect.height);
            Rect middle = new Rect(left.xMax + gap, rect.y, middleWidth, rect.height);
            Rect right = new Rect(middle.xMax + gap, rect.y, rightWidth, rect.height);

            templatePanel.DrawTemplateList(
                left,
                profile,
                selectedTemplateId,
                OnTemplateSelected,
                CreateEmptyTemplate,
                RenameSelectedTemplate,
                DeleteSelectedTemplate);
            DrawPreviewPanel(middle, profile, mode);
            templatePanel.DrawTemplateDetail(right, profile, selectedTemplateId);
        }

        /// <summary>绘制中间 Pawn 立绘预览。</summary>
        private void DrawPreviewPanel(
            Rect rect,
            CombatBodyAppearanceProfile profile,
            CombatBodyFrontMode mode)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(8f);
            Widgets.Label(
                new Rect(inner.x, inner.y, inner.width, 28f),
                "BDP_Window_CombatBodyAppearance_Preview".Translate());
            EnsurePreview(profile, mode);
            Rect portraitRect = new Rect(inner.x, inner.y + 34f, inner.width, inner.height - 34f);
            if (previewState?.Texture != null)
            {
                GUI.DrawTexture(portraitRect, previewState.Texture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                Widgets.Label(
                    portraitRect,
                    "BDP_Message_CombatBodyAppearance_PreviewFailed".Translate());
            }
        }

        /// <summary>绘制底部上传和关闭按钮。</summary>
        private void DrawBottomBar(
            Rect rect,
            CombatBodyAppearanceProfile profile)
        {
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);
            float closeWidth = 90f;
            if (Widgets.ButtonText(
                new Rect(rect.xMax - closeWidth, rect.y + 5f, closeWidth, 24f),
                "BDP_Window_CombatBodyAppearance_Close".Translate()))
            {
                Close();
            }

            if (Widgets.ButtonText(
                new Rect(rect.x, rect.y + 5f, 210f, 24f),
                "BDP_Window_CombatBodyAppearance_Upload".Translate()))
            {
                ConfirmUpload();
            }
        }

        /// <summary>获取当前已经持久化的 Pawn 档案。</summary>
        private CombatBodyAppearanceProfile ResolveProfile()
        {
            CombatBodyAppearanceLedgerService.TryGetProfile(
                pawn,
                out CombatBodyAppearanceProfile profile);
            if (profile != null
                && !profile.TryGetTemplate(selectedTemplateId, out CombatBodyAppearanceTemplate ignored)
                && profile.SelectedTemplateId != CombatBodyAppearanceProfile.NoTemplateId)
            {
                selectedTemplateId = profile.SelectedTemplateId;
            }

            return profile;
        }

        /// <summary>按模式和模板选择建立或复用预览缓存。</summary>
        private void EnsurePreview(
            CombatBodyAppearanceProfile profile,
            CombatBodyFrontMode mode)
        {
            if (previewResolved
                && previewState != null
                && previewState.TemplateId == selectedTemplateId
                && previewMode == mode)
            {
                return;
            }

            ReleasePreview();
            previewState = previewService.Capture(pawn, profile);
            previewMode = mode;
            previewResolved = true;
        }

        /// <summary>切换镜像/预设模式并即时保存。</summary>
        private void SetMode(CombatBodyFrontMode mode)
        {
            CombatBodyAppearanceProfile profile =
                CombatBodyAppearanceLedgerService.GetOrCreateProfile(pawn);
            if (profile == null)
            {
                return;
            }

            if (mode == CombatBodyFrontMode.Preset
                && !profile.TryGetTemplate(
                    selectedTemplateId,
                    out CombatBodyAppearanceTemplate selectedTemplate))
            {
                Messages.Message(
                    "BDP_Message_CombatBodyAppearance_PresetUnavailable".Translate(),
                    editor,
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            if (mode == CombatBodyFrontMode.Preset)
            {
                profile.SelectedTemplateId = selectedTemplateId;
            }

            CombatBodyAppearanceLedgerService.SetMode(pawn, mode);
            previewResolved = false;
        }

        /// <summary>选择模板但不强制切换当前模式。</summary>
        private void OnTemplateSelected(int templateId)
        {
            if (CombatBodyAppearanceLedgerService.SelectTemplate(pawn, templateId))
            {
                selectedTemplateId = templateId;
                previewResolved = false;
            }
        }

        /// <summary>创建合法空模板并自动选中。</summary>
        private void CreateEmptyTemplate()
        {
            CombatBodyAppearanceProfile profile =
                CombatBodyAppearanceLedgerService.GetOrCreateProfile(pawn);
            if (profile == null)
            {
                return;
            }

            CombatBodyAppearanceTemplate template =
                CombatBodyAppearanceLedgerService.AddTemplate(
                    pawn,
                    "BDP_CombatBodyEditor_DefaultTemplateName"
                        .Translate(profile.NextTemplateId)
                        .ToString());
            if (template != null && CombatBodyAppearanceLedgerService.SelectTemplate(pawn, template.TemplateId))
            {
                selectedTemplateId = template.TemplateId;
                previewResolved = false;
            }
        }

        /// <summary>打开原版重命名窗口。</summary>
        private void RenameSelectedTemplate()
        {
            CombatBodyAppearanceProfile profile = ResolveProfile();
            if (profile == null
                || !profile.TryGetTemplate(
                    selectedTemplateId,
                    out CombatBodyAppearanceTemplate template))
            {
                return;
            }

            Find.WindowStack.Add(new TemplateRenameDialog(
                new TemplateRenameTarget(pawn, template.TemplateId, template.Name)));
        }

        /// <summary>弹出确认后删除当前模板；删除时由账本服务回退镜像。</summary>
        private void DeleteSelectedTemplate()
        {
            CombatBodyAppearanceProfile profile = ResolveProfile();
            if (profile == null
                || !profile.TryGetTemplate(
                    selectedTemplateId,
                    out CombatBodyAppearanceTemplate template))
            {
                return;
            }

            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "BDP_Message_CombatBodyAppearance_DeleteConfirm".Translate(template.Name),
                delegate
                {
                    if (CombatBodyAppearanceLedgerService.DeleteTemplate(pawn, selectedTemplateId))
                    {
                        selectedTemplateId = CombatBodyAppearanceProfile.NoTemplateId;
                        previewResolved = false;
                    }
                },
                true));
        }

        /// <summary>确认上传当前服装，避免玩家误移除身上实体。</summary>
        private void ConfirmUpload()
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "BDP_Message_CombatBodyAppearance_UploadConfirm".Translate(),
                UploadCurrentApparel,
                true));
        }

        /// <summary>提交当前服装上传并刷新模板选择与预览。</summary>
        private void UploadCurrentApparel()
        {
            CombatBodyAppearanceUploadResult result = templateService.UploadCurrentApparel(pawn);
            if (!result.Succeeded)
            {
                Messages.Message(
                    result.FailureMessageKey.Translate(),
                    editor,
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            selectedTemplateId = result.Template.TemplateId;
            previewResolved = false;
        }

        /// <summary>释放旧预览并标记下一帧重新捕获。</summary>
        private void ReleasePreview()
        {
            previewState?.DestroyResources();
            previewState = null;
            previewResolved = false;
        }

        /// <summary>给原版 Dialog_Rename 提供个人模板名称适配对象。</summary>
        private sealed class TemplateRenameTarget : IRenameable
        {
            /// <summary>所属 Pawn。</summary>
            private readonly Pawn pawn;

            /// <summary>所属模板编号。</summary>
            private readonly int templateId;

            /// <summary>当前显示名称。</summary>
            private string label;

            /// <summary>建立模板重命名适配对象。</summary>
            public TemplateRenameTarget(Pawn pawn, int templateId, string label)
            {
                this.pawn = pawn;
                this.templateId = templateId;
                this.label = label ?? string.Empty;
            }

            /// <summary>原版输入框当前名称。</summary>
            public string RenamableLabel
            {
                get { return label; }
                set
                {
                    if (CombatBodyAppearanceLedgerService.RenameTemplate(pawn, templateId, value))
                    {
                        label = value.Trim();
                    }
                }
            }

            /// <summary>原版输入框基础标签。</summary>
            public string BaseLabel
            {
                get { return "BDP_Window_CombatBodyAppearance_TemplateName".Translate(); }
            }

            /// <summary>原版检视标签。</summary>
            public string InspectLabel
            {
                get { return label; }
            }
        }

        /// <summary>
        /// 原版 Dialog_Rename 的具体实例类型。
        /// </summary>
        private sealed class TemplateRenameDialog : Dialog_Rename<TemplateRenameTarget>
        {
            /// <summary>建立模板名称重命名窗口。</summary>
            public TemplateRenameDialog(TemplateRenameTarget target)
                : base(target)
            {
            }
        }
    }
}
