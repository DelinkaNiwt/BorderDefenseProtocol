using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Semantics;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Flight
{
    /// <summary>
    /// Flight 阶段上下文。
    /// 模块只能读取当前 projectile 事实和正式飞行输入，不能直接改原版字段。
    /// </summary>
    public readonly struct FlightStageContext
    {
        internal FlightStageContext(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord previous, IRangedAttackModuleRuntime currentRuntime)
        {
            ModuleSession = null;
            Projectile = projectile;
            InitPlan = initPlan;
            Previous = previous;
            CurrentRuntime = currentRuntime;
            Map = projectile != null ? projectile.Map : null;
            Launcher = initPlan != null ? initPlan.Launcher : null;
            SourceThing = initPlan != null ? initPlan.SourceThing : null;
            AttackInstanceId = initPlan != null ? initPlan.AttackInstanceId : null;
            ResultId = initPlan != null ? initPlan.ResultId : null;
            EmitIndex = initPlan != null ? initPlan.EmitIndex : 0;
            AimTarget = previous != null ? previous.AimTarget : (initPlan != null ? initPlan.AimTarget : LocalTargetInfo.Invalid);
            CurrentTarget = previous != null ? previous.CurrentTarget : (initPlan != null ? initPlan.CurrentTarget : LocalTargetInfo.Invalid);
            CurrentDestination = previous != null ? previous.CurrentDestination : default;
            RedirectDestination = previous != null ? previous.RedirectDestination : null;
            AttackContextSnapshot = initPlan != null ? initPlan.AttackContextSnapshot : null;
            SemanticContext = initPlan != null ? initPlan.SemanticContext : null;
        }

        internal FlightStageContext(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord previous, IRangedAttackModuleRuntime currentRuntime, RangedAttackModuleSession moduleSession)
        {
            Projectile = projectile;
            InitPlan = initPlan;
            Previous = previous;
            CurrentRuntime = currentRuntime;
            ModuleSession = moduleSession;
            Map = projectile != null ? projectile.Map : null;
            Launcher = initPlan != null ? initPlan.Launcher : null;
            SourceThing = initPlan != null ? initPlan.SourceThing : null;
            AttackInstanceId = initPlan != null ? initPlan.AttackInstanceId : null;
            ResultId = initPlan != null ? initPlan.ResultId : null;
            EmitIndex = initPlan != null ? initPlan.EmitIndex : 0;
            AimTarget = previous != null ? previous.AimTarget : (initPlan != null ? initPlan.AimTarget : LocalTargetInfo.Invalid);
            CurrentTarget = previous != null ? previous.CurrentTarget : (initPlan != null ? initPlan.CurrentTarget : LocalTargetInfo.Invalid);
            CurrentDestination = previous != null ? previous.CurrentDestination : default;
            RedirectDestination = previous != null ? previous.RedirectDestination : null;
            AttackContextSnapshot = initPlan != null ? initPlan.AttackContextSnapshot : null;
            SemanticContext = initPlan != null ? initPlan.SemanticContext : null;
        }

        public Projectile Projectile { get; }

        internal ProjectileInitPlan InitPlan { get; }

        internal FlightRecord Previous { get; }

        internal IRangedAttackModuleRuntime CurrentRuntime { get; }

        internal RangedAttackModuleSession ModuleSession { get; }

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
        /// 当前飞行使用的瞄准目标。
        /// </summary>
        public LocalTargetInfo AimTarget { get; }

        /// <summary>
        /// 当前飞行实际追踪的目标。
        /// </summary>
        public LocalTargetInfo CurrentTarget { get; }

        /// <summary>
        /// 当前飞行阶段结论给出的目标世界坐标。
        /// </summary>
        public Vector3 CurrentDestination { get; }

        /// <summary>
        /// 当前飞行是否存在重定向目标坐标。
        /// </summary>
        public Vector3? RedirectDestination { get; }

        /// <summary>
        /// 当前投射物携带的统一攻击上下文快照。
        /// </summary>
        public AttackContextSnapshot AttackContextSnapshot { get; }

        /// <summary>
        /// 当前投射物继承的统一语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; }

        /// <summary>
        /// 读取当前模块自己的私有上下文。
        /// </summary>
        public T GetPrivateContext<T>()
            where T : class, IRangedModulePrivateContext
        {
            return AttackContextSnapshot != null
                ? AttackContextSnapshot.Get<T>(AttackContextKeys.GetModulePrivateKey(ResolveMountIndex()))
                : null;
        }

        /// <summary>
        /// 尝试读取当前模块自己的私有上下文。
        /// </summary>
        public bool TryGetPrivateContext<T>(out T context)
            where T : class, IRangedModulePrivateContext
        {
            context = GetPrivateContext<T>();
            return context != null;
        }

        /// <summary>
        /// 读取或创建当前模块自己的私有上下文。
        /// </summary>
        public T GetOrCreatePrivateContext<T>()
            where T : class, IRangedModulePrivateContext, new()
        {
            return GetPrivateContext<T>();
        }

        /// <summary>
        /// 解析当前模块运行时对应的挂载顺序索引。
        /// </summary>
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
