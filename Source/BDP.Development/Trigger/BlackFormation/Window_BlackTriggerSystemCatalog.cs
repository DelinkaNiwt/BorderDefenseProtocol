using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Trigger.Black.Formation;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Development.Trigger.BlackFormation
{
    /// <summary>以定义、当前事件和世界状态三层展示黑触发器真实运行数据。</summary>
    public sealed class Window_BlackTriggerSystemCatalog : Window
    {
        private enum CatalogTab { Definition, CurrentEvent, WorldState }

        private Vector2 listScroll;
        private Vector2 detailScroll;
        private ThingDef selected;
        private CatalogTab tab;
        private string search = string.Empty;

        /// <summary>构造开发总览。</summary>
        public Window_BlackTriggerSystemCatalog()
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
        }

        /// <summary>窗口尺寸。</summary>
        public override Vector2 InitialSize { get { return new Vector2(1180f, 780f); } }

        /// <summary>绘制搜索清单、三页签和独立详情滚动区。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "BDP_BlackTrigger_Dev_CatalogTitle".Translate());
            Text.Font = GameFont.Small;
            search = Widgets.TextField(new Rect(inRect.x, inRect.y + 36f, 350f, 30f), search);
            if (Widgets.ButtonText(new Rect(inRect.x + 360f, inRect.y + 36f, 100f, 30f), "BDP_BlackTrigger_Dev_Refresh".Translate()))
            {
                BlackTriggerBindingIndex.Rebuild();
            }

            Rect body = new Rect(inRect.x, inRect.y + 74f, inRect.width, inRect.height - 82f);
            Rect left = new Rect(body.x, body.y, 390f, body.height);
            Rect right = new Rect(left.xMax + 12f, body.y, body.width - left.width - 12f, body.height);
            DrawList(left, ResolveDefs());
            DrawRight(right);
        }

        /// <summary>返回所有参与黑触发器系统的物品定义。</summary>
        private List<ThingDef> ResolveDefs()
        {
            string query = search?.Trim() ?? string.Empty;
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(value => value?.GetModExtension<BlackTriggerFormationExtension>() != null)
                .Where(value => query.NullOrEmpty() || value.LabelCap.ToString().IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || value.defName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(value => value.label ?? value.defName, StringComparer.CurrentCulture)
                .ToList();
            if (selected == null && defs.Count > 0) selected = defs[0];
            return defs;
        }

        /// <summary>绘制左侧整行可点、带边框与标记的定义清单。</summary>
        private void DrawList(Rect outer, List<ThingDef> defs)
        {
            Widgets.DrawMenuSection(outer);
            Rect viewport = outer.ContractedBy(8f);
            Rect view = new Rect(0f, 0f, viewport.width - 16f, Math.Max(viewport.height, defs.Count * 52f + 8f));
            Widgets.BeginScrollView(viewport, ref listScroll, view);
            float y = 0f;
            foreach (ThingDef def in defs)
            {
                Rect row = new Rect(0f, y, view.width, 46f);
                bool isSelected = def == selected;
                if (isSelected) { Widgets.DrawHighlightSelected(row); Widgets.DrawBox(row, 2); }
                else if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);
                if (Widgets.ButtonInvisible(row)) { selected = def; detailScroll = Vector2.zero; }
                Widgets.Label(new Rect(row.x + 8f, row.y + 3f, row.width - 16f, 22f), (isSelected ? "✓ " : string.Empty) + def.LabelCap);
                GUI.color = Color.gray;
                Widgets.Label(new Rect(row.x + 24f, row.y + 24f, row.width - 30f, 20f), def.defName);
                GUI.color = Color.white;
                y += 52f;
            }
            Widgets.EndScrollView();
        }

        /// <summary>绘制页签和右侧详情。</summary>
        private void DrawRight(Rect outer)
        {
            Widgets.DrawMenuSection(outer);
            Rect tabs = new Rect(outer.x + 8f, outer.y + 8f, outer.width - 16f, 34f);
            float width = tabs.width / 3f;
            if (Widgets.ButtonText(new Rect(tabs.x, tabs.y, width - 4f, tabs.height), "BDP_BlackTrigger_Dev_TabDefinition".Translate())) tab = CatalogTab.Definition;
            if (Widgets.ButtonText(new Rect(tabs.x + width, tabs.y, width - 4f, tabs.height), "BDP_BlackTrigger_Dev_TabCurrentEvent".Translate())) tab = CatalogTab.CurrentEvent;
            if (Widgets.ButtonText(new Rect(tabs.x + width * 2f, tabs.y, width, tabs.height), "BDP_BlackTrigger_Dev_TabWorldState".Translate())) tab = CatalogTab.WorldState;

            Rect viewport = new Rect(outer.x + 10f, tabs.yMax + 8f, outer.width - 20f, outer.height - 58f);
            Rect view = new Rect(0f, 0f, viewport.width - 16f, 2200f);
            Widgets.BeginScrollView(viewport, ref detailScroll, view);
            float y = 0f;
            if (tab == CatalogTab.Definition) y = DrawDefinition(view, selected, y);
            else if (tab == CatalogTab.CurrentEvent) y = DrawCurrentEvent(view, y);
            else y = DrawWorldState(view, y);
            Widgets.EndScrollView();
        }

        /// <summary>展示所选 ThingDef 的形成、适格和校验声明。</summary>
        private static float DrawDefinition(Rect rect, ThingDef def, float y)
        {
            if (def == null) return Line(rect, y, "请从左侧选择一件黑触发器。");
            BlackTriggerFormationExtension extension = def.GetModExtension<BlackTriggerFormationExtension>();
            y = Heading(rect, y, def.LabelCap + "　[" + def.defName + "]");
            y = Line(rect, y, "来源模组：" + (def.modContentPack?.Name ?? "未知") + "；程序集：BDP.Content.dll");
            y = Line(rect, y, "贸易：不可交易或赠礼；世界唯一：" + (BlackTriggerFormationWorldService.IsFinalized(BlackTriggerFormationWorldService.Resolve(), def.defName) ? "已形成" : "未形成"));
            y = Line(rect, y, "关键组件：" + string.Join("、", (def.comps ?? new List<CompProperties>()).Select(value => value.compClass?.Name ?? "无类型").ToArray()), 48f);
            y = Heading(rect, y + 8f, "形成路线");
            List<BlackTriggerFormationRoute> routes = extension.formationRoutes ?? new List<BlackTriggerFormationRoute>();
            if (routes.Count == 0) y = Line(rect, y, "形成路线待设计，因此不进入形成池。");
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                BlackTriggerFormationRoute route = routes[routeIndex];
                string routeId = route?.routeId.NullOrEmpty() == false
                    ? route.routeId
                    : def.defName + "#" + (routeIndex + 1);
                y = Line(rect, y, "路线：" + routeId, 28f, true);
                y = Line(rect, y, "  侧重：" + (route?.emphasis == null ? "无" : route.emphasis.LabelCap.ToString() + " [" + route.emphasis.defName + "]"));
                y = Line(rect, y, "  必须：" + Join(route?.allOf?.Select(FormatFactReference)));
                y = Line(rect, y, "  至少 " + (route?.anyOfMinimum ?? 0) + " 项：" + Join(route?.anyOf?.Select(FormatFactReference)));
                y = Line(rect, y, "  排斥：" + Join(route?.noneOf?.Select(FormatFactReference)));
            }
            y = Heading(rect, y + 8f, "适格条件");
            List<BlackTriggerEligibilityConditionGroup> eligibility = extension.eligibilityGroups ?? new List<BlackTriggerEligibilityConditionGroup>();
            if (eligibility.Count == 0) y = Line(rect, y, "未声明额外内容条件；仍受绑定约束。");
            for (int index = 0; index < eligibility.Count; index++)
            {
                BlackTriggerEligibilityConditionGroup group = eligibility[index];
                y = Line(rect, y, "条件组 " + (index + 1) + "（组间为或）：", 28f, true);
                y = Line(rect, y, "  必须：" + Join(group?.allOf?.Select(FormatEligibility)));
                y = Line(rect, y, "  至少 " + (group?.anyOfMinimum ?? 0) + " 项：" + Join(group?.anyOf?.Select(FormatEligibility)));
                y = Line(rect, y, "  排斥：" + Join(group?.noneOf?.Select(FormatEligibility)));
            }
            y = Heading(rect, y + 8f, "定义校验");
            return Line(rect, y, BlackTriggerFormationDefValidator.IsValid(def) ? "通过" : "失败；该定义不会进入形成池。");
        }

        /// <summary>展示唯一待决事件的冻结资料、路线真值和当前对照。</summary>
        private static float DrawCurrentEvent(Rect rect, float y)
        {
            GameComponent_BlackTriggerWorld world = BlackTriggerFormationWorldService.Resolve();
            BlackTriggerFormationEvent formationEvent = world?.PendingEvent;
            if (formationEvent == null) return Line(rect, y, "当前没有待决的黑触发器形成事件。");
            y = Heading(rect, y, "事件 " + formationEvent.EventId);
            y = Line(rect, y, "来源人物：" + formationEvent.Snapshot.MakerLabel + " [" + formationEvent.Snapshot.MakerLoadId + "]");
            y = Line(rect, y, "尸体：" + DescribeThing(formationEvent.Corpse) + "；剩余：" + formationEvent.RemainingTicks.ToStringTicksToPeriod());
            y = Line(rect, y, "当前决定：侧重=" + (formationEvent.SelectedEmphasisDefName ?? "未选") + "；传承人=" + (formationEvent.HeirLoadId ?? "未选") + "；特性=" + (formationEvent.SelectedTraitOptionKey ?? "未选"));
            y = Line(rect, y, "最近技术失败：" + (formationEvent.LastTechnicalFailureKey ?? "无"));
            y = Heading(rect, y + 8f, "完整冻结快照");
            foreach (BlackTriggerFactSnapshot fact in formationEvent.Snapshot.Facts) y = Line(rect, y, fact.StableKey + "　" + BlackTriggerImprintFormatter.FormatFact(fact));
            y = Line(rect, y, "传承人选项：" + Join(formationEvent.Snapshot.HeirCandidates.Select(value => value.Label + " [" + value.LoadId + "]")), 48f);
            y = Line(rect, y, "特性选项：" + Join(formationEvent.Snapshot.TraitOptions.Select(value => value.Label + " [" + value.StableKey + "]")), 48f);
            y = Heading(rect, y + 8f, "冻结候选与侧重分配");
            foreach (BlackTriggerCandidatePlan plan in formationEvent.CandidatePlans)
            {
                y = Line(rect, y, plan.ThingDef.LabelCap.ToString() + " [" + plan.ThingDef.defName + "] → " + (plan.AssignedEmphasis?.LabelCap.ToString() ?? "无"), 28f, true);
                foreach (BlackTriggerMatchedRouteSnapshot route in plan.MatchedRoutes)
                {
                    y = Line(rect, y, "  路线 " + route.RouteId + "；命中=" + Join(route.MatchedFactKeys) + "；侧重=" + (route.Emphasis?.LabelCap.ToString() ?? "无"), 44f);
                }
            }
            if (!formationEvent.SelectedEmphasisDefName.NullOrEmpty())
            {
                List<BlackTriggerCandidatePlan> previewPool = formationEvent.CandidatePlans
                    .Where(value => value?.AssignedEmphasis?.defName == formationEvent.SelectedEmphasisDefName
                        && !BlackTriggerFormationWorldService.IsFinalized(world, value.ThingDef.defName))
                    .ToList();
                BlackTriggerCandidatePlan preview = BlackTriggerStableRandom.Pick(
                    previewPool,
                    formationEvent.Snapshot.StableSeed,
                    formationEvent.EventId + ":" + formationEvent.SelectedEmphasisDefName);
                y = Line(rect, y, "稳定抽取预览：" + (preview?.ThingDef?.LabelCap.ToString() ?? "当前无可用结果")
                    + (preview?.ThingDef == null ? string.Empty : " [" + preview.ThingDef.defName + "]"));
            }
            y = Heading(rect, y + 8f, "当前重新计算对照");
            BlackTriggerFormationEvaluation current = BlackTriggerFormationMatcher.Evaluate(formationEvent.Snapshot);
            y = Line(rect, y, "冻结候选：" + formationEvent.CandidatePlans.Count + "；当前重算候选：" + current.CandidatePlans.Count);
            foreach (BlackTriggerRouteEvaluation route in current.RouteEvaluations)
            {
                y = Line(rect, y, (route.ThingDef?.LabelCap.ToString() ?? "无") + " / " + route.RouteId + "：" + (route.Passed ? "通过" : "失败")
                    + "；命中=" + Join(route.MatchedFactKeys) + "；失败=" + Join(route.FailureDetails), 48f);
            }
            return y;
        }

        /// <summary>展示冷却、永久账本、现存实例、绑定和印记。</summary>
        private static float DrawWorldState(Rect rect, float y)
        {
            GameComponent_BlackTriggerWorld world = BlackTriggerFormationWorldService.Resolve();
            y = Heading(rect, y, "全局状态");
            int remainingCooldown = Math.Max(0, (world?.NextFormationAllowedTick ?? 0) - Find.TickManager.TicksGame);
            y = Line(rect, y, "普通形成冷却：" + remainingCooldown.ToStringTicksToPeriod());
            y = Line(rect, y, "唯一待决事件：" + (world?.PendingEvent?.EventId ?? "无"));
            y = Line(rect, y, "永久世界唯一账本：" + Join(world?.FinalizedThingDefNames));
            y = Heading(rect, y + 8f, "现存实例");
            List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead
                .Concat(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead)
                .Where(value => value != null).Distinct().ToList();
            HashSet<Thing> allThings = new HashSet<Thing>(Find.Maps.SelectMany(map => map.listerThings.AllThings));
            foreach (Pawn pawn in pawns) allThings.UnionWith(pawn.EquippedWornOrInventoryThings);
            List<Thing> instances = allThings
                .Where(value => value?.TryGetComp<CompBlackTriggerBiocodable>() != null)
                .OrderBy(value => value.def.defName).ThenBy(value => value.ThingID).ToList();
            if (instances.Count == 0) return Line(rect, y, "当前地图中没有现存黑触发器实例。");
            foreach (Thing thing in instances)
            {
                CompBlackTriggerBiocodable code = thing.TryGetComp<CompBlackTriggerBiocodable>();
                CompBlackTriggerImprint imprint = thing.TryGetComp<CompBlackTriggerImprint>();
                y = Line(rect, y, thing.LabelCap + " [" + thing.def.defName + "]　ThingID=" + thing.ThingID, 28f, true);
                y = Line(rect, y, "  位置=" + DescribeThing(thing) + "；绑定者=" + (code?.CodedPawn?.LabelShort ?? "无"));
                y = Line(rect, y, "  印记来源=" + (imprint?.MakerLabel ?? "无") + "；侧重=" + (imprint?.Emphasis?.LabelCap ?? "无")
                    + "；传承人=" + (imprint?.HeirLabel ?? "无") + "；继承特性=" + (imprint?.InheritedTrait?.Label ?? "无"), 44f);
            }
            y = Heading(rect, y + 8f, "异常检查");
            List<string> anomalies = new List<string>();
            foreach (IGrouping<string, Thing> duplicate in instances.GroupBy(value => value.def.defName).Where(value => value.Count() > 1))
            {
                anomalies.Add("重复世界唯一：" + duplicate.Key + "，实例数=" + duplicate.Count());
            }
            foreach (IGrouping<Pawn, Thing> duplicate in instances
                .Where(value => value.TryGetComp<CompBlackTriggerBiocodable>()?.CodedPawn != null)
                .GroupBy(value => value.TryGetComp<CompBlackTriggerBiocodable>().CodedPawn)
                .Where(value => value.Count() > 1))
            {
                anomalies.Add("重复绑定：" + duplicate.Key.LabelShort + "，绑定数=" + duplicate.Count());
            }
            foreach (Pawn pawn in pawns)
            {
                foreach (Trait_BlackTriggerInherited trait in pawn.story?.traits?.allTraits?.OfType<Trait_BlackTriggerInherited>() ?? Enumerable.Empty<Trait_BlackTriggerInherited>())
                {
                    bool sourceEquipped = pawn.equipment?.AllEquipmentListForReading?
                        .Any(value => value?.ThingID == trait.SourceThingId) == true;
                    if (!sourceEquipped) anomalies.Add("孤立临时特性：" + pawn.LabelShort + " / " + trait.Label + " / " + trait.SourceThingId);
                }
            }
            if (anomalies.Count == 0) return Line(rect, y, "未发现重复唯一、重复绑定或孤立临时特性。");
            foreach (string anomaly in anomalies) y = Line(rect, y, anomaly, 30f, true);
            return y;
        }

        /// <summary>格式化形成事实引用，中文名与技术名并列。</summary>
        private static string FormatFactReference(BlackTriggerFactReference reference)
        {
            if (reference?.skill != null) return "技能：" + reference.skill.LabelCap + " [" + reference.skill.defName + "]";
            if (reference?.record != null) return "记录：" + reference.record.LabelCap + " [" + reference.record.defName + "]";
            return "无效引用";
        }

        /// <summary>格式化适格条件及其参数。</summary>
        private static string FormatEligibility(BlackTriggerEligibilityCondition condition)
        {
            if (condition == null) return "空条件";
            string target = condition.skill?.LabelCap ?? condition.trait?.LabelCap ?? condition.gene?.LabelCap ?? condition.record?.LabelCap ?? condition.customWorkerType;
            return condition.kind + (target.NullOrEmpty() ? string.Empty : "：" + target) + (condition.minimumValue == 0f ? string.Empty : " ≥ " + condition.minimumValue);
        }

        /// <summary>描述物品的地图、容器或世界持有位置。</summary>
        private static string DescribeThing(Thing thing)
        {
            if (thing == null) return "无";
            if (thing.MapHeld != null) return thing.MapHeld.Parent.LabelCap + " / " + thing.PositionHeld;
            Thing parent = ThingOwnerUtility.GetFirstParentThing(thing);
            return parent == null ? "未解析的世界容器" : parent.LabelCap + " 的物品栏";
        }

        /// <summary>绘制醒目的分节标题。</summary>
        private static float Heading(Rect rect, float y, string text)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, y, rect.width, 30f), text);
            Text.Font = GameFont.Small;
            Widgets.DrawLineHorizontal(rect.x, y + 30f, rect.width);
            return y + 38f;
        }

        /// <summary>绘制一行可换行诊断文本。</summary>
        private static float Line(Rect rect, float y, string text, float height = 26f, bool highlight = false)
        {
            Rect row = new Rect(rect.x, y, rect.width, height);
            if (highlight) Widgets.DrawHighlight(row);
            Widgets.Label(row, text ?? string.Empty);
            return y + height;
        }

        /// <summary>格式化可空集合。</summary>
        private static string Join(IEnumerable<string> values)
        {
            string[] items = values?.Where(value => !value.NullOrEmpty()).ToArray() ?? new string[0];
            return items.Length == 0 ? "无" : string.Join("、", items);
        }
    }
}
