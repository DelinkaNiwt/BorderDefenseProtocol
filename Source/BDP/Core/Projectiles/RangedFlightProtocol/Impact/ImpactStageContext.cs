using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// Impact 阶段上下文。
    /// 它只暴露 hit 结果和 projectile 输入，不负责下游防御总裁决。
    /// </summary>
    public readonly struct ImpactStageContext
    {
        internal ImpactStageContext(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight, HitRecord hit, IRangedAttackModuleRuntime currentRuntime, RangedAttackModuleSession moduleSession)
        {
            Projectile = projectile;
            InitPlan = initPlan;
            Flight = flight;
            Hit = hit;
            CurrentRuntime = currentRuntime;
            ModuleSession = moduleSession;
            Launcher = initPlan != null ? initPlan.Launcher : null;
            SourceThing = initPlan != null ? initPlan.SourceThing : null;
            AttackInstanceId = initPlan != null ? initPlan.AttackInstanceId : null;
            ResultId = initPlan != null ? initPlan.ResultId : null;
            EmitIndex = initPlan != null ? initPlan.EmitSequence : 0;
            AimTarget = initPlan != null ? initPlan.AimTarget : LocalTargetInfo.Invalid;
            CurrentTarget = flight != null ? flight.CurrentTarget : (initPlan != null ? initPlan.CurrentTarget : LocalTargetInfo.Invalid);
            AttackContextSnapshot = initPlan != null ? initPlan.AttackContextSnapshot : null;
            SemanticContext = initPlan != null ? initPlan.SemanticContext : null;
            IsValidHit = hit != null && hit.IsValidHit;
            HitThing = hit != null ? hit.HitThing : null;
            HitCell = hit != null ? hit.HitCell : default;
            ForceGround = hit != null && hit.ForceGround;
        }

        public Projectile Projectile { get; }

        internal ProjectileInitPlan InitPlan { get; }

        internal FlightRecord Flight { get; }

        internal HitRecord Hit { get; }

        internal IRangedAttackModuleRuntime CurrentRuntime { get; }

        internal RangedAttackModuleSession ModuleSession { get; }

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
        /// 当前命中沿用的瞄准目标。
        /// </summary>
        public LocalTargetInfo AimTarget { get; }

        /// <summary>
        /// 当前命中对应的追踪目标。
        /// </summary>
        public LocalTargetInfo CurrentTarget { get; }

        /// <summary>
        /// 当前投射物携带的统一攻击上下文快照。
        /// </summary>
        public AttackContextSnapshot AttackContextSnapshot { get; }

        /// <summary>
        /// 当前投射物继承的统一语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; }

        /// <summary>
        /// 当前命中记录是否判定为有效命中。
        /// </summary>
        public bool IsValidHit { get; }

        /// <summary>
        /// 当前命中到的 Thing。
        /// </summary>
        public Thing HitThing { get; }

        /// <summary>
        /// 当前命中的格子。
        /// </summary>
        public IntVec3 HitCell { get; }

        /// <summary>
        /// 当前命中是否强制落地。
        /// </summary>
        public bool ForceGround { get; }

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
