namespace BDP.Core.Expressions
{
    /// <summary>
    /// 公开表达读取面里的一条轻量键值数据。
    /// 它只承接已经正式发布的暴露字段，不回带内部运行时对象。
    /// </summary>
    public sealed class ExpressionPublishedDatum
    {
        /// <summary>
        /// 当前数据项稳定键。
        /// </summary>
        public string Key { get; internal set; }

        /// <summary>
        /// 当前数据项字符串值。
        /// </summary>
        public string Value { get; internal set; }
    }
}
