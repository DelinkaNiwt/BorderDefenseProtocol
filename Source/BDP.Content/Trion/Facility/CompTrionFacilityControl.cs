using System.Collections.Generic;
using BDP.Content.Trion.Facility.Jobs;
using BDP.Content.Trion.Facility.Sacrifice;
using BDP.Content.Trion.Royalty.Contract;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Facility
{
    /// <summary>皇室核心业务参数；身份与人物契约从正式定义读取。</summary>
    public sealed class CompProperties_TrionFacilityControl : CompProperties
    {
        /// <summary>提供核心绑定资格的皇室基因。</summary>
        public GeneDef royalGene;
        /// <summary>识别本皇室具体契约者的健康记录。</summary>
        public HediffDef contractHediff;
        /// <summary>挂载正式核心业务组件。</summary>
        public CompProperties_TrionFacilityControl()
        {
            compClass = typeof(CompTrionFacilityControl);
        }
    }

    /// <summary>本核心的唯一皇室归属、公共操作权限和绑定状态快照。</summary>
    public sealed class CompTrionFacilityControl : ThingComp
    {
        /// <summary>持久归属；直接引用人物，旅行和跨图不替换引用。</summary>
        private Pawn boundRoyal;
        /// <summary>最近一次统一检测的清醒资格；查询不触碰人物健康。</summary>
        private bool canSustainOperation;
        /// <summary>最近一次检测的停运原因，用于只读检查面板。</summary>
        private string operationProblemKey = "BDP_TrionFacility_Unbound";
        /// <summary>读取业务参数。</summary>
        private CompProperties_TrionFacilityControl Props => (CompProperties_TrionFacilityControl)props;
        /// <summary>唯一余额与统一检查周期的组件。</summary>
        private CompTrionFacilityResource Resource => parent.TryGetComp<CompTrionFacilityResource>();
        /// <summary>当前保存的皇室归属；死亡或资格变化在下一检测周期解除。</summary>
        public Pawn BoundRoyal => boundRoyal;
        /// <summary>向资源组件提供只读资格快照，不扫描地图或实时查意识。</summary>
        public bool CanSustainOperation => canSustainOperation;

        /// <summary>持续资格只看人物生死、派系和有效基因，不以位置或生成状态替代。</summary>
        private bool IsRoyalMember(Pawn pawn) => pawn != null && !pawn.Dead
            && pawn.Faction?.IsPlayer == true
            && Props.royalGene != null && pawn.genes?.GetGene(Props.royalGene)?.Active == true;

        /// <summary>由资源组件统一周期调用；这里不另开逐刻检测或人物补丁。</summary>
        internal void RefreshBindingState()
        {
            if (boundRoyal != null && !IsRoyalMember(boundRoyal))
                ClearBinding();
            canSustainOperation = boundRoyal != null && boundRoyal.health.capacities.CanBeAwake;
            operationProblemKey = boundRoyal == null ? "BDP_TrionFacility_Unbound"
                : (canSustainOperation ? null : "BDP_TrionFacility_Unconscious");
        }

        /// <summary>归属解除的唯一写入点；不清储量、不改人物之间的契约。</summary>
        private void ClearBinding()
        {
            boundRoyal = null;
        }

        /// <summary>未绑定时，正式玩家皇室可建立归属；不要求已使用人物契约。</summary>
        public bool CanBind(Pawn actor) => parent.Faction?.IsPlayer == true && Resource != null
            && boundRoyal == null && IsRoyalMember(actor) && !actor.IsQuestLodger();

        /// <summary>只有本核心绑定皇室可以手动解除归属，契约者不能代办。</summary>
        public bool CanUnbind(Pawn actor) => actor != null && actor == boundRoyal
            && HasCoreAccess(actor);

        /// <summary>普通核心业务共用权限；未来普通操作沿用它，绑定／解绑另加条件。</summary>
        public bool HasCoreAccess(Pawn actor)
        {
            if (parent.Faction?.IsPlayer != true || actor == null || actor.Dead
                || actor.Faction?.IsPlayer != true || actor.IsQuestLodger()
                || !IsRoyalMember(boundRoyal))
                return false;
            if (actor == boundRoyal) return true;
            if (Props.contractHediff == null) return false;
            foreach (Hediff hediff in boundRoyal.health.hediffSet.hediffs)
            {
                var contract = hediff as Hediff_RoyalContract;
                if (contract != null && contract.def == Props.contractHediff
                    && contract.royalSigner && contract.partner == actor)
                    return true;
            }
            return false;
        }

        /// <summary>独立绑定动作；先结算旧状态，再建立归属和发布新运行结果。</summary>
        public bool TryBind(Pawn actor)
        {
            Resource?.RefreshOperation();
            if (!CanBind(actor)) return false;
            boundRoyal = actor;
            Resource.RefreshOperation();
            return true;
        }

        /// <summary>独立手动解绑动作；余额保留，清醒或欠费等状态不改变权限分工。</summary>
        public bool TryUnbind(Pawn actor)
        {
            Resource?.RefreshOperation();
            if (!CanUnbind(actor)) return false;
            ClearBinding();
            Resource.RefreshOperation();
            return true;
        }

        /// <summary>准备、自动派工与最终提交共用核验；接受现场供物或本核心已经搬入的实物。</summary>
        public bool CanSacrifice(Pawn actor, IList<ThingCountClass> batch, out double total, out string problemKey)
        {
            total = 0;
            Resource?.RefreshOperation();
            problemKey = "BDP_TrionFacility_Sacrifice_InvalidActor";
            if (Resource == null || !parent.Spawned || !HasCoreAccess(actor)
                || !actor.Spawned || actor.Map != parent.Map) return false;
            problemKey = "BDP_TrionFacility_Sacrifice_Full";
            if (Resource.MaxResource <= 0 || Resource.CurrentResource >= Resource.MaxResource) return false;
            problemKey = "BDP_TrionFacility_Sacrifice_InvalidGoods";
            float lastUnitYield;
            if (!TrionFacilitySacrificeUtility.TryGetBatchYield(batch, out total, out lastUnitYield)) return false;
            CompTransporter cargo = parent.TryGetComp<CompTransporter>();
            foreach (ThingCountClass entry in batch)
                if (!(entry.thing.Spawned && entry.thing.Map == parent.Map)
                    && cargo?.innerContainer.Contains(entry.thing) != true) return false;
            problemKey = "BDP_TrionFacility_Sacrifice_OverflowLimit";
            if (!TrionFacilitySacrificeUtility.CanAcceptBatch(total, lastUnitYield,
                Resource.CurrentResource, Resource.MaxResource)) return false;
            problemKey = null;
            return true;
        }

        /// <summary>独立献祭提交；核验整批后一次入账，只消费实际选中数量，不绑定或解绑。</summary>
        public bool TrySacrifice(Pawn actor, IList<ThingCountClass> batch, out string problemKey)
        {
            double total;
            if (!CanSacrifice(actor, batch, out total, out problemKey)) return false;
            // 同一任务步骤内没有模拟刻穿插；先确认全额接收，再消费，拒绝不吞供物。
            problemKey = "BDP_TrionFacility_Sacrifice_Full";
            if (Resource.TryAddResource((float)total) <= 0) return false;
            foreach (ThingCountClass entry in batch)
                entry.thing.SplitOff(entry.Count).Destroy(DestroyMode.Vanish);
            problemKey = null;
            return true;
        }

        /// <summary>保存原版人物引用；资格快照在引用解析和建筑初始化后重建。</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref boundRoyal, "bdpTrionFacilityBoundRoyal");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                canSustainOperation = false;
        }

        /// <summary>只展示持久归属和缓存停运原因，不在每帧推动状态检测。</summary>
        public override string CompInspectStringExtra()
        {
            string text = boundRoyal == null ? "BDP_TrionFacility_Unbound".Translate().ToString()
                : "BDP_TrionFacility_BoundRoyal".Translate(boundRoyal.LabelShortCap).ToString();
            return boundRoyal != null && operationProblemKey != null
                ? text + "\n" + operationProblemKey.Translate().ToString() : text;
        }

        /// <summary>正式建筑入口；操作者在菜单中选择，后续用原版任务亲自执行。</summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            if (parent.Faction?.IsPlayer != true || parent.Map == null) yield break;
            bool bind = boundRoyal == null;
            yield return new Command_Action
            {
                defaultLabel = (bind ? "BDP_TrionFacility_Bind" : "BDP_TrionFacility_Unbind").Translate(),
                defaultDesc = (bind ? "BDP_TrionFacility_BindDesc" : "BDP_TrionFacility_UnbindDesc").Translate(),
                icon = parent.def.uiIcon,
                action = () => ShowBindingMenu(bind)
            };
            if (!bind && parent.TryGetComp<CompTransporter>()?.LoadingInProgressOrReadyToLaunch != true)
                yield return new Command_Action
                {
                    defaultLabel = "BDP_TrionFacility_Sacrifice".Translate(),
                    defaultDesc = "BDP_TrionFacility_SacrificeDesc".Translate(),
                    icon = parent.def.uiIcon,
                    action = () => Find.WindowStack.Add(new Dialog_TrionFacilitySacrifice(parent as Building_TrionFacility))
                };
        }

        /// <summary>只在玩家点击时枚举本地图候选人；地图位置仅限制行走操作，不限制运行。</summary>
        private void ShowBindingMenu(bool bind)
        {
            var options = new List<FloatMenuOption>();
            foreach (Pawn actor in parent.Map.mapPawns.FreeColonistsSpawned)
            {
                if (!(bind ? CanBind(actor) : CanUnbind(actor))) continue;
                options.Add(new FloatMenuOption(actor.LabelShortCap, () =>
                    actor.jobs.TryTakeOrderedJob(JobMaker.MakeJob(bind
                        ? TrionFacilityJobDefOf.BDP_BindTrionFacility
                        : TrionFacilityJobDefOf.BDP_UnbindTrionFacility, parent))));
            }
            if (options.Count == 0)
                options.Add(new FloatMenuOption("BDP_TrionFacility_NoEligibleRoyal".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
