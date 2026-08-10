using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Semantics;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Aim
{
    /// <summary>
    /// Aim 阶段上下文。
    /// 模块只能读取这里给出的事实，不能借机跨阶段写别的结果。
    /// </summary>
    public readonly struct AimStageContext
    {
        internal AimStageContext(RangedAttackEntry entry, IRangedAttackModuleRuntime currentRuntime)
        {
            Entry = entry;
            ModuleSession = entry != null ? entry.ModuleSession : null;
            AttackContext = entry != null ? entry.AttackContext : new AttackContext();
            CurrentRuntime = currentRuntime;
            Pawn = entry != null ? entry.Pawn : null;
            Map = Pawn != null ? Pawn.Map : null;
            AttackInstanceId = entry != null ? entry.AttackInstanceId : null;
            RequestedTarget = entry != null ? entry.Target : LocalTargetInfo.Invalid;
            SemanticContext = entry != null ? entry.SemanticContext : null;
        }

        internal RangedAttackEntry Entry { get; }

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
        /// 进入 Aim 阶段前确认下来的请求目标。
        /// </summary>
        public LocalTargetInfo RequestedTarget { get; }

        /// <summary>
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
