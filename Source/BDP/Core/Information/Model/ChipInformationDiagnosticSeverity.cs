namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息解析诊断严重级别。
    /// </summary>
    public enum ChipInformationDiagnosticSeverity
    {
        /// <summary>辅助性说明。</summary>
        Info,

        /// <summary>存在降级但模型仍可继续使用。</summary>
        Warning,

        /// <summary>当前块或字段无法安全解析。</summary>
        Error
    }
}
