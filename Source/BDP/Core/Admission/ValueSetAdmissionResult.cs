namespace BDP.Core.Admission
{
    /// <summary>中性集合准入失败种类。</summary>
    public enum ValueSetAdmissionFailureKind
    {
        /// <summary>没有失败。</summary>
        None = 0,

        /// <summary>命中了黑名单。</summary>
        Denied = 1,

        /// <summary>没有命中非空白名单。</summary>
        NotAllowed = 2,

        /// <summary>缺少必须项目。</summary>
        RequiredMissing = 3
    }

    /// <summary>中性字符串集合准入结果。</summary>
    public sealed class ValueSetAdmissionResult
    {
        /// <summary>当前候选集合是否通过。</summary>
        public bool IsAllowed { get; internal set; }

        /// <summary>未通过时的稳定失败种类。</summary>
        public ValueSetAdmissionFailureKind FailureKind { get; internal set; }

        /// <summary>触发失败的配置值；白名单整体未命中时为空。</summary>
        public string FailureValue { get; internal set; }
    }
}
