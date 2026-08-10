namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条被动来源向外暴露的轻量数据项。
    /// 它只承载稳定键值，不承载运行时真值。
    /// </summary>
    public sealed class PassiveExpressionExposedDatumConfig
    {
        /// <summary>
        /// 当前数据项稳定键。
        /// </summary>
        public string DataKey;

        /// <summary>
        /// 当前数据项字符串值。
        /// 第一版先保持为最小可读形态。
        /// </summary>
        public string DataValue;
    }
}
