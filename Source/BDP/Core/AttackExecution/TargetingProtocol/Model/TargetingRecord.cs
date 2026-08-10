using BDP.Core.Expressions;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Targeting 阶段记录。
    /// 它承接原版 Targeter 需要的中性输入表面。
    /// </summary>
    public sealed class TargetingRecord
    {
        /// <summary>
        /// 当前记录绑定的统一攻击上下文。
        /// 目标交互输入状态与交互推进状态都从这里读写。
        /// </summary>
        private AttackContext attackContext = new AttackContext();

        /// <summary>
        /// 当前阶段正在执行的模块运行时。
        /// 它只服务作者读取自己的私有上下文，不对外公开。
        /// </summary>
        internal IRangedAttackModuleRuntime CurrentRuntime { get; set; }

        /// <summary>
        /// 当前 Targeting 阶段所属 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前 Targeting 阶段绑定的正式结果。
        /// </summary>
        internal FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前 Targeting 阶段借用的原版 Verb。
        /// </summary>
        public Verb Verb { get; set; }

        /// <summary>
        /// 当前 Targeting 阶段绑定的模块会话。
        /// </summary>
        internal RangedAttackModuleSession ModuleSession { get; set; }

        /// <summary>
        /// 当前 Targeting 阶段绑定的统一攻击上下文。
        /// 这是目标交互链跨轮次延续状态的唯一正式主干。
        /// </summary>
        public AttackContext AttackContext
        {
            get { return attackContext; }
            set { attackContext = value ?? new AttackContext(); }
        }

        /// <summary>
        /// 当前 Targeting 阶段绑定的正式结果标识。
        /// </summary>
        public string ResultId => Result != null ? Result.Id : null;

        /// <summary>
        /// 当前阶段最终判定出的近战标记。
        /// </summary>
        public bool IsMeleeAttack { get; set; }

        /// <summary>
        /// 当前目标选择阶段声明的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前阶段是否允许进入原版目标选择流程。
        /// </summary>
        public bool Targetable { get; set; } = true;

        /// <summary>
        /// 当前阶段是否允许原版 Shift 多选。
        /// </summary>
        public bool MultiSelect { get; set; }

        /// <summary>
        /// 当前阶段是否隐藏 Pawn tooltip。
        /// </summary>
        public bool HidePawnTooltips { get; set; }

        /// <summary>
        /// 当前阶段提供给原版 Targeter 的目标参数。
        /// </summary>
        public TargetingParameters TargetingParameters { get; set; }

        /// <summary>
        /// 当前扩展输入状态。
        /// </summary>
        public TargetingInputState InputState => AttackContext.GetOrCreate<TargetingInputState>(AttackContextKeys.TargetingInputState);

        /// <summary>
        /// 当前瞄准过程绑定的目标交互会话。
        /// </summary>
        public TargetingInteractionSession InteractionSession => AttackContext.GetOrCreate<TargetingInteractionSession>(AttackContextKeys.TargetingInteraction);

        /// <summary>
        /// 当前这一轮目标交互输入帧。
        /// </summary>
        public TargetingInputFrame InputFrame { get; set; } = new TargetingInputFrame();

        /// <summary>
        /// 当前阶段可用的中性段合法性查询面。
        /// 模块可用它检查“任意起点到候选目标”这一段是否按现有目标规则成立，但它不代表最终确认是否允许下单。
        /// </summary>
        public ITargetingSegmentLegalityService SegmentLegality { get; set; } = DefaultTargetingSegmentLegalityService.Instance;

        /// <summary>
        /// 当前鼠标候选点是否合法，是否已被模块显式接管。
        /// 它只服务 `current-frame candidate（当前这一帧候选点）` 的即时反馈；为 `false` 时，宿主继续沿用原版 `Verb（动词）` 的即时合法性判定。
        /// </summary>
        public bool HasCurrentTargetLegalityOverride { get; set; }

        /// <summary>
        /// 当前鼠标候选点在模块显式接管后的合法性真值。
        /// 它只服务 Targeter 当前这一帧的即时反馈，不承载正式确认结果。
        /// </summary>
        public bool CurrentTargetIsLegal { get; set; } = true;

        /// <summary>
        /// 当前鼠标候选点在模块显式接管后给出的拒绝原因。
        /// 宿主只有在当前候选点不合法时才会把它反馈给玩家；它不承载正式确认失败原因。
        /// </summary>
        public string CurrentTargetRejectReason { get; set; }

        /// <summary>
        /// 当前这一轮输入处理后形成的推进裁决。
        /// </summary>
        public TargetingAdvanceDecision AdvanceDecision { get; set; } = new TargetingAdvanceDecision();

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
