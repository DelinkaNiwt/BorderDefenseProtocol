using System;
using System.Collections.Generic;
using BDP.Core.Genes;
using BDP.Core.Trion.Capacity;
using BDP.Core.Trion.Intensity;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 资源真值宿主。
    ///
    /// 第一阶段里，它只负责资源事实本身：
    /// - 当前量
    /// - 最大量
    /// - 已占用量
    /// - 预占用量
    /// - 聚合持续消耗
    /// - 自然恢复
    ///
    /// 它不直接承担：
    /// - 战斗体流程
    /// - 触发体槽位流程
    /// - 表达和攻击流程
    /// </summary>
    public sealed class CompTrion : ThingComp
    {
        /// <summary>
        /// RimWorld 一天对应的 tick 数。
        /// </summary>
        private const float TicksPerDay = 60000f;

        /// <summary>
        /// RimWorld 一秒对应的 tick 数。
        /// </summary>
        private const float TicksPerSecond = 60f;

        /// <summary>
        /// 当前总量。
        /// </summary>
        private float cur;

        /// <summary>
        /// 当前最大容量。
        /// </summary>
        private float max;

        /// <summary>
        /// 已正式锁出去的量。
        /// </summary>
        private float allocated;

        /// <summary>
        /// 预占用量。
        /// </summary>
        private float reserved;

        /// <summary>
        /// 是否冻结自然恢复。
        /// </summary>
        private bool frozen;

        /// <summary>
        /// 是否已经完成首次初始化，避免反复生成时覆盖状态。
        /// </summary>
        private bool initialized;

        /// <summary>
        /// 该人形角色永久不变的潜在 Trion 容量。
        /// </summary>
        private int trionCapacityPotential;

        /// <summary>
        /// 是否已经生成潜在容量；用于兼容缺少新字段的旧存档。
        /// </summary>
        private bool trionCapacityPotentialInitialized;

        /// <summary>
        /// 该人形角色永久不变的先天 Trion 释放力。
        /// </summary>
        private int innateTrionIntensity;

        /// <summary>
        /// 是否已经生成先天 Trion 释放力。
        /// </summary>
        private bool trionIntensityInitialized;

        /// <summary>
        /// 当前持续消耗注册表。
        /// key 是业务身份，value 的单位为 Trion/秒。
        /// </summary>
        private Dictionary<TrionDrainKey, float> drainRegistry;

        /// <summary>
        /// 下次持续消耗结算时机。
        /// </summary>
        private int drainSettleTick;

        /// <summary>
        /// 下次自然恢复结算时机。
        /// </summary>
        private int recoveryTick;

        /// <summary>
        /// 可用量从大于 0 变成小于等于 0 时触发。
        /// </summary>
        internal event Action AvailableDepleted;

        /// <summary>
        /// 总量从大于 0 变成小于等于 0 时触发。
        /// </summary>
        internal event Action TrionDepleted;

        /// <summary>
        /// Trion 正式服务。
        /// </summary>
        private readonly TrionService service;

        /// <summary>
        /// 初始化 owner 和各正式表面。
        /// </summary>
        public CompTrion()
        {
            service = new TrionService(this);
        }

        /// <summary>
        /// 对外统一返回 Trion 正式读取口。
        /// </summary>
        internal TrionService Service
        {
            get { return service; }
        }

        /// <summary>
        /// 读取当前 Comp 的 Trion 配置。
        /// </summary>
        private CompProperties_Trion Props
        {
            get { return (CompProperties_Trion)props; }
        }

        /// <summary>
        /// 当前 Trion 总量。
        /// </summary>
        internal float Cur
        {
            get { return cur; }
        }

        /// <summary>
        /// 当前 Trion 最大容量。
        /// </summary>
        internal float Max
        {
            get { return max; }
        }

        /// <summary>
        /// 已经正式锁出去、不能再自由挪用的量。
        /// </summary>
        internal float Allocated
        {
            get { return allocated; }
        }

        /// <summary>
        /// 预占用量。
        /// 这部分用于表达“准备要花”，但还没真正锁定。
        /// </summary>
        internal float Reserved
        {
            get { return reserved; }
        }

        /// <summary>
        /// 当前还能自由使用的 Trion 量。
        /// </summary>
        internal float Available
        {
            // 真正还能自由使用的量 = 当前总量 - 已正式占用量。
            get { return Mathf.Max(0f, cur - allocated); }
        }

        /// <summary>
        /// 当前每日自然恢复量。
        /// Pawn 宿主从 stat 派生，非 Pawn 宿主继续读 Props。
        /// </summary>
        internal float RecoveryPerDay
        {
            get { return ResolveRecoveryPerDay(); }
        }

        /// <summary>
        /// 当前所有持续消耗来源合并后的“每秒总消耗量”。
        /// </summary>
        internal float TotalDrainPerSecond
        {
            get
            {
                if (drainRegistry == null || drainRegistry.Count == 0)
                {
                    return 0f;
                }

                float total = 0f;
                foreach (KeyValuePair<TrionDrainKey, float> pair in drainRegistry)
                {
                    total += pair.Value;
                }

                return Mathf.Max(0f, total);
            }
        }

        /// <summary>
        /// 当前是否冻结自然恢复。
        /// </summary>
        internal bool Frozen
        {
            get { return frozen; }
        }

        /// <summary>
        /// 永久潜在容量；首次正式读取时会安全补齐旧存档。
        /// </summary>
        internal int TrionCapacityPotential
        {
            get
            {
                EnsureTrionCapacityPotentialInitialized();
                return trionCapacityPotential;
            }
        }

        /// <summary>
        /// 永久先天 Trion 释放力；首次正式读取时才生成。
        /// </summary>
        internal int InnateTrionIntensity
        {
            get
            {
                EnsureTrionIntensityInitialized();
                return innateTrionIntensity;
            }
        }

        /// <summary>
        /// 是否已经完成资源首次初始化；供基因区分创建阶段与运行阶段。
        /// </summary>
        internal bool HasCompletedInitialResourceSetup
        {
            get { return initialized; }
        }

        /// <summary>
        /// 初始化内部状态容器。
        /// </summary>
        public override void Initialize(CompProperties properties)
        {
            base.Initialize(properties);
            EnsureInternalState();
        }

        /// <summary>
        /// 生成或读档挂回 Pawn 时补齐运行状态。
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureInternalState();
            EnsureTrionCapacityPotentialInitialized();

            // 首次生成时按 Props 初始化。
            // 读档回来则直接保留存档状态。
            if (!respawningAfterLoad && !initialized)
            {
                RefreshDerivedStats();
                cur = Mathf.Clamp(max * Props.startPercent, 0f, max);
                allocated = 0f;
                reserved = 0f;
                frozen = false;
                initialized = true;
            }
            else
            {
                RefreshDerivedStats();
            }

            ClampState();
            ScheduleNextTicks();
        }

        /// <summary>
        /// 按游戏时间推进持续消耗和自然恢复。
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();

            // 资源自然过程允许依赖时间推进。
            // 所以聚合持续消耗与自然恢复仍在这里按 tick 结算。
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick >= drainSettleTick)
            {
                SettleDrain();
                drainSettleTick = currentTick + Mathf.Max(1, Props.drainSettleInterval);
            }

            if (!frozen && RecoveryPerDay > 0f && currentTick >= recoveryTick)
            {
                RecoverByInterval();
                recoveryTick = currentTick + Mathf.Max(1, Props.recoveryInterval);
            }
        }

        /// <summary>
        /// 存读档当前 Trion 真值。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref cur, "cur", 0f);
            Scribe_Values.Look(ref max, "max", 0f);
            Scribe_Values.Look(ref allocated, "allocated", 0f);
            Scribe_Values.Look(ref reserved, "reserved", 0f);
            Scribe_Values.Look(ref frozen, "frozen", false);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref trionCapacityPotential, "trionCapacityPotential", 0);
            Scribe_Values.Look(ref trionCapacityPotentialInitialized, "trionCapacityPotentialInitialized", false);
            Scribe_Values.Look(ref innateTrionIntensity, "innateTrionIntensity", 0);
            Scribe_Values.Look(ref trionIntensityInitialized, "trionIntensityInitialized", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureInternalState();
                EnsureTrionCapacityPotentialInitialized();
                drainRegistry.Clear();
                RefreshDerivedStats();
                ClampState();
                ScheduleNextTicks();
            }
        }

        /// <summary>
        /// 刷新当前宿主的 Trion 派生值。
        /// </summary>
        public void RefreshDerivedStats()
        {
            RefreshDerivedStats(TrionEligibilityChangeReason.Recalculate);
        }

        /// <summary>
        /// 由单一入口应用腺体资格变化，确保当前量与见底事件一致。
        /// </summary>
        public void RefreshDerivedStats(TrionEligibilityChangeReason reason)
        {
            float oldAvailable = Available;
            float oldCur = cur;
            max = ResolveMax();

            if (reason == TrionEligibilityChangeReason.RuntimeGranted)
            {
                cur = 0f;
            }

            ClampState();
            NotifyBoundaries(oldAvailable, oldCur);
        }

        /// <summary>
        /// 判断当前自由量是否足够支付这次消耗。
        /// </summary>
        internal bool CanAfford(float cost)
        {
            return cost <= 0f || Available >= cost;
        }

        /// <summary>
        /// 尝试一次性扣除指定消耗。
        /// 扣不动就直接失败，不做半扣。
        /// </summary>
        internal bool TryConsume(float cost)
        {
            if (cost <= 0f)
            {
                return true;
            }

            if (Available < cost)
            {
                return false;
            }

            // 这个入口是“要么全扣，要么不扣”。
            float oldAvailable = Available;
            float oldCur = cur;
            cur -= cost;
            ClampState();
            NotifyBoundaries(oldAvailable, oldCur);
            return true;
        }

        /// <summary>
        /// 尽力扣减，直到自由量见底为止。
        /// </summary>
        internal void ConsumeUntilDepleted(float amount)
        {
            if (amount <= 0f || Available <= 0f)
            {
                return;
            }

            // 这个入口允许“尽力而为”，直到可用量见底。
            float oldAvailable = Available;
            float oldCur = cur;
            float actualCost = Mathf.Min(amount, Available);
            cur -= actualCost;
            ClampState();
            NotifyBoundaries(oldAvailable, oldCur);
        }

        /// <summary>
        /// 设置当前预占用量。
        /// </summary>
        internal void SetReserved(float amount)
        {
            reserved = Mathf.Max(0f, amount);
        }

        /// <summary>
        /// 把一部分自由量正式锁成已占用量。
        /// </summary>
        internal bool Allocate(float amount)
        {
            if (amount <= 0f)
            {
                return false;
            }

            // Allocate 不减少总量，只把一部分自由量转成正式占用量。
            if (Available < amount)
            {
                return false;
            }

            allocated += amount;
            ClampState();
            return true;
        }

        /// <summary>
        /// 释放已经占用的量，让它重新回到自由量池。
        /// </summary>
        internal void Release(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            allocated = Mathf.Max(0f, allocated - amount);
            ClampState();
        }

        /// <summary>
        /// 注册一个持续消耗来源，单位为 Trion/秒。
        /// </summary>
        internal void RegisterDrain(TrionDrainKey key, float perSecond)
        {
            EnsureInternalState();

            if (perSecond <= 0f)
            {
                return;
            }

            // 同一个 key 再注册时直接覆盖，保证同一业务只保留一份当前值。
            drainRegistry[key] = perSecond;
        }

        /// <summary>
        /// 移除一个持续消耗来源。
        /// </summary>
        internal void UnregisterDrain(TrionDrainKey key)
        {
            if (drainRegistry == null)
            {
                return;
            }

            drainRegistry.Remove(key);
        }

        /// <summary>
        /// 取出当前持续消耗登记表快照。
        /// </summary>
        internal IReadOnlyDictionary<TrionDrainKey, float> GetDrainSnapshot()
        {
            EnsureInternalState();
            return new Dictionary<TrionDrainKey, float>(drainRegistry);
        }

        /// <summary>
        /// 设置是否冻结自然恢复。
        /// </summary>
        internal void SetFrozen(bool value)
        {
            frozen = value;
        }

        /// <summary>
        /// 按增减量调整当前值。
        /// 调试写入口也必须保持正式锁定下界不被击穿。
        /// </summary>
        internal TrionCurrentWriteResult AdjustCurrent(float delta)
        {
            float previousCurrent = cur;
            float currentFloor = ResolveCurrentFloor();
            float target = cur + delta;
            float nextCurrent = Mathf.Clamp(target, currentFloor, max);
            bool wasClamped = delta < 0f && nextCurrent > target;

            ApplyCurrentValue(nextCurrent);

            return new TrionCurrentWriteResult
            {
                Succeeded = true,
                WasClamped = wasClamped,
                PreviousCurrent = previousCurrent,
                Current = cur,
                  Message = wasClamped ? "BDP_Message_Trion_LockedMinimum".Translate() : null
            };
        }

        /// <summary>
        /// 尝试直接设置当前值。
        /// 置 0 这类明确请求在存在正式锁定下界时直接拒绝，不做偷偷夹值。
        /// </summary>
        internal TrionCurrentWriteResult TrySetCurrent(float target)
        {
            float previousCurrent = cur;
            float currentFloor = ResolveCurrentFloor();
            if (target <= 0f && currentFloor > 0f)
            {
                return new TrionCurrentWriteResult
                {
                    Succeeded = false,
                    WasClamped = false,
                    PreviousCurrent = previousCurrent,
                    Current = cur,
                    Message = "BDP_Message_Trion_LockedZero".Translate()
                };
            }

            float nextCurrent = Mathf.Clamp(target, currentFloor, max);
            bool wasClamped = nextCurrent != target;
            ApplyCurrentValue(nextCurrent);

            return new TrionCurrentWriteResult
            {
                Succeeded = true,
                WasClamped = wasClamped,
                PreviousCurrent = previousCurrent,
                Current = cur,
                Message = wasClamped ? "BDP_Message_Trion_LockedBelow".Translate() : null
            };
        }

        /// <summary>
        /// 确保持久容器和运行容器都已经就绪。
        /// </summary>
        private void EnsureInternalState()
        {
            if (drainRegistry == null)
            {
                // 聚合持续消耗统一收在这里。
                drainRegistry = new Dictionary<TrionDrainKey, float>();
            }
        }

        /// <summary>
        /// 根据当前游戏时刻安排下一次结算节点。
        /// </summary>
        private void ScheduleNextTicks()
        {
            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            drainSettleTick = currentTick + Mathf.Max(1, Props.drainSettleInterval);
            recoveryTick = currentTick + Mathf.Max(1, Props.recoveryInterval);
        }

        /// <summary>
        /// 只为人形 Pawn 生成一次永久潜在容量。
        /// 非人形宿主保持无潜在 Trion 事实。
        /// </summary>
        private void EnsureTrionCapacityPotentialInitialized()
        {
            if (trionCapacityPotentialInitialized || !(parent is Pawn pawn) || !pawn.RaceProps.Humanlike)
            {
                return;
            }

            trionCapacityPotential = TrionCapacityPotentialGenerator.Instance.Generate(
                TrionCapacityPotentialDistributionDefOf.BDP_TrionCapacityPotentialDistribution);
            trionCapacityPotentialInitialized = true;
        }

        /// <summary>
        /// 只为人形 Pawn 生成一次永久先天 Trion 释放力。
        /// 动物、机械体和非 Pawn 宿主不生成，并统一读取为零。
        /// </summary>
        private void EnsureTrionIntensityInitialized()
        {
            if (trionIntensityInitialized || !(parent is Pawn pawn) || !pawn.RaceProps.Humanlike)
            {
                return;
            }

            innateTrionIntensity = TrionIntensityGenerator.Instance.Generate(
                TrionIntensityDistributionDefOf.BDP_TrionIntensityDistribution);
            trionIntensityInitialized = true;
        }

        /// <summary>
        /// 应用新的当前值，并保持边界事件语义一致。
        /// </summary>
        private void ApplyCurrentValue(float nextCurrent)
        {
            float oldAvailable = Available;
            float oldCur = cur;
            cur = nextCurrent;
            ClampState();
            NotifyBoundaries(oldAvailable, oldCur);
        }

        /// <summary>
        /// 把持续消耗表折算到当前间隔并扣除。
        /// </summary>
        private void SettleDrain()
        {
            float totalDrainPerSecond = TotalDrainPerSecond;
            if (totalDrainPerSecond <= 0f || Available <= 0f)
            {
                return;
            }

            // 把“每秒总持续消耗”折算成当前结算间隔的一次实际扣减量。
            float oldAvailable = Available;
            float oldCur = cur;
            float perTick = totalDrainPerSecond / TicksPerSecond;
            float intervalCost = perTick * Mathf.Max(1, Props.drainSettleInterval);
            float actualCost = Mathf.Min(intervalCost, Available);

            cur -= actualCost;
            ClampState();
            NotifyBoundaries(oldAvailable, oldCur);
        }

        /// <summary>
        /// 把自然恢复量折算到当前间隔并回充。
        /// </summary>
        private void RecoverByInterval()
        {
            if (RecoveryPerDay <= 0f || cur >= max)
            {
                return;
            }

            // 自然恢复也按“每天总量 -> 当前间隔增量”折算。
            float perTick = RecoveryPerDay / TicksPerDay;
            float intervalGain = perTick * Mathf.Max(1, Props.recoveryInterval);
            cur = Mathf.Min(max, cur + intervalGain);
            ClampState();
        }

        /// <summary>
        /// 解析当前宿主的最大 Trion 容量。
        /// </summary>
        private float ResolveMax()
        {
            if (parent is Pawn pawn)
            {
                return Mathf.Max(0f, pawn.GetStatValue(TrionStatDefOf.BDP_TrionCapacity, true));
            }

            return Mathf.Max(0f, Props.baseMax);
        }

        /// <summary>
        /// 解析当前宿主的每日自然恢复量。
        /// </summary>
        private float ResolveRecoveryPerDay()
        {
            if (parent is Pawn pawn)
            {
                return Mathf.Max(0f, pawn.GetStatValue(TrionStatDefOf.BDP_TrionRecoveryRate, true));
            }

            return Mathf.Max(0f, Props.recoveryPerDay);
        }

        /// <summary>
        /// 解析当前值允许下降到的正式下界。
        /// 只要存在已锁定量，当前值就不能被写到它下面。
        /// </summary>
        private float ResolveCurrentFloor()
        {
            return Mathf.Max(allocated, 0f);
        }

        /// <summary>
        /// 把各项数值压回合法范围，避免存档和运行时漂移。
        /// </summary>
        private void ClampState()
        {
            max = Mathf.Max(0f, max);
            cur = Mathf.Clamp(cur, 0f, max);
            allocated = Mathf.Clamp(allocated, 0f, cur);
            reserved = Mathf.Max(0f, reserved);
        }

        /// <summary>
        /// 在可用量或总量跨过见底边界时抛出事件。
        /// </summary>
        private void NotifyBoundaries(float oldAvailable, float oldCur)
        {
            if (oldAvailable > 0f && Available <= 0f)
            {
                AvailableDepleted?.Invoke();
            }

            if (oldCur > 0f && cur <= 0f)
            {
                TrionDepleted?.Invoke();
            }
        }
    }
}
