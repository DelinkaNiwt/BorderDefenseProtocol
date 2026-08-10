using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Fire
{
    /// <summary>
    /// Fire 阶段上下文。
    /// 模块只能读取 Entry/Aim/Prepare 三段正式结果，不能直接碰 projectile 宿主。
    /// </summary>
    public readonly struct FireStageContext
    {
        internal FireStageContext(RangedAttackEntry entry, AimRecord aim, PrepareRecord prepare, IRangedAttackModuleRuntime currentRuntime)
        {
            Entry = entry;
            Aim = aim;
            Prepare = prepare;
            ModuleSession = entry != null ? entry.ModuleSession : null;
            AttackContext = entry != null ? entry.AttackContext : new AttackContext();
            CurrentRuntime = currentRuntime;
            Pawn = entry != null ? entry.Pawn : null;
            Map = Pawn != null ? Pawn.Map : null;
            AttackInstanceId = entry != null ? entry.AttackInstanceId : null;
            RequestedTarget = entry != null ? entry.Target : LocalTargetInfo.Invalid;
            FinalTarget = aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid;
            AccuracyFactor = aim != null ? aim.AccuracyFactor : 1f;
            ForcedMissRadius = aim != null ? aim.ForcedMissRadius : 0f;
            ResourceCost = prepare != null ? prepare.ResourceCost : 0f;
            MinimumRequired = prepare != null ? prepare.MinimumRequired : 0f;
            SkipResourceConsumption = prepare != null && prepare.SkipResourceConsumption;
            RequiresWarmup = prepare != null && prepare.RequiresWarmup;
            WarmupTicks = prepare != null ? prepare.WarmupTicks : 0;
            RequiresCharge = prepare != null && prepare.RequiresCharge;
            ChargeTicks = prepare != null ? prepare.ChargeTicks : 0;
            RequiresLock = prepare != null && prepare.RequiresLock;
            LockSatisfied = prepare != null && prepare.LockSatisfied;
            SemanticContext = entry != null ? entry.SemanticContext : null;
        }

        internal RangedAttackEntry Entry { get; }

        internal AimRecord Aim { get; }

        internal PrepareRecord Prepare { get; }

        internal RangedAttackModuleSession ModuleSession { get; }

        /// <summary>
        /// 当前阶段绑定的统一攻击上下文。
        /// 前半段跨阶段传递只认这条主干。
        /// </summary>
        public AttackContext AttackContext { get; }

        internal IRangedAttackModuleRuntime CurrentRuntime { get; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前阶段所属施放 Pawn。
        /// </summary>
        public Pawn Pawn { get; }

        /// <summary>
        /// 当前阶段所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 进入 Fire 阶段前的请求目标。
        /// </summary>
        public LocalTargetInfo RequestedTarget { get; }

        /// <summary>
        /// Aim 阶段裁决后的最终目标。
        /// </summary>
        public LocalTargetInfo FinalTarget { get; }

        /// <summary>
        /// Aim 阶段给出的精度倍率。
        /// </summary>
        public float AccuracyFactor { get; }

        /// <summary>
        /// Aim 阶段给出的强制失准半径。
        /// </summary>
        public float ForcedMissRadius { get; }

        /// <summary>
        /// 进入 Fire 前确认的资源消耗。
        /// </summary>
        public float ResourceCost { get; }

        /// <summary>
        /// 进入 Fire 前要求的最小资源量。
        /// </summary>
        public float MinimumRequired { get; }

        /// <summary>
        /// 当前发射是否跳过资源扣除。
        /// </summary>
        public bool SkipResourceConsumption { get; }

        /// <summary>
        /// 当前发射是否要求预热。
        /// </summary>
        public bool RequiresWarmup { get; }

        /// <summary>
        /// 当前发射要求的预热 Tick 数。
        /// </summary>
        public int WarmupTicks { get; }

        /// <summary>
        /// 当前发射是否要求充能。
        /// </summary>
        public bool RequiresCharge { get; }

        /// <summary>
        /// 当前发射要求的充能 Tick 数。
        /// </summary>
        public int ChargeTicks { get; }

        /// <summary>
        /// 当前发射是否要求锁定。
        /// </summary>
        public bool RequiresLock { get; }

        /// <summary>
        /// 当前锁定要求是否已经满足。
        /// </summary>
        public bool LockSatisfied { get; }

        /// 当前攻击继承的统一语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; }

        /// <summary>
        /// 读取当前模块自己的私有上下文。
        /// </summary>
        public T GetPrivateContext<T>()
            where T : class, IRangedModulePrivateContext
        {
            return ModuleSession != null && CurrentRuntime != null
                ? ModuleSession.GetPrivateContext<T>(CurrentRuntime)
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
            return ModuleSession != null && CurrentRuntime != null
                ? ModuleSession.GetOrCreatePrivateContext<T>(CurrentRuntime)
                : null;
        }
    }
}
