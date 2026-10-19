using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// Trion 地标设施的独立资源容器。
    /// 它只负责资源余额、容量边界和按时间消耗，不知道献祭者与心情。
    /// </summary>
    public sealed class CompTrionFacilityResource : ThingComp
    {
        /// <summary>
        /// RimWorld 一天对应的 tick 数。
        /// </summary>
        private const float TicksPerDay = 60000f;

        /// <summary>
        /// 当前资源余额。
        /// </summary>
        private float currentResource;

        /// <summary>
        /// 上次资源结算的游戏 tick。
        /// </summary>
        private int lastSettledTick;

        /// <summary>
        /// 下一次组件自动结算的游戏 tick。
        /// 这是可重建的运行时状态，不进入存档。
        /// </summary>
        private int nextSettleTick;

        /// <summary>
        /// 是否已经建立过资源时间基准。
        /// </summary>
        private bool initialized;

        /// <summary>
        /// 上帝模式调试用的强制运行状态。
        /// 与原版 CompPowerTrader 的 PowerOn 一样保存为建筑运行状态。
        /// </summary>
        private bool forcedOperational;

        /// <summary>已经发布的运行开关；下游读取不检查人物或结算资源。</summary>
        private bool operational;

        /// <summary>本建筑的归属业务组件；只在检测或操作时读取。</summary>
        private CompTrionFacilityControl Control => parent.TryGetComp<CompTrionFacilityControl>();

        /// <summary>
        /// 读取资源组件配置。
        /// </summary>
        private CompProperties_TrionFacilityResource Props
        {
            get { return (CompProperties_TrionFacilityResource)props; }
        }

        /// <summary>
        /// 当前资源余额。
        /// 返回最近结算的余额；普通信息展示不推进时间或人物检查。
        /// </summary>
        public float CurrentResource
        {
            get { return currentResource; }
        }

        /// <summary>
        /// 接受下一次补充的容量门槛；已经入账的超额储量仍保留。
        /// </summary>
        public float MaxResource
        {
            get { return Props != null ? Mathf.Max(0f, Props.maxResource) : 0f; }
        }

        /// <summary>
        /// Def 提供的基础每日消耗速率。
        /// </summary>
        public float BaseDrainPerDay
        {
            get { return Props != null ? Mathf.Max(0f, Props.baseDrainPerDay) : 0f; }
        }

        /// <summary>
        /// 当前外部修正后的每日消耗速率。
        /// </summary>
        public float EffectiveDrainPerDay
        {
            get
            {
                return TrionFacilityDrainRateRegistry.ResolveDrainPerDay(
                    parent as Building,
                    BaseDrainPerDay);
            }
        }

        /// <summary>
        /// 最近一次统一检测发布的运行状态，支持低频检测的短暂延迟。
        /// </summary>
        public bool IsOperational
        {
            get { return operational; }
        }

        /// <summary>
        /// 当前是否由上帝模式调试开关强制运行。
        /// </summary>
        public bool IsForcedOperational
        {
            get { return forcedOperational; }
        }

        /// <summary>
        /// 组件生成后建立初始资源和时间基准。
        /// 新建筑从 0 资源开始，读档则保留存档余额。
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!initialized)
            {
                if (!respawningAfterLoad)
                {
                    currentResource = 0f;
                }

                initialized = true;
                lastSettledTick = GetCurrentTick();
            }

            ClampResource();
            ScheduleNextSettleTick(GetCurrentTick());
            RefreshOperationalGlow();
        }

        /// <summary>
        /// 按组件配置的间隔推进资源消耗。
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();
            int currentTick = GetCurrentTick();
            if (currentTick < nextSettleTick)
            {
                return;
            }

            // 资源结算可能改变设施运作状态；状态翻转时由结算方法即时刷新光照。
            RefreshOperation();
            ScheduleNextSettleTick(currentTick);
        }

        /// <summary>新建或地图最终初始化时重建运行状态；调用方随后登记地图许可。</summary>
        public void InitializeOperation()
        {
            ClampResource();
            Control?.RefreshBindingState();
            operational = forcedOperational || (currentResource > 0f && Control?.CanSustainOperation == true);
            ScheduleNextSettleTick(GetCurrentTick());
            RefreshOperationalGlow();
        }

        /// <summary>唯一检测路径：先按旧运行状态结算，再更新绑定者，最后发布一次结果。</summary>
        public void RefreshOperation()
        {
            bool wasOperational = operational;
            SettleResourceWithoutPublishing(GetCurrentTick());
            Control?.RefreshBindingState();
            PublishOperationalState(wasOperational);
        }

        /// <summary>
        /// 尝试向容器增加资源，返回实际接收量。
        /// 献祭物价值和献祭者资格由外部系统在调用前决定。
        /// </summary>
        /// <param name="amount">准备补充的资源量。</param>
        /// <returns>实际接收量；输入无效或容器已满时为 0。</returns>
        public float TryAddResource(float amount)
        {
            if (amount <= 0f || MaxResource <= 0f)
            {
                return 0f;
            }

            RefreshOperation();
            // 结算本身可能先停运；本次补充应以结算后的状态判断是否重新发布许可。
            bool wasOperational = IsOperational;
            if (currentResource >= MaxResource)
            {
                return 0f;
            }

            // 献祭业务先限制供物数量；容器只接收已核验金额，不截断最后一件的收益。
            currentResource += amount;
            ClampResource();
            PublishOperationalState(wasOperational);
            return amount;
        }

        /// <summary>
        /// 将资源结算到指定游戏 tick。
        /// 先推进时间基准，再判断当前余额，避免资源归零后重复扣除历史时间。
        /// </summary>
        /// <param name="currentTick">要结算到的目标 tick。</param>
        public void SettleResourceToTick(int currentTick)
        {
            bool wasOperational = IsOperational;
            SettleResourceWithoutPublishing(currentTick);
            PublishOperationalState(wasOperational);
        }

        /// <summary>按已发布状态结算旧区间；不在结算中读取当前健康或通知下游。</summary>
        private void SettleResourceWithoutPublishing(int currentTick)
        {
            if (!initialized)
            {
                initialized = true;
                lastSettledTick = currentTick;
                ClampResource();
                return;
            }

            if (currentTick <= lastSettledTick)
            {
                return;
            }

            int elapsedTicks = currentTick - lastSettledTick;
            lastSettledTick = currentTick;
            if (!operational || currentResource <= 0f)
            {
                currentResource = Mathf.Max(0f, currentResource);
                return;
            }

            float effectiveDrainPerDay = EffectiveDrainPerDay;
            float drain = effectiveDrainPerDay * elapsedTicks / TicksPerDay;
            currentResource = Mathf.Max(0f, currentResource - drain);
            ClampResource();
        }

        /// <summary>
        /// 保存资源余额与时间基准；下一次运行时重新计算自动结算 tick。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            // 保存前结清已发布状态的时间，读档不用用新健康状态猜保存前区间。
            if (Scribe.mode == LoadSaveMode.Saving)
                SettleResourceToTick(GetCurrentTick());
            Scribe_Values.Look(ref currentResource, "bdpTrionFacilityCurrentResource", 0f);
            Scribe_Values.Look(ref lastSettledTick, "bdpTrionFacilityLastSettledTick", 0);
            Scribe_Values.Look(ref initialized, "bdpTrionFacilityResourceInitialized", false);
            Scribe_Values.Look(ref forcedOperational, "bdpTrionFacilityForcedOperational", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ClampResource();
                operational = false;
                ScheduleNextSettleTick(GetCurrentTick());
            }
        }

        /// <summary>
        /// 生成检查面板中的资源状态文本。
        /// </summary>
        public override string CompInspectStringExtra()
        {
            return "BDP_TrionFacility_Resource".Translate(
                currentResource.ToString("0.##"),
                MaxResource.ToString("0.##"))
                + "\n"
                + "BDP_TrionFacility_Drain".Translate(EffectiveDrainPerDay.ToString("0.##"))
                + "\n"
                + (forcedOperational
                    ? "BDP_TrionFacility_Status_ForcedRunning".Translate()
                    : (IsOperational
                        ? "BDP_TrionFacility_Status_Running".Translate()
                        : "BDP_TrionFacility_Status_Stopped".Translate()));
        }

        /// <summary>
        /// 提供与原版电力组件一致的上帝模式调试按钮。
        /// 正式献祭系统仍只应使用 TryAddResource，不应依赖这些入口。
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (!DebugSettings.ShowDevGizmos)
            {
                yield break;
            }

            yield return CreateResourceDebugAction(
                "BDP_TrionFacility_Debug_Add50",
                "BDP_TrionFacility_Debug_Add50Desc",
                delegate { AdjustResourceForDebug(50f); });
            yield return CreateResourceDebugAction(
                "BDP_TrionFacility_Debug_Remove50",
                "BDP_TrionFacility_Debug_Remove50Desc",
                delegate { AdjustResourceForDebug(-50f); });
            yield return CreateResourceDebugAction(
                "BDP_TrionFacility_Debug_Fill",
                "BDP_TrionFacility_Debug_FillDesc",
                delegate { SetResourceForDebug(MaxResource); });
            yield return CreateResourceDebugAction(
                "BDP_TrionFacility_Debug_Empty",
                "BDP_TrionFacility_Debug_EmptyDesc",
                delegate { SetResourceForDebug(0f); });
            yield return new Command_Toggle
            {
                defaultLabel = "BDP_TrionFacility_Debug_ForceOperation".Translate(),
                defaultDesc = "BDP_TrionFacility_Debug_ForceOperationDesc".Translate(),
                isActive = () => forcedOperational,
                toggleAction = delegate
                {
                    RefreshOperation();
                    bool wasOperational = IsOperational;
                    forcedOperational = !forcedOperational;
                    PublishOperationalState(wasOperational);
                }
            };
        }

        /// <summary>
        /// 创建一个资源调试动作按钮。
        /// </summary>
        private Command_Action CreateResourceDebugAction(
            string labelKey,
            string descriptionKey,
            System.Action action)
        {
            return new Command_Action
            {
                defaultLabel = labelKey.Translate(),
                defaultDesc = descriptionKey.Translate(),
                action = action
            };
        }

        /// <summary>
        /// 在调试入口中按增减量调整余额，不丢弃已有超额储量。
        /// </summary>
        private void AdjustResourceForDebug(float delta)
        {
            RefreshOperation();
            bool wasOperational = IsOperational;
            currentResource = Mathf.Max(currentResource + delta, 0f);
            ClampResource();
            PublishOperationalState(wasOperational);
        }

        /// <summary>
        /// 在调试入口中设置非负余额；填满按钮显式使用容量门槛。
        /// </summary>
        private void SetResourceForDebug(float target)
        {
            RefreshOperation();
            bool wasOperational = IsOperational;
            currentResource = Mathf.Max(target, 0f);
            ClampResource();
            PublishOperationalState(wasOperational);
        }

        /// <summary>
        /// 在资源运作状态发生变化时刷新原版光照网格，避免每次资源读取都重建光照。
        /// </summary>
        private void PublishOperationalState(bool wasOperational)
        {
            operational = forcedOperational || (currentResource > 0f && Control?.CanSustainOperation == true);
            if (wasOperational != IsOperational)
            {
                Building_TrionFacility facility = parent as Building_TrionFacility;
                facility?.RefreshOperationalGlow();
                facility?.NotifyOperationalStateChanged();
            }
        }

        /// <summary>
        /// 在建筑组件完成生成或读档时刷新一次原版光照网格。
        /// </summary>
        private void RefreshOperationalGlow()
        {
            (parent as Building_TrionFacility)?.RefreshOperationalGlow();
        }

        /// <summary>
        /// 获取当前游戏 tick；游戏尚未初始化时安全返回 0。
        /// </summary>
        private static int GetCurrentTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        /// <summary>
        /// 只保留非负约束；生成、结算和读档均不得清除超额储量。
        /// </summary>
        private void ClampResource()
        {
            currentResource = Mathf.Max(currentResource, 0f);
        }

        /// <summary>
        /// 根据时间基准安排下一次自动结算。
        /// </summary>
        private void ScheduleNextSettleTick(int baselineTick)
        {
            int interval = Props != null ? Mathf.Max(1, Props.settleInterval) : 1;
            // 固定在各建筑自己的相位；手动操作不重置周期，避免一直推迟状态检测。
            int phase = (baselineTick % interval + parent.thingIDNumber % interval) % interval;
            nextSettleTick = baselineTick + interval - phase;
        }
    }
}
