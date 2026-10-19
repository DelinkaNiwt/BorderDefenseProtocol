using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>展示冻结生命资料，并让玩家只决定侧重、传承人和继承特性。</summary>
    public sealed class Window_BlackTriggerFormationDecision : Window
    {
        private const string NoneSelection = "None";
        private readonly string eventId;
        private Vector2 leftScroll;
        private Vector2 rightScroll;

        /// <summary>构造限时形成决策窗口。</summary>
        public Window_BlackTriggerFormationDecision(string eventId)
        {
            this.eventId = eventId;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            bool forced = BlackTriggerFormationWorldService.Find(eventId)?.ForceDecision == true;
            doCloseX = !forced;
            closeOnCancel = !forced;
        }

        /// <summary>窗口尺寸随实际屏幕收窄，避免固定尺寸在小分辨率下越界。</summary>
        public override Vector2 InitialSize
        {
            get
            {
                float width = Mathf.Min(1080f, Mathf.Max(720f, Verse.UI.screenWidth - 40f));
                float height = Mathf.Min(760f, Mathf.Max(520f, Verse.UI.screenHeight - 40f));
                return new Vector2(width, height);
            }
        }

        /// <summary>到期强制决定时拒绝右上角和 Esc 关闭。</summary>
        public override bool OnCloseRequest()
        {
            return BlackTriggerFormationWorldService.Find(eventId)?.ForceDecision != true;
        }

        /// <summary>绘制顶部摘要、双栏滚动主体和固定底栏。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            BlackTriggerFormationEvent formationEvent = BlackTriggerFormationWorldService.Find(eventId);
            if (formationEvent == null)
            {
                Widgets.Label(inRect, "BDP_BlackTrigger_Formation_MissingEvent".Translate());
                return;
            }
            doCloseX = !formationEvent.ForceDecision;
            closeOnCancel = !formationEvent.ForceDecision;
            ValidateLiveSelections(formationEvent);

            Rect header = new Rect(inRect.x, inRect.y, inRect.width, 64f);
            Rect footer = new Rect(inRect.x, inRect.yMax - 48f, inRect.width, 40f);
            Rect body = new Rect(inRect.x, header.yMax + 8f, inRect.width, footer.y - header.yMax - 16f);
            DrawHeader(header, formationEvent);

            float gap = 12f;
            float leftWidth = body.width * 0.40f;
            Rect left = new Rect(body.x, body.y, leftWidth, body.height);
            Rect right = new Rect(left.xMax + gap, body.y, body.width - leftWidth - gap, body.height);
            DrawLifeData(left, formationEvent);
            DrawChoices(right, formationEvent);
            DrawFooter(footer, formationEvent);
        }

        /// <summary>绘制来源、方式和剩余期限。</summary>
        private static void DrawHeader(Rect rect, BlackTriggerFormationEvent formationEvent)
        {
            Widgets.DrawMenuSection(rect);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 6f, rect.width * 0.65f, 28f),
                "BDP_BlackTrigger_Formation_Title".Translate(formationEvent.Snapshot.MakerLabel));
            Text.Font = GameFont.Small;
            string remaining = formationEvent.ForceDecision
                ? "BDP_BlackTrigger_Formation_DeadlineReached".Translate().ToString()
                : "BDP_BlackTrigger_Formation_Remaining".Translate(formationEvent.RemainingTicks.ToStringTicksToPeriod()).ToString();
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(rect.x + rect.width * 0.65f, rect.y + 9f, rect.width * 0.35f - 12f, 24f), remaining);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 36f, rect.width - 24f, 22f),
                "BDP_BlackTrigger_Formation_SourceSummary".Translate(
                    BlackTriggerImprintFormatter.SourceLabel(formationEvent.Snapshot.Source)));
        }

        /// <summary>左栏统一展示冻结的生命印记资料，不拆分形成匹配过程。</summary>
        private void DrawLifeData(Rect outer, BlackTriggerFormationEvent formationEvent)
        {
            Widgets.DrawMenuSection(outer);
            Rect viewport = outer.ContractedBy(10f);
            List<BlackTriggerFactSnapshot> skills = formationEvent.Snapshot.Facts
                .Where(value => value?.Kind == BlackTriggerFactKind.Skill)
                .OrderByDescending(value => value.Value)
                .ThenBy(value => value.DefName, StringComparer.Ordinal)
                .ToList();
            List<BlackTriggerFactSnapshot> records = formationEvent.Snapshot.Facts
                .Where(value => value?.Kind == BlackTriggerFactKind.Record)
                .OrderBy(value => RecordGroupOrder(value.GroupKey))
                .ThenBy(value => value.Rank)
                .ThenBy(value => value.Label, StringComparer.CurrentCulture)
                .ToList();
            float contentHeight = MeasureLifeDataHeight(viewport.width - 16f, skills, records);
            Rect view = new Rect(0f, 0f, viewport.width - 16f, Math.Max(viewport.height, contentHeight));
            Widgets.BeginScrollView(viewport, ref leftScroll, view);
            float y = 0f;
            DrawSectionTitle(new Rect(0f, y, view.width, 28f), "BDP_BlackTrigger_Formation_LifeData".Translate());
            y += 38f;
            if (skills.Count == 0 && records.Count == 0)
            {
                Widgets.Label(new Rect(10f, y, view.width - 20f, 44f), "BDP_BlackTrigger_Formation_NoLifeData".Translate());
                y += 50f;
            }
            else
            {
                foreach (BlackTriggerFactSnapshot skill in skills)
                {
                    y = DrawFactLine(view, y, skill, 10f);
                }
                foreach (IGrouping<string, BlackTriggerFactSnapshot> group in records.GroupBy(value => value.GroupKey))
                {
                    Widgets.Label(new Rect(8f, y, view.width - 16f, 24f), (group.Key ?? "BDP_BlackTrigger_RecordGroup_Other").Translate());
                    y += 26f;
                    foreach (BlackTriggerFactSnapshot record in group)
                    {
                        y = DrawFactLine(view, y, record, 18f);
                    }
                    y += 6f;
                }
            }
            Widgets.EndScrollView();
        }

        /// <summary>右栏展示候选抽象后的侧重，以及独立的传承人和特性决定。</summary>
        private void DrawChoices(Rect outer, BlackTriggerFormationEvent formationEvent)
        {
            Widgets.DrawMenuSection(outer);
            Rect viewport = outer.ContractedBy(10f);
            List<BlackTriggerEmphasisDef> emphases = AvailableEmphases(formationEvent);
            float viewWidth = viewport.width - 16f;
            float contentHeight = MeasureChoiceHeight(viewWidth, formationEvent, emphases);
            Rect view = new Rect(0f, 0f, viewWidth, Math.Max(viewport.height, contentHeight));
            Widgets.BeginScrollView(viewport, ref rightScroll, view);
            float y = 0f;

            string emphasisTitle = emphases.Count == 1
                ? "BDP_BlackTrigger_Formation_EmphasisSingle".Translate().ToString()
                : "BDP_BlackTrigger_Formation_SelectEmphasisTitle".Translate().ToString();
            DrawSectionTitle(new Rect(0f, y, view.width, 28f), emphasisTitle);
            y += 34f;
            foreach (BlackTriggerEmphasisDef emphasis in emphases)
            {
                string summary = emphasis.summaryKey.NullOrEmpty() ? string.Empty : emphasis.summaryKey.Translate().ToString();
                float cardHeight = EmphasisSelectableHeight(view.width,
                    BlackTriggerImprintFormatter.EmphasisLabel(emphasis), summary);
                Rect card = new Rect(0f, y, view.width, cardHeight);
                DrawEmphasisSelectable(card, formationEvent.SelectedEmphasisDefName == emphasis.defName,
                    BlackTriggerImprintFormatter.EmphasisLabel(emphasis), summary,
                    delegate { formationEvent.SelectedEmphasisDefName = emphasis.defName; });
                y += cardHeight + 8f;
            }

            y += 8f;
            DrawSectionTitle(new Rect(0f, y, view.width, 28f), "BDP_BlackTrigger_Formation_HeirTitle".Translate());
            y += 34f;
            y = DrawSectionHelp(view, y, "BDP_BlackTrigger_Formation_HeirHelp");
            DrawSelectable(new Rect(0f, y, view.width, 46f), formationEvent.HeirLoadId == NoneSelection,
                "BDP_BlackTrigger_Formation_HeirNoneChoice".Translate(), string.Empty,
                delegate { formationEvent.HeirLoadId = NoneSelection; });
            y += 52f;
            foreach (BlackTriggerHeirCandidateSnapshot heir in formationEvent.Snapshot.HeirCandidates ?? new List<BlackTriggerHeirCandidateSnapshot>())
            {
                string detail = "BDP_BlackTrigger_Formation_HeirLine".Translate(
                    heir.RelationLabel, heir.Opinion.ToString("+0;-0;0"), heir.LocationLabel);
                BlackTriggerHeirCandidateSnapshot localHeir = heir;
                float heirHeight = SelectableHeight(view.width, detail, 46f);
                DrawSelectable(new Rect(0f, y, view.width, heirHeight), formationEvent.HeirLoadId == heir.LoadId,
                    heir.Label, detail, delegate { formationEvent.HeirLoadId = localHeir.LoadId; });
                y += heirHeight + 6f;
            }

            y += 8f;
            DrawSectionTitle(new Rect(0f, y, view.width, 28f), "BDP_BlackTrigger_Formation_TraitTitle".Translate());
            y += 34f;
            y = DrawSectionHelp(view, y, "BDP_BlackTrigger_Formation_TraitHelp");
            Pawn sourcePawn = formationEvent.Corpse?.InnerPawn ?? formationEvent.Snapshot.Maker;
            foreach (BlackTriggerTraitOptionSnapshot option in formationEvent.Snapshot.TraitOptions ?? new List<BlackTriggerTraitOptionSnapshot>())
            {
                BlackTriggerTraitOptionSnapshot localOption = option;
                string effects = BlackTriggerImprintFormatter.FormatTraitEffects(option, sourcePawn);
                float traitHeight = TraitSelectableHeight(view.width, option.Label, effects);
                Rect row = new Rect(0f, y, view.width, traitHeight);
                DrawTraitSelectable(row, formationEvent.SelectedTraitOptionKey == option.StableKey,
                    option.Label, effects,
                    delegate { formationEvent.SelectedTraitOptionKey = localOption.StableKey; });
                y += traitHeight + 6f;
            }
            DrawSelectable(new Rect(0f, y, view.width, 46f), formationEvent.SelectedTraitOptionKey == NoneSelection,
                "BDP_BlackTrigger_Formation_TraitNone".Translate(), string.Empty,
                delegate { formationEvent.SelectedTraitOptionKey = NoneSelection; });
            Widgets.EndScrollView();
        }

        /// <summary>绘制固定底栏和形成操作按钮。</summary>
        private void DrawFooter(Rect rect, BlackTriggerFormationEvent formationEvent)
        {
            string abandonLabel = formationEvent.ForceDecision
                ? "BDP_BlackTrigger_Formation_Reject".Translate().ToString()
                : "BDP_BlackTrigger_Formation_Abandon".Translate().ToString();
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 140f, rect.height), abandonLabel))
            {
                string confirmKey = formationEvent.ForceDecision
                    ? "BDP_BlackTrigger_Formation_RejectConfirm"
                    : "BDP_BlackTrigger_Formation_AbandonConfirm";
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmKey.Translate(), delegate
                {
                    BlackTriggerFormationWorldService.Abandon(eventId);
                    Close();
                }, destructive: true));
            }

            Rect confirmRect = new Rect(rect.xMax - 160f, rect.y, 160f, rect.height);
            Rect laterRect = formationEvent.ForceDecision
                ? new Rect(confirmRect.x, rect.y, 0f, rect.height)
                : new Rect(confirmRect.x - 152f, rect.y, 140f, rect.height);
            if (!formationEvent.ForceDecision)
            {
                if (Widgets.ButtonText(laterRect, "BDP_BlackTrigger_Formation_Later".Translate())) Close();
            }
            string missingKey = ResolveMissingDecision(formationEvent);
            bool canConfirm = missingKey.NullOrEmpty();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = canConfirm;
