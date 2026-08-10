using BDP.Core.Expressions;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 手动入口阶段记录。
    /// 它描述按钮发出前的中性入口事实。
    /// </summary>
    public sealed class ManualEntryRecord
    {
        /// <summary>
        /// 当前阶段正在执行的模块运行时。
        /// 它只服务作者读取自己的私有上下文，不对外公开。
        /// </summary>
        internal IRangedAttackModuleRuntime CurrentRuntime { get; set; }

        /// <summary>
        /// 当前手动入口所属 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前手动入口绑定的正式结果。
        /// </summary>
        internal FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前手动入口绑定的模块会话。
        /// </summary>
        internal RangedAttackModuleSession ModuleSession { get; set; }

        /// <summary>
        /// 当前手动入口所属的聚合组标识。
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// 当前手动入口对应的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前手动入口最终显示名称。
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 当前手动入口最终显示说明。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 当前手动入口显式图标路径。
        /// </summary>
        public string ManualEntryIconTexPath { get; set; }

        /// <summary>
        /// 当前手动入口阶段声明的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前手动入口是否应被隐藏。
        /// </summary>
        public bool Hidden { get; set; }

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
