using System.Collections.Generic;
using System.Linq;
using BDP.Content.Trion.Facility.Jobs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Facility.Sacrifice
{
    /// <summary>借用原版运输容器的搬运接入，不提供发射、燃料或分组操作。</summary>
    public sealed class CompProperties_TrionFacilitySacrifice : CompProperties_Transporter
    {
        /// <summary>每座核心独立准备一批供物，实物继续按原版容器规则更新。</summary>
        public CompProperties_TrionFacilitySacrifice()
        {
            compClass = typeof(CompTrionFacilitySacrifice);
            max1PerGroup = true;
            showOverallStats = false;
        }
    }

    /// <summary>原版负责协作搬运；业务保存献祭者、完整件数和玩家最后加入的一件的价值特征。</summary>
    public sealed class CompTrionFacilitySacrifice : CompTransporter
    {
        /// <summary>玩家选择的最终献祭者；普通搬运不检查此身份。</summary>
        private Pawn sacrificer;
        /// <summary>原版待装清单会随搬运缩减，单独保留批次完整性的验收基准。</summary>
        private int requestedCount;
        /// <summary>最后加入物品的实际定义；定义与固有市值共同决定单件收益，不依赖会被合堆销毁的来源。</summary>
        private ThingDef lastSelectedDef;
        /// <summary>最后加入时的固有市值；实际供物变化时拒绝，不猜另一件不同收益的供物为末项。</summary>
        private float lastSelectedMarketValue;
        /// <summary>备齐后最近一次低频检查的等待原因，不反复输出消息。</summary>
        private string waitingProblemKey;
        /// <summary>本核心业务入口。</summary>
        private CompTrionFacilityControl Control => parent.TryGetComp<CompTrionFacilityControl>();
        /// <summary>是否存在已确认的准备任务。</summary>
        public bool HasPreparation => groupID >= 0;
        /// <summary>已经进入容器的实际件数。</summary>
        public int LoadedCount => innerContainer.Sum(thing => thing.stackCount);
        /// <summary>仍需搬运的件数；失去来源引用也不能被误判为已搬齐。</summary>
        public int PendingCount => leftToLoad?.Sum(entry => entry.CountToTransfer) ?? 0;
        /// <summary>同时核对原版待装数量和真实实物，防止缺失或多装被当作完成。</summary>
        public bool IsReady => HasPreparation && requestedCount > 0
            && PendingCount == 0 && LoadedCount == requestedCount;
        /// <summary>核心不是飞行器，无需运输舱燃料口。</summary>
        public override bool RequiresFuelingPort => false;

        /// <summary>确认准备；使用原版合组与唯一装载编号，让原版搬运工作识别本核心。</summary>
        public bool TryBegin(Pawn actor, IList<ThingCountClass> batch, out string problemKey)
        {
            problemKey = "BDP_TrionFacility_Sacrifice_AlreadyPreparing";
            if (HasPreparation || innerContainer.Any) return false;
            double total;
            if (Control == null)
            {
                problemKey = "BDP_TrionFacility_Sacrifice_Unavailable";
                return false;
            }
            if (!Control.CanSacrifice(actor, batch, out total, out problemKey)) return false;
            sacrificer = actor;
            requestedCount = batch.Sum(entry => entry.Count);
            Thing lastSelected = batch[batch.Count - 1].thing;
            lastSelectedDef = lastSelected is MinifiedThing minified ? minified.InnerThing.def : lastSelected.def;
            lastSelectedMarketValue = lastSelected.MarketValue;
            leftToLoad = new List<TransferableOneWay>();
            foreach (ThingCountClass entry in batch)
            {
                var transferable = new TransferableOneWay();
                transferable.things.Add(entry.thing);
                // 原版按实际容器的匹配规则合组，拆堆后仍可正确扣减待装数量。
                AddToTheToLoadList(transferable, entry.Count);
            }
            TransporterUtility.InitiateLoading(new[] { this });
            waitingProblemKey = null;
            return true;
        }

        /// <summary>最终执行只读取实际已搬入的供物，不消费地图上尚待搬运的引用。</summary>
        public List<ThingCountClass> LoadedBatch()
        {
            // 只需确定相同单件收益：读取真实供物的定义与价值，不按搬运顺序或最高收益重排。
            Thing lastLoadedItem = innerContainer.FirstOrDefault(thing =>
                (thing is MinifiedThing minified ? minified.InnerThing.def : thing.def) == lastSelectedDef
                    && thing.MarketValue == lastSelectedMarketValue);
            if (lastLoadedItem == null) return null;
            var batch = innerContainer.Where(thing => thing != lastLoadedItem)
                .Select(thing => new ThingCountClass(thing, thing.stackCount)).ToList();
            batch.Add(new ThingCountClass(lastLoadedItem, lastLoadedItem.stackCount));
            return batch;
        }

        /// <summary>最终人物必须与准备窗口选择一致，权限变化仍由核心业务重新核验。</summary>
        public bool CanFinish(Pawn actor) => IsReady && actor == sacrificer && Control.HasCoreAccess(actor);

        /// <summary>取消或成功完成均清除准备状态；剩余实物由原版就近放回地图，不销毁。</summary>
        public void ClearPreparation(Map map)
        {
            sacrificer = null;
            requestedCount = 0;
            lastSelectedDef = null;
            lastSelectedMarketValue = 0;
            waitingProblemKey = null;
            CleanUpLoadingVars(map);
        }

        /// <summary>原版保存待装清单和实物，额外保存人物、初始件数及最后加入的一件的价值特征。</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            // 新挂组件没有旧存档节点：原版深读取会把构造的容器覆盖为空，编号则默认读成0。
            // 在加载入口恢复原版空闲初始状态；已有容器和正在准备的供物不得重置。
            if (Scribe.mode == LoadSaveMode.LoadingVars && innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
                groupID = -1;
                massCapacityOverride = -1f;
            }
            Scribe_References.Look(ref sacrificer, "bdpTrionFacilitySacrificer");
            Scribe_Values.Look(ref requestedCount, "bdpTrionFacilitySacrificeRequestedCount", 0);
            Scribe_Defs.Look(ref lastSelectedDef, "bdpTrionFacilitySacrificeLastDef");
            Scribe_Values.Look(ref lastSelectedMarketValue, "bdpTrionFacilitySacrificeLastMarketValue", 0f);
        }

        /// <summary>保留原版容器更新和搬运提醒；自动执行仅每250刻检查一次，不扫描地图供物。</summary>
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned || !parent.IsHashIntervalTick(250) || !IsReady) return;
            waitingProblemKey = "BDP_TrionFacility_Sacrifice_InvalidActor";
            if (sacrificer == null || !sacrificer.Spawned || sacrificer.Map != parent.Map
                || sacrificer.Dead || sacrificer.Downed || sacrificer.Drafted || sacrificer.InMentalState
                || !sacrificer.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return;
            if (sacrificer.CurJobDef == TrionFacilityJobDefOf.BDP_SacrificeToTrionFacility
                && sacrificer.CurJob.targetA.Thing == parent)
            {
                waitingProblemKey = null;
                return;
            }
            double total;
            if (!Control.CanSacrifice(sacrificer, LoadedBatch(), out total, out waitingProblemKey)) return;
            waitingProblemKey = "BDP_TrionFacility_Sacrifice_Blocked";
            if (!sacrificer.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Deadly)) return;
            waitingProblemKey = null;
            sacrificer.jobs.TryTakeOrderedJob(JobMaker.MakeJob(TrionFacilityJobDefOf.BDP_SacrificeToTrionFacility, parent));
        }

        /// <summary>拆除或销毁退回供物；随重力船移动时沿用原版容器保留规则，不发运输舱损毁文案。</summary>
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            if (parent.BeingTransportedOnGravship) return;
            ClearPreparation(map);
        }

        /// <summary>只保留献祭准备的取消入口，避免继承运输舱发射和多人分组按钮。</summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction?.IsPlayer != true || !parent.Spawned || (!HasPreparation && !innerContainer.Any)) yield break;
            yield return new Command_Action
            {
                defaultLabel = "BDP_TrionFacility_Sacrifice_CancelPreparation".Translate(),
                defaultDesc = "BDP_TrionFacility_Sacrifice_CancelPreparationDesc".Translate(),
                icon = CancelLoadCommandTex,
                action = () => ClearPreparation(parent.Map)
            };
        }

        /// <summary>在建筑检查面板显示总进度；逐类待搬／已搬实物由原版内容页展示。</summary>
        public override string CompInspectStringExtra()
        {
            if (!HasPreparation) return null;
            string text = "BDP_TrionFacility_Sacrifice_LoadingProgress".Translate(LoadedCount, requestedCount,
                PendingCount, sacrificer?.LabelShortCap ?? "BDP_TrionFacility_Sacrifice_SelectOperator".Translate()).ToString();
            if (IsReady) text += "\n" + (waitingProblemKey ?? "BDP_TrionFacility_Sacrifice_Ready").Translate();
            return text;
        }
    }
}
