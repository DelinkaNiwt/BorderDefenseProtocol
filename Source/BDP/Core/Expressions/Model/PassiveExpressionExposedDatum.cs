namespace BDP.Core.Expressions
{
    /// <summary>
    /// 被动来源在正式链路中携带的一条轻量暴露数据。
    /// 它属于正式结果内容，不是外部系统的运行时真值。
    /// </summary>
    internal sealed class PassiveExpressionExposedDatum
    {
        /// <summary>
        /// 当前数据项稳定键。
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 当前数据项字符串值。
        /// </summary>
        public string Value { get; set; }
    }
}
