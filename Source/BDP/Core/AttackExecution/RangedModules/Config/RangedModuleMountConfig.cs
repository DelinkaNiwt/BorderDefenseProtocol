namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 单条远程攻击模块挂载记录。
    /// 顺序语义由 XML 列表顺序决定，这里不声明额外排序字段。
    /// </summary>
    public sealed class RangedModuleMountConfig
    {
        /// <summary>
        /// 当前挂载引用的模块定义。
        /// </summary>
        public BdpRangedAttackModuleDef moduleDef;

        /// <summary>
        /// 当前挂载是否启用。
        /// </summary>
        public bool enabled = true;

        /// <summary>
        /// 当前挂载显式提供的配置块。
        /// 留空时由下游回退到模块 Def 默认配置。
        /// </summary>
        public RangedModuleConfigNode config;

        /// <summary>
        /// 当前挂载所属的单侧来源结果标识。
        /// 复合结果合并模块时由上游写入，供运行时按来源隔离影响范围。
        /// </summary>
        public string sourceResultId;

        /// <summary>
        /// 复制当前挂载记录。
        /// </summary>
        public RangedModuleMountConfig Clone()
        {
            return new RangedModuleMountConfig
            {
                moduleDef = moduleDef,
                enabled = enabled,
                config = config != null ? config.Clone() : null,
                sourceResultId = sourceResultId
            };
        }
    }
}
