namespace BDP.Core.Information
{
    /// <summary>适配层无法解释的中性扩展事实。</summary>
    public sealed class ChipInformationUnknownFact
    {
        /// <summary>稳定来源键。</summary>
        public string SourceKey { get; private set; }

        /// <summary>诊断代码。</summary>
        public string Code { get; private set; }

        /// <summary>诊断细节键或适配器参数。</summary>
        public string Detail { get; private set; }

        /// <summary>创建未知事实。</summary>
        public ChipInformationUnknownFact(
            string sourceKey,
            string code,
            string detail)
        {
            SourceKey = sourceKey;
            Code = code;
            Detail = detail;
        }
    }
}
