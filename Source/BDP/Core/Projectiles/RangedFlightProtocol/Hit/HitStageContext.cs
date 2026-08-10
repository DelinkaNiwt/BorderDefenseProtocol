using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Semantics;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Hit
{
    /// <summary>
    /// Hit 阶段上下文。
    /// 它只暴露命中现场，不允许模块在这里直接生成伤害计划。
    /// </summary>
    public readonly struct HitStageContext
    {
        internal HitStageContext(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight, ArrivalRecord arrival, Thing hitThing, IRangedAttackModuleRuntime currentRuntime, RangedAttackModuleSession moduleSession)
        {
            Projectile = projectile;
            InitPlan = initPlan;
            Flight = flight;
            Arrival = arrival;
            CurrentRuntime = currentRuntime;
            ModuleSession = moduleSession;
            HitThing = hitThing;
            Map = projectile != null ? projectile.Map : null;
            Launcher = initPlan != null ? initPlan.Launcher : null;
            SourceThing = initPlan != null ? initPlan.SourceThing : null;
            AttackInstanceId = initPlan != null ? initPlan.AttackInstanceId : null;
            ResultId = initPlan != null ? initPlan.ResultId : null;
            EmitIndex = initPlan != null ? initPlan.EmitIndex : 0;
            AimTarget = initPlan != null ? initPlan.AimTarget : LocalTargetInfo.Invalid;
            CurrentTarget = flight != null ? flight.CurrentTarget : (initPlan != null ? initPlan.CurrentTarget : LocalTargetInfo.Invalid);
            CurrentDestination = flight != null ? flight.CurrentDestination : default;
            ArrivalContinuesFlight = arrival != null && arrival.ContinueFlight;
            ArrivalNextDestination = arrival != null ? arrival.NextDestination : default;
            HitCell = projectile != null ? projectile.Position : IntVec3.Invalid;
            AttackContextSnapshot = initPlan != null ? initPlan.AttackContextSnapshot : null;
            SemanticContext = initPlan != null ? initPlan.SemanticContext : null;
        }

        /// <summary>
        /// 当前命中阶段所属投射物宿主。
        /// </summary>
        public Projectile Projectile { get; }

        internal ProjectileInitPlan InitPlan { get; }

        internal FlightRecord Flight { get; }

        internal ArrivalRecord Arrival { get; }

        internal IRangedAttackModuleRuntime CurrentRuntime { get; }

        internal RangedAttackModuleSession ModuleSession { get; }

        /// <summary>
        /// 原版命中流程传入的命中 Thing。
        /// </summary>
        public Thing HitThing { get; }

        /// <summary>
        /// 当前投射物所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 当前投射物对应的发射 Pawn。
        /// </summary>
        public Pawn Launcher { get; }

        /// <summary>
        /// 当前投射物归属的来源宿主。
        /// </summary>
        public Thing SourceThing { get; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前投射物绑定的正式结果标识。
        /// </summary>
        public string ResultId { get; }

        /// <summary>
        /// 当前投射物在本次发射中的 emit 序号。
        /// </summary>
        public int EmitIndex { get; }

        /// <summary>
        /// 当前投射物沿用的瞄准目标。
        /// </summary>
        public LocalTargetInfo AimTarget { get; }

        /// <summary>
        /// 当前飞行正在追踪的目标。
        /// </summary>
        public LocalTargetInfo CurrentTarget { get; }

        /// <summary>
        /// 当前飞行阶段给出的目的地。
        /// </summary>
        public Vector3 CurrentDestination { get; }

        /// <summary>
        /// Arrival 阶段是否裁定继续飞行。
        /// </summary>
        public bool ArrivalContinuesFlight { get; }

        /// <summary>
        /// Arrival 阶段裁定出的下一段目的地。
        /// </summary>
        public Vector3 ArrivalNextDestination { get; }

        /// <summary>
        /// 当前命中发生的格子。
        /// </summary>
        public IntVec3 HitCell { get; }

        /// <summary>
        /// 当前投射物携带的统一攻击上下文快照。
        /// </summary>
        public AttackContextSnapshot AttackContextSnapshot { get; }

        /// <summary>
        /// 当前投射物继承的统一语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; }

        public T GetPrivateContext<T>()
            where T : class, IRangedModulePrivateContext
        {
            return AttackContextSnapshot != null
                ? AttackContextSnapshot.Get<T>(AttackContextKeys.GetModulePrivateKey(ResolveMountIndex()))
                : null;
        }

        public bool TryGetPrivateContext<T>(out T context)
            where T : class, IRangedModulePrivateContext
        {
            context = GetPrivateContext<T>();
            return context != null;
        }

        public T GetOrCreatePrivateContext<T>()
            where T : class, IRangedModulePrivateContext, new()
        {
            return GetPrivateContext<T>();
        }

        private int ResolveMountIndex()
        {
            if (ModuleSession?.Slots == null || CurrentRuntime == null)
            {
                return -1;
            }

            for (int i = 0; i < ModuleSession.Slots.Count; i++)
            {
                RangedAttackModuleSlot slot = ModuleSession.Slots[i];
                if (slot != null && ReferenceEquals(slot.Runtime, CurrentRuntime))
                {
                    return slot.MountIndex;
                }
            }

            return -1;
        }
    }
}
