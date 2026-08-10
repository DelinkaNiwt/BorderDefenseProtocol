using BDP.Core.Chips;
using BDP.Core.Combos;
using BDP.Core.Expressions;

namespace BDP.Core.Expressions.Runtime
{
    /// <summary>
    /// 表达系统运行时仓库。
    /// 它统一持有静态定义读取、契约解释、组合技匹配和快照构建链的长期复用依赖。
    /// </summary>
    internal sealed class ExpressionRuntimeRepository
    {
        /// <summary>
        /// 共享的芯片定义读取缓存。
        /// </summary>
        internal ChipDefinitionCache ChipDefinitionCache { get; }

        /// <summary>
        /// 共享的表达契约解释缓存。
        /// </summary>
        internal ExpressionContractCache ExpressionContractCache { get; }

        /// <summary>
        /// 共享的组合技运行时索引。
        /// </summary>
        internal ComboRuntimeIndex ComboRuntimeIndex { get; }

        /// <summary>
        /// 共享的芯片定义读取口。
        /// </summary>
        internal IChipDefinitionReader ChipDefinitionReader { get; }

        /// <summary>
        /// 共享的表达契约解释器。
        /// </summary>
        internal IChipExpressionContractInterpreter ContractInterpreter { get; }

        /// <summary>
        /// 共享的表达来源声明提供器。
        /// </summary>
        internal IExpressionSourceDeclarationProvider DeclarationProvider { get; }

        /// <summary>
        /// 共享的表达条件评估器。
        /// </summary>
        internal DefaultExpressionConditionEvaluator ConditionEvaluator { get; }

        /// <summary>
        /// 共享的表达快照构建器。
        /// </summary>
        internal ExpressionSnapshotBuilder SnapshotBuilder { get; }

        /// <summary>
        /// 初始化共享运行时仓库。
        /// </summary>
        public ExpressionRuntimeRepository()
        {
            ChipDefinitionCache = ChipSurfaceAccess.ResolveDefinitionCache();
            ExpressionContractCache = new ExpressionContractCache();
            ComboRuntimeIndex = ComboSurfaceAccess.ResolveRuntimeIndex();
            ChipDefinitionReader = ChipSurfaceAccess.ResolveDefinitionReader();
            ContractInterpreter = new ChipExpressionContractInterpreter(ExpressionContractCache);
            DeclarationProvider = new DefaultExpressionSourceDeclarationProvider(
                ChipDefinitionReader,
                ContractInterpreter);
            ConditionEvaluator = new DefaultExpressionConditionEvaluator();
            SnapshotBuilder = new ExpressionSnapshotBuilder(DeclarationProvider, ConditionEvaluator);
        }
    }
}
