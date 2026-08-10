namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技表达声明句柄。
    /// 它让读取层可以只确认“有无表达块”和“表达块结构键”。
    /// </summary>
    internal sealed class ComboExpressionContractHandle
    {
        /// <summary>
        /// 当前是否存在表达块。
        /// </summary>
        public bool HasExpressionBlock;

        /// <summary>
        /// 当前表达块对应的原始配置。
        /// </summary>
        public ComboExpressionConfig Config;

        /// <summary>
        /// 当前表达块的结构键。
        /// 它只服务结构级说明，不服务业务分流。
        /// </summary>
        public string StructureKey;
    }
}
