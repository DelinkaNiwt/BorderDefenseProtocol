using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Prepare
{
    /// <summary>
    /// Prepare 阶段上下文。
    /// 模块只能读取入口和 Aim 结果，不能跳过去决定 projectile 行为。
    /// </summary>
    public readonly struct PrepareStageContext
    {
        internal PrepareStageContext(RangedAttackEntry entry, AimRecord aim, IRangedAttackModuleRuntime currentRuntime)
        {
            Entry = entry;
            Aim = aim;
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
            SemanticContext = entry != null ? entry.SemanticContext : null;
        }

        internal RangedAttackEntry Entry { get; }

        internal AimRecord Aim { get; }

        internal RangedAttackModuleSession ModuleSession { get; }

        /// <summary>
        /// 当前阶段绑定的统一攻击上下文。
        /// 前半段跨阶段传递只认这条主干。
        /// </summary>
        public AttackContext AttackContext { get; }

        internal IRangedAttackModuleRuntime CurrentRuntime { get; }

        /// <summary>
        /// 当前准备阶段所属攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前准备阶段所属施放 Pawn。
        /// </summary>
        public Pawn Pawn { get; }

        /// <summary>
        /// 当前准备阶段所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 进入 Aim 前确认下来的原始请求目标。
        /// </summary>
        public LocalTargetInfo RequestedTarget { get; }

        /// <summary>
        /// Aim 阶段裁定后的正式目标。
        /// </summary>
        public LocalTargetInfo FinalTarget { get; }

        /// <summary>
        /// Aim 阶段输出的命中倍率。
        /// </summary>
        public float AccuracyFactor { get; }

        /// <summary>
        /// Aim 阶段输出的强制失准半径。
        /// </summary>
        public float ForcedMissRadius { get; }

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