#if BDP_RIMWORLD_1_7
            // 当前 1.7 兼容按钮不读取 GUI.enabled，必须让原版控件显式拒绝未填齐的点击。
            bool clicked = Widgets.ButtonText(confirmRect, "BDP_BlackTrigger_Formation_Confirm".Translate(), active: canConfirm);
#else
            bool clicked = Widgets.ButtonText(confirmRect, "BDP_BlackTrigger_Formation_Confirm".Translate());
#endif
            GUI.enabled = previousEnabled;
            if (clicked)
            {
                if (BlackTriggerFormationFinalizationService.TryFinalize(eventId)) Close();
                else
                {
                    Messages.Message(
                        (formationEvent.LastTechnicalFailureKey ?? "BDP_BlackTrigger_Formation_TechnicalFailure").Translate(),
                        MessageTypeDefOf.RejectInput,
                        false);
                }
            }
        }

        /// <summary>按真实文本高度测量选择项，避免说明覆盖相邻选项。</summary>
        private static float SelectableHeight(float width, string detail, float minimumHeight)
        {
            Text.Font = GameFont.Small;
            float detailHeight = detail.NullOrEmpty()
                ? 0f
                : Text.CalcHeight(detail, Mathf.Max(80f, width - 38f));
            return Mathf.Max(minimumHeight, 36f + detailHeight);
        }

        /// <summary>绘制选项区说明文字并返回下一行起点。</summary>
        private static float DrawSectionHelp(Rect view, float y, string key)
        {
            string text = key.Translate().ToString();
            float height = Text.CalcHeight(text, view.width - 20f);
            GUI.color = new Color(0.78f, 0.78f, 0.78f);
            Widgets.Label(new Rect(10f, y, view.width - 20f, height), text);
            GUI.color = Color.white;
            return y + height + 8f;
        }

        /// <summary>绘制一条形成资料并返回下一行起点。</summary>
        private static float DrawFactLine(Rect view, float y, BlackTriggerFactSnapshot fact, float leftPadding)
        {
            string text = "• " + BlackTriggerImprintFormatter.FormatFact(fact);
            float height = Mathf.Max(28f, Text.CalcHeight(text, view.width - leftPadding - 10f));
            Widgets.Label(new Rect(leftPadding, y, view.width - leftPadding - 10f, height), text);
            return y + height + 4f;
        }

        /// <summary>测量左栏实际内容高度，给滚动视图提供准确范围。</summary>
        private static float MeasureLifeDataHeight(
            float width,
            List<BlackTriggerFactSnapshot> skills,
            List<BlackTriggerFactSnapshot> records)
        {
            float y = 38f + 30f;
            if (skills.Count == 0 && records.Count == 0)
            {
                y += 50f;
            }
            else
            {
                foreach (BlackTriggerFactSnapshot fact in skills)
                {
                    y += Math.Max(32f, Text.CalcHeight("• " + BlackTriggerImprintFormatter.FormatFact(fact), width - 20f) + 4f);
                }
                foreach (IGrouping<string, BlackTriggerFactSnapshot> group in records.GroupBy(value => value.GroupKey))
                {
                    y += 32f;
                    foreach (BlackTriggerFactSnapshot record in group)
                    {
                        y += Math.Max(32f, Text.CalcHeight("• " + BlackTriggerImprintFormatter.FormatFact(record), width - 28f) + 4f);
                    }
                    y += 6f;
                }
            }
            return y + 24f;
        }

        /// <summary>测量右栏三类决定的实际内容高度。</summary>
        private static float MeasureChoiceHeight(float width, BlackTriggerFormationEvent formationEvent, List<BlackTriggerEmphasisDef> emphases)
        {
            float y = 34f;
            foreach (BlackTriggerEmphasisDef emphasis in emphases)
            {
                string summary = emphasis.summaryKey.NullOrEmpty() ? string.Empty : emphasis.summaryKey.Translate().ToString();
                y += EmphasisSelectableHeight(width,
                    BlackTriggerImprintFormatter.EmphasisLabel(emphasis), summary) + 8f;
            }
            y += 8f + 34f;
            y += Text.CalcHeight("BDP_BlackTrigger_Formation_HeirHelp".Translate().ToString(), width - 20f) + 8f;
            y += 52f;
            foreach (BlackTriggerHeirCandidateSnapshot heir in formationEvent.Snapshot.HeirCandidates ?? new List<BlackTriggerHeirCandidateSnapshot>())
            {
                string detail = "BDP_BlackTrigger_Formation_HeirLine".Translate(
                    heir.RelationLabel, heir.Opinion.ToString("+0;-0;0"), heir.LocationLabel);
                y += SelectableHeight(width, detail, 46f) + 6f;
            }
            y += 8f + 34f;
            y += Text.CalcHeight("BDP_BlackTrigger_Formation_TraitHelp".Translate().ToString(), width - 20f) + 8f;
            Pawn sourcePawn = formationEvent.Corpse?.InnerPawn ?? formationEvent.Snapshot.Maker;
            foreach (BlackTriggerTraitOptionSnapshot option in formationEvent.Snapshot.TraitOptions ?? new List<BlackTriggerTraitOptionSnapshot>())
            {
                string effects = BlackTriggerImprintFormatter.FormatTraitEffects(option, sourcePawn);
                y += TraitSelectableHeight(width, option.Label, effects) + 6f;
            }
            return y + 70f;
        }

        /// <summary>验证待决期间可能死亡或失效的实时选择。</summary>
        private static void ValidateLiveSelections(BlackTriggerFormationEvent formationEvent)
        {
            if (formationEvent.HeirLoadId.NullOrEmpty()) formationEvent.HeirLoadId = NoneSelection;
            if (!formationEvent.HeirLoadId.NullOrEmpty() && formationEvent.HeirLoadId != NoneSelection)
            {
                BlackTriggerHeirCandidateSnapshot heir = formationEvent.Snapshot.HeirCandidates?
                    .FirstOrDefault(value => value?.LoadId == formationEvent.HeirLoadId);
                if (heir?.Pawn == null || heir.Pawn.Dead) formationEvent.HeirLoadId = null;
            }
            if (!formationEvent.SelectedEmphasisDefName.NullOrEmpty()
                && !AvailableEmphases(formationEvent).Any(value => value.defName == formationEvent.SelectedEmphasisDefName))
            {
                formationEvent.SelectedEmphasisDefName = null;
            }
        }

        /// <summary>返回确认被禁用的首个直接原因。</summary>
        private static string ResolveMissingDecision(BlackTriggerFormationEvent formationEvent)
        {
            if (AvailableEmphases(formationEvent).Count == 0) return "BDP_BlackTrigger_Formation_CandidateUnavailable";
            if (formationEvent.SelectedEmphasisDefName.NullOrEmpty()) return "BDP_BlackTrigger_Formation_SelectEmphasis";
            if (formationEvent.HeirLoadId.NullOrEmpty()) return "BDP_BlackTrigger_Formation_SelectHeir";
            if (formationEvent.SelectedTraitOptionKey.NullOrEmpty()) return "BDP_BlackTrigger_Formation_SelectTrait";
            return null;
        }

        /// <summary>返回仍具有未定形成品的不同侧重。</summary>
        private static List<BlackTriggerEmphasisDef> AvailableEmphases(BlackTriggerFormationEvent formationEvent)
        {
            GameComponent_BlackTriggerWorld world = BlackTriggerFormationWorldService.Resolve();
            return (formationEvent.CandidatePlans ?? new List<BlackTriggerCandidatePlan>())
                .Where(value => value?.ThingDef != null && value.AssignedEmphasis != null
                    && !BlackTriggerFormationWorldService.IsFinalized(world, value.ThingDef.defName))
                .Select(value => value.AssignedEmphasis)
                .GroupBy(value => value.defName, StringComparer.Ordinal)
                .Select(value => value.First())
                .OrderBy(value => value.label ?? value.defName, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>固定七类经历的玩家阅读顺序。</summary>
        private static int RecordGroupOrder(string key)
        {
            string[] order =
            {
                "BDP_BlackTrigger_RecordGroup_Combat",
                "BDP_BlackTrigger_RecordGroup_Medicine",
                "BDP_BlackTrigger_RecordGroup_Social",
                "BDP_BlackTrigger_RecordGroup_Production",
                "BDP_BlackTrigger_RecordGroup_Research",
                "BDP_BlackTrigger_RecordGroup_Adversity",
                "BDP_BlackTrigger_RecordGroup_Experience"
            };
            int index = Array.IndexOf(order, key);
            return index < 0 ? order.Length : index;
        }

        /// <summary>绘制带分隔线的栏标题。</summary>
        private static void DrawSectionTitle(Rect rect, string label)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, label);
            Text.Font = GameFont.Small;
            Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width);
        }

        /// <summary>测量侧重名称与简介同行显示所需的高度。</summary>
        private static float EmphasisSelectableHeight(float width, string title, string summary)
        {
            return InlineSelectableHeight(width, title, summary);
        }

        /// <summary>测量特性名称与机械效果同行显示所需的高度。</summary>
        private static float TraitSelectableHeight(float width, string title, string effects)
        {
            return InlineSelectableHeight(width, title, effects);
        }

        /// <summary>测量名称与右侧摘要同行显示所需的高度。</summary>
        private static float InlineSelectableHeight(float width, string title, string detail)
        {
            Text.Font = GameFont.Small;
            float titleWidth = Mathf.Min(Mathf.Max(130f, Text.CalcSize("○ " + title).x + 16f), width * 0.46f);
            float summaryWidth = Mathf.Max(80f, width - titleWidth - 12f);
            float summaryHeight = detail.NullOrEmpty() ? 24f : Text.CalcHeight(detail, summaryWidth);
            return Mathf.Max(36f, summaryHeight + 10f);
        }

        /// <summary>绘制侧重名称和简介同行的可选项。</summary>
        private static void DrawEmphasisSelectable(Rect rect, bool selected, string title, string summary, Action action)
        {
            DrawInlineSelectable(rect, selected, title, summary, action);
        }

        /// <summary>绘制特性名称和机械效果同行的可选项。</summary>
        private static void DrawTraitSelectable(Rect rect, bool selected, string title, string effects, Action action)
        {
            DrawInlineSelectable(rect, selected, title, effects, action);
        }

        /// <summary>绘制名称与右侧摘要同行的可选项。</summary>
        private static void DrawInlineSelectable(Rect rect, bool selected, string title, string detail, Action action)
        {
            if (selected)
            {
                Widgets.DrawHighlightSelected(rect);
                Widgets.DrawBox(rect, 2);
            }
            else if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            if (Widgets.ButtonInvisible(rect)) action();
            Text.Font = GameFont.Small;
            float titleWidth = Mathf.Min(Mathf.Max(130f, Text.CalcSize("○ " + title).x + 16f), rect.width * 0.46f);
            string marker = selected ? "✓ " : "○ ";
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 5f, titleWidth, rect.height - 10f), marker + title);
            if (!detail.NullOrEmpty())
            {
                GUI.color = selected ? Color.white : new Color(0.78f, 0.78f, 0.78f);
                Widgets.Label(new Rect(rect.x + titleWidth + 8f, rect.y + 5f,
                    Mathf.Max(80f, rect.width - titleWidth - 16f), rect.height - 10f), detail);
                GUI.color = Color.white;
            }
            if (!detail.NullOrEmpty()) TooltipHandler.TipRegion(rect, detail);
        }

        /// <summary>绘制整行可点击、带底色、边框和勾选标记的选项。</summary>
        private static void DrawSelectable(Rect rect, bool selected, string title, string detail, Action action)
        {
            if (selected)
            {
                Widgets.DrawHighlightSelected(rect);
                Widgets.DrawBox(rect, 2);
            }
            else if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            if (Widgets.ButtonInvisible(rect)) action();
            string marker = selected ? "✓ " : "○ ";
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, 24f), marker + title);
            if (!detail.NullOrEmpty())
            {
                GUI.color = selected ? Color.white : new Color(0.78f, 0.78f, 0.78f);
                Widgets.Label(new Rect(rect.x + 28f, rect.y + 29f, rect.width - 38f, rect.height - 32f), detail);
                GUI.color = Color.white;
            }
            if (!detail.NullOrEmpty()) TooltipHandler.TipRegion(rect, detail);
        }
    }
}
