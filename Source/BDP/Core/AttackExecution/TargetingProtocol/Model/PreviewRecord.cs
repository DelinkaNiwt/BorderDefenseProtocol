using BDP.Core.Expressions;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Preview 阶段记录。
    /// 它描述原版 Targeter 正在预览的当前目标。
    /// </summary>
    public sealed class PreviewRecord
    {
        /// <summary>
        /// 当前记录绑定的统一攻击上下文。
        /// 预览阶段只从这里读取目标交互推进状态，不再挂独立主干字段。
        /// </summary>
        private AttackContext attackContext = new AttackContext();

        /// <summary>
        /// 当前阶段正在执行的模块运行时。
        /// 它只服务作者读取自己的私有上下文，不对外公开。
        /// </summary>
        internal IRangedAttackModuleRuntime CurrentRuntime { get; set; }

        /// <summary>
        /// 当前 Preview 阶段所属 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前 Preview 阶段绑定的正式结果。
        /// </summary>
        internal FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前 Preview 阶段借用的原版 Verb。
        /// </summary>
        public Verb Verb { get; set; }

        /// <summary>
        /// 当前 Preview 阶段绑定的模块会话。
        /// </summary>
        internal RangedAttackModuleSession ModuleSession { get; set; }

        /// <summary>
        /// 当前 Preview 阶段绑定的统一攻击上下文。
        /// 预览阶段需要读取交互链延续状态时，只从这里取节点。
        /// </summary>
        public AttackContext AttackContext
        {
            get { return attackContext; }
            set { attackContext = value ?? new AttackContext(); }
        }

        /// <summary>
        /// 当前 Preview 阶段绑定的正式结果标识。
        /// </summary>
        public string ResultId => Result != null ? Result.Id : null;

        /// <summary>
        /// 当前预览阶段声明的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前正在预览的目标。
        /// </summary>
        public LocalTargetInfo Target { get; set; }

        /// <summary>
        /// 当前阶段是否继续沿用原版射程圈绘制。
        /// </summary>
        public bool UseVanillaRangeRing { get; set; } = true;

        /// <summary>
        /// 当前阶段是否继续沿用原版目标高亮绘制。
        /// </summary>
        public bool UseVanillaTargetHighlight { get; set; } = true;

        /// <summary>
        /// 当前阶段是否继续沿用原版目标周边范围绘制。
        /// </summary>
        public bool UseVanillaFieldRadius { get; set; } = true;

        /// <summary>
        /// 当前阶段是否继续沿用原版鼠标附着绘制。
        /// </summary>
        public bool UseVanillaMouseAttachment { get; set; } = true;

        /// <summary>
        /// 当前阶段追加的正式扩展绘制项。
        /// </summary>
        public List<PreviewDrawItem> DrawItems { get; } = new List<PreviewDrawItem>();

        /// <summary>
        /// 当前预览阶段绑定的目标交互会话。
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
