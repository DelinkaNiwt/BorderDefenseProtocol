namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 单条模块维度声明。
    /// 它只描述“哪个维度”以及“按什么裁决方式”处理。
    /// </summary>
    internal sealed class ModuleDimensionClaim
    {
        /// <summary>
        /// 当前声明对应的维度键。
        /// </summary>
        public string DimensionKey { get; set; }

        /// <summary>
        /// 当前维度应采用的裁决方式。
        /// </summary>
        public ModuleDimensionKind Kind { get; set; }
    }
}
