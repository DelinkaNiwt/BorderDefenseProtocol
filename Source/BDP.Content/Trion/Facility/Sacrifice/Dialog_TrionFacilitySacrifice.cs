using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Facility.Sacrifice
{
    /// <summary>复用原版活动准备窗口；选择一名操作者和一批供物，不创建群体仪式。</summary>
    public sealed class Dialog_TrionFacilitySacrifice : Dialog_BeginLordJob
    {
        /// <summary>窗口针对的核心。</summary>
        private readonly Building_TrionFacility facility;
        /// <summary>满足原版必需接口的单人选择部件。</summary>
        private readonly SingleOperatorWidget operatorWidget;
        /// <summary>打开时一次扫描并聚合同种无差异供物；确认时分配到实际堆栈。</summary>
        private readonly List<TrionFacilitySacrificeSupplyGroup> supplies;
        /// <summary>只保存仍保留的增加段；减量从该类最近段撤销，最后一件不受界面排序影响。</summary>
        private readonly List<KeyValuePair<TrionFacilitySacrificeSupplyGroup, int>> selectionOrder
            = new List<KeyValuePair<TrionFacilitySacrificeSupplyGroup, int>>();
        /// <summary>供物列表滚动位置。</summary>
        private Vector2 suppliesScroll;
        /// <summary>使用共同规则计算的实际选择收益。</summary>
        private double previewYield;
        /// <summary>最近一次选择变化或低频检查的拒绝原因。</summary>
        private string problemKey;

        /// <summary>创建非空原版人物选择部件。</summary>
        public Dialog_TrionFacilitySacrifice(Building_TrionFacility facility)
            : this(facility, new SingleOperatorWidget(facility)) { }

        /// <summary>初始化一次物品扫描；每帧绘制不重新扫描地图。</summary>
        private Dialog_TrionFacilitySacrifice(Building_TrionFacility facility, SingleOperatorWidget widget) : base(widget)
        {
            this.facility = facility;
            operatorWidget = widget;
            widget.Refresh = RefreshPreview;
            facility.ResourceComp.RefreshOperation();
            supplies = TrionFacilitySacrificeSupplyGroup.Create(facility.Map.listerThings.AllThings.Where(thing =>
                thing.def.category == ThingCategory.Item && thing.def.EverHaulable && !thing.IsForbidden(Faction.OfPlayer)));
            supplies.Sort((a, b) => string.Compare(a.Representative.LabelNoCount, b.Representative.LabelNoCount, StringComparison.CurrentCulture));
            RefreshPreview();
        }

        /// <summary>中文标题。</summary>
        public override TaggedString HeaderLabel => "BDP_TrionFacility_Sacrifice_Title".Translate();
        /// <summary>说明正式搬运及整批消费。</summary>
        public override TaggedString DescriptionLabel => "BDP_TrionFacility_Sacrifice_Description".Translate();
        /// <summary>不展示原版仪式的质量说明。</summary>
        public override TaggedString ExtraExplanationLabel => string.Empty;
        /// <summary>明确下达任务。</summary>
        public override TaggedString OkButtonLabel => "BDP_TrionFacility_Sacrifice_Start".Translate();
        /// <summary>复用核心图标。</summary>
        public override Texture2D Icon => facility.def.uiIcon;
        /// <summary>按钮只读缓存；回车与按钮最终都由Start再次核验。</summary>
        public override bool CanBegin => problemKey == null;
        /// <summary>原版按钮区域的简短拒绝说明。</summary>
        protected override IEnumerable<string> BlockingIssues()
        {
            if (problemKey != null) yield return problemKey.Translate().ToString();
        }

        /// <summary>保留原版标题、说明和确认流程，只将固定左右列改为紧凑操作者与全宽供物。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            using (TextBlock.Default())
            {
                var layout = new RectDivider(inRect, GetHashCode());
                layout.NewRow(0f, VerticalJustification.Bottom, 1f);
                DoHeader(ref layout);
                layout.NewRow(0f);
                DoDescription(ref layout);
                DoButtonRow(ref layout, CanBegin);
                layout.NewRow(0f, VerticalJustification.Top, 12f);
                operatorWidget.DrawPawnList(layout.NewRow(40f, VerticalJustification.Top, 12f));
                DoRightColumn(ref layout);
            }
        }

        /// <summary>供物以单行显示名称、收益与可用数，数量输入及四个增减按钮复用原版贸易控件。</summary>
        public override void DoRightColumn(ref RectDivider layout)
        {
            Rect column = layout.Rect;
            Widgets.Label(new Rect(column.x, column.y, column.width, 78f),
                "BDP_TrionFacility_Sacrifice_Preview".Translate(facility.ResourceComp.CurrentResource.ToString("0.##"),
                    facility.ResourceComp.MaxResource.ToString("0.##"), previewYield.ToString("0.##"),
                    (facility.ResourceComp.CurrentResource + previewYield).ToString("0.##")));
            Rect listRect = new Rect(column.x, column.y + 84f, column.width, column.height - 84f);
            if (supplies.Count == 0)
            {
                Widgets.Label(listRect, "BDP_TrionFacility_Sacrifice_NoSupplies".Translate());
                return;
            }
            Rect viewRect = new Rect(0, 0, listRect.width - 18f, supplies.Count * 32f);
            Widgets.BeginScrollView(listRect, ref suppliesScroll, viewRect);
            using (TextBlock.Default())
            {
                Text.WordWrap = false;
                for (int i = 0; i < supplies.Count; i++)
                {
                    TrionFacilitySacrificeSupplyGroup entry = supplies[i];
                    int availableCount = entry.AvailableCount;
                    Rect row = new Rect(0, i * 32f, viewRect.width, 32f);
                    Rect countRect = new Rect(row.xMax - 210f, row.y, 210f, row.height);
                    Rect infoRect = new Rect(countRect.x - 258f, row.y, 250f, row.height);
                    Rect nameRect = new Rect(32f, row.y, infoRect.x - 40f, row.height);
                    Widgets.DrawHighlightIfMouseover(row);
                    Widgets.ThingIcon(new Rect(0, row.y + 3f, 26f, 26f), entry.Representative);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    string label = entry.Representative.LabelNoCount.CapitalizeFirst();
                    Widgets.Label(nameRect, label);
                    TooltipHandler.TipRegion(nameRect, label);
                    Widgets.Label(infoRect,
                        "BDP_TrionFacility_Sacrifice_UnitYield".Translate(entry.UnitYield.ToString("0.##"), availableCount));
                    int oldCount = entry.CountToTransfer;
                    TransferableUIUtility.DoCountAdjustInterface(countRect, entry, i, 0, entry.GetMaximumToTransfer());
                    if (oldCount != entry.CountToTransfer)
                    {
                        RecordSelectionChange(entry, entry.CountToTransfer - oldCount);
                        RefreshPreview();
                    }
                }
            }
            Widgets.EndScrollView();
        }

        /// <summary>增加记录追加到末尾；减量撤回该类最近加入的数量，清空后可回到此前最后一项。</summary>
        private void RecordSelectionChange(TrionFacilitySacrificeSupplyGroup group, int change)
        {
            if (change > 0)
            {
                int last = selectionOrder.Count - 1;
                if (last >= 0 && selectionOrder[last].Key == group)
                    selectionOrder[last] = new KeyValuePair<TrionFacilitySacrificeSupplyGroup, int>(group,
                        selectionOrder[last].Value + change);
                else selectionOrder.Add(new KeyValuePair<TrionFacilitySacrificeSupplyGroup, int>(group, change));
                return;
            }
            for (int i = selectionOrder.Count - 1; i >= 0 && change < 0; i--)
            {
                if (selectionOrder[i].Key != group) continue;
                int remaining = selectionOrder[i].Value + change;
                if (remaining <= 0)
                {
                    change += selectionOrder[i].Value;
                    selectionOrder.RemoveAt(i);
                }
                else
                {
                    selectionOrder[i] = new KeyValuePair<TrionFacilitySacrificeSupplyGroup, int>(group, remaining);
                    change = 0;
                }
            }
        }

        /// <summary>分配真实实物，将玩家最后一项放末尾供共同规则核验；不足时拒绝，不按收益重排。</summary>
        private List<ThingCountClass> SelectedBatch()
        {
            var batch = new List<ThingCountClass>();
            var lastGroup = selectionOrder.Count > 0 ? selectionOrder[selectionOrder.Count - 1].Key : null;
            foreach (TrionFacilitySacrificeSupplyGroup entry in supplies)
                if (entry != lastGroup && !entry.TryAppendSelected(batch)) return null;
            if (lastGroup != null && !lastGroup.TryAppendSelected(batch)) return null;
            return batch;
        }

        /// <summary>选择变化或低频刷新时两遍更新上限；只读取已有供物组，不逐帧扫描地图。</summary>
        private void RefreshSelectionLimits()
        {
            double selectedYield = 0;
            foreach (TrionFacilitySacrificeSupplyGroup entry in supplies)
                if (entry.CountToTransfer > 0)
                    selectedYield += (double)entry.UnitYield * entry.CountToTransfer;
            foreach (TrionFacilitySacrificeSupplyGroup entry in supplies)
            {
                double otherYield = selectedYield - (double)entry.UnitYield * Math.Max(0, entry.CountToTransfer);
                entry.CapacityLimit = TrionFacilitySacrificeUtility.GetMaximumCount(entry.UnitYield,
                    entry.AvailableCount, facility.ResourceComp.CurrentResource, facility.ResourceComp.MaxResource,
                    otherYield, entry.CountToTransfer);
            }
        }

        /// <summary>选择变化或每秒更新一次，仅检查已选物品；不每帧扫描全地图。</summary>
        private void RefreshPreview()
        {
            previewYield = 0;
            problemKey = "BDP_TrionFacility_Sacrifice_Unavailable";
            if (facility.Destroyed || !facility.Spawned || facility.SacrificeComp == null) return;
            problemKey = "BDP_TrionFacility_Sacrifice_AlreadyPreparing";
            if (facility.SacrificeComp.HasPreparation) return;
            RefreshSelectionLimits();
            problemKey = "BDP_TrionFacility_Sacrifice_InvalidActor";
            Pawn actor = operatorWidget.Selected;
            if (!CanOperateHere(facility, actor)) return;
            problemKey = "BDP_TrionFacility_Sacrifice_Full";
            if (facility.ResourceComp.MaxResource <= 0
                || facility.ResourceComp.CurrentResource >= facility.ResourceComp.MaxResource) return;
            var batch = SelectedBatch();
            problemKey = "BDP_TrionFacility_Sacrifice_InvalidGoods";
            if (batch == null) return;
            problemKey = "BDP_TrionFacility_Sacrifice_NoGoods";
            if (batch.Count == 0) return;
            problemKey = "BDP_TrionFacility_Sacrifice_InvalidGoods";
            float lastUnitYield;
            if (!TrionFacilitySacrificeUtility.TryGetBatchYield(batch, out previewYield, out lastUnitYield)
                || batch.Any(entry => !entry.thing.Spawned || entry.thing.Map != facility.Map)) return;
            problemKey = "BDP_TrionFacility_Sacrifice_OverflowLimit";
            if (!TrionFacilitySacrificeUtility.CanAcceptBatch(previewYield, lastUnitYield,
                facility.ResourceComp.CurrentResource, facility.ResourceComp.MaxResource)) return;
            problemKey = null;
        }

        /// <summary>真实操作服从原版现场及身体条件，核心运行本身不以这些位置条件判断。</summary>
        private static bool CanOperateHere(Building_TrionFacility facility, Pawn actor) => actor != null
            && actor.Spawned && actor.Map == facility.Map && !actor.Downed
            && actor.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
            && facility.ControlComp.HasCoreAccess(actor);

        /// <summary>按钮与回车共同入口；登记完整批次，由原版搬运工作协作准备，不预留全部供物给献祭者。</summary>
        protected override void Start()
        {
            if (facility.Spawned && !facility.Destroyed) facility.ResourceComp.RefreshOperation();
            RefreshPreview();
            if (problemKey != null)
            {
                Messages.Message(problemKey.Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            Pawn actor = operatorWidget.Selected;
            var batch = SelectedBatch();
            if (!actor.CanReserveAndReach(facility, PathEndMode.InteractionCell, Danger.Deadly))
            {
                Messages.Message("BDP_TrionFacility_Sacrifice_Blocked".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            if (!facility.SacrificeComp.TryBegin(actor, batch, out problemKey))
            {
                Messages.Message(problemKey.Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            Close();
        }

        /// <summary>单人选择满足原版必需接口；不创建仪式角色、Lord或群体控制。</summary>
        private sealed class SingleOperatorWidget : IPawnRoleSelectionWidget
        {
            /// <summary>打开时本地图有权操作者。</summary>
            private readonly List<Pawn> candidates;
            /// <summary>下一次轻量预览核验的实际时间；原版窗口会暂停游戏刻。</summary>
            private float nextRefresh;
            /// <summary>唯一选定人物。</summary>
            public Pawn Selected;
            /// <summary>选择变更与低频刷新回调。</summary>
            public Action Refresh;
            /// <summary>只枚举一次；优先本核心皇室，无法现场工作时允许其契约者代办。</summary>
            public SingleOperatorWidget(Building_TrionFacility facility)
            {
                candidates = facility.Map.mapPawns.FreeColonistsSpawned.Where(pawn => CanOperateHere(facility, pawn)).ToList();
                Selected = candidates.Contains(facility.ControlComp.BoundRoyal)
                    ? facility.ControlComp.BoundRoyal : candidates.FirstOrDefault();
            }
            /// <summary>顶部短行选择唯一操作者，不为最多两人保留整幅左列。</summary>
            public void DrawPawnList(Rect listRectPawns)
            {
                if (candidates.Count == 0)
                {
                    Widgets.Label(listRectPawns, "BDP_TrionFacility_Sacrifice_NoOperator".Translate());
                    return;
                }
                Widgets.Label(new Rect(listRectPawns.x, listRectPawns.y + 7f, 80f, 28f),
                    "BDP_TrionFacility_Sacrifice_Operator".Translate());
                Rect selector = new Rect(listRectPawns.x + 80f, listRectPawns.y + 3f,
                    Math.Min(300f, listRectPawns.width - 80f), 34f);
                if (Widgets.ButtonText(selector, Selected != null ? Selected.LabelShortCap.ToString()
                    : "BDP_TrionFacility_Sacrifice_SelectOperator".Translate().ToString()))
                {
                    var options = candidates.Select(pawn => new FloatMenuOption(pawn.LabelShortCap, () =>
                        {
                            Selected = pawn;
                            Refresh?.Invoke();
                        })).ToList();
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            /// <summary>原版每帧调用本接口，但实际核验最多每秒一次。</summary>
            public void WindowUpdate()
            {
                if (Time.realtimeSinceStartup < nextRefresh) return;
                nextRefresh = Time.realtimeSinceStartup + 1f;
                Refresh?.Invoke();
            }
        }
    }
}
