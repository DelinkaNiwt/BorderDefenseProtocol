using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 单个模块运行时实例的初始化上下文。
    /// 它只提供中性会话事实，不提供具体业务语义。
    /// </summary>
    public sealed class RangedAttackModuleRuntimeContext
    {
        /// <summary>
        /// 当前实例对应的挂载顺序索引。
        /// 它同时也是模块私有上下文槽位的稳定身份。
        /// </summary>
        public int MountIndex { get; set; }

        /// <summary>
        /// 当前攻击的宿主 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前攻击绑定的正式表达结果。
        /// </summary>
        internal FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前攻击绑定的正式结果标识。
        /// 作者模块应优先按这个稳定标识做轻量判断，而不是依赖内部正式模型。
        /// </summary>
        public string ResultId => Result != null ? Result.Id : null;

        /// <summary>
        /// 当前挂载所属的单侧来源结果标识。
        /// 复合结果缺省时回退到当前 ResultId。
        /// </summary>
        public string SourceResultId => !string.IsNullOrWhiteSpace(Mount?.sourceResultId)
            ? Mount.sourceResultId
            : ResultId;

        /// <summary>
        /// 当前运行时实例对应的挂载记录。
        /// </summary>
        public RangedModuleMountConfig Mount { get; set; }

        /// <summary>
        /// 当前运行时实例对应的模块定义。
        /// </summary>
        public BdpRangedAttackModuleDef ModuleDef { get; set; }

        /// <summary>
        /// 当前实例实际生效的配置块。
        /// 它在模块实例初始化前就已经冻结完成。
        /// </summary>
        public RangedModuleConfigNode Config { get; internal set; }
    }
}
