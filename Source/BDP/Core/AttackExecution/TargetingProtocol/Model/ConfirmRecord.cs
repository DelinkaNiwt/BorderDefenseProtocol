using BDP.Core.Expressions;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Confirm 阶段记录。
    /// 它描述玩家完成目标确认时要写回执行边界的中性事实。
    /// </summary>
    public sealed class ConfirmRecord
    {
        /// <summary>
        /// 当前记录绑定的统一攻击上下文。
        /// Confirm 阶段只在这条主干上做最后修改与冻结准备。
        /// </summary>
        private AttackContext attackContext = new AttackContext();

        /// <summary>
        /// 当前阶段正在执行的模块运行时。
        /// 它只服务作者读取自己的私有上下文，不对外公开。
        /// </summary>
        internal IRangedAttackModuleRuntime CurrentRuntime { get; set; }

        /// <summary>
        /// 当前 Confirm 阶段所属 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前 Confirm 阶段绑定的正式结果。
        /// </summary>
        internal FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前 Confirm 阶段绑定的模块会话。
        /// </summary>
        internal RangedAttackModuleSession ModuleSession { get; set; }

        /// <summary>
        /// 当前 Confirm 阶段绑定的统一攻击上下文。
        /// 这是确认阶段写回冻结事实的唯一正式主干。
        /// </summary>
        public AttackContext AttackContext
        {
            get { return attackContext; }
            set { attackContext = value ?? new AttackContext(); }
        }

        /// <summary>
        /// 当前 Confirm 阶段绑定的正式结果标识。
        /// </summary>
        public string ResultId => Result != null ? Result.Id : null;

        /// <summary>
        /// 当前确认阶段最终冻结下来的导航目标。
        /// 它会写入 `NavigationTarget（导航目标）`，服务后续导航、朝向与首段物理取值。
        /// </summary>
        public LocalTargetInfo Target { get; set; }

        /// <summary>
        /// 当前确认阶段最终冻结下来的语义目标。
        /// 未显式写入时，下游应回退到 `Target（导航目标）`，避免把“缺省语义”误解成额外业务规则。
        /// </summary>
        public LocalTargetInfo SemanticTarget { get; set; }

        /// <summary>
        /// 当前确认使用的投影版本号。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 当前确认写回执行边界时采用的来源原因。
        /// </summary>
        public AttackExecutionReason Reason { get; set; }

        /// <summary>
        /// 当前确认写回执行边界时采用的派单意图。
        /// </summary>
        public AttackDispatchIntent DispatchIntent { get; set; }

        /// <summary>
        /// 当前确认阶段声明的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前确认结果是否允许正式下单。
        /// </summary>
        public bool IsAllowed { get; set; } = true;

        /// <summary>
        /// 当前确认被拒绝时的原因。
        /// </summary>
        public string RejectReason { get; set; }

        /// <summary>
        /// 当前确认阶段可读取的输入状态节点。
        /// 它来自统一攻击上下文，不再是独立主干字段。
        /// </summary>
        public TargetingInputState InputState => AttackContext.GetOrCreate<TargetingInputState>(AttackContextKeys.TargetingInputState);

        /// <summary>
        /// 当前确认阶段可读取的交互会话节点。
        /// 它来自统一攻击上下文，不再是独立主干字段。
        /// </summary>
        public TargetingInteractionSession InteractionSession => AttackContext.GetOrCreate<TargetingInteractionSession>(AttackContextKeys.TargetingInteraction);

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
