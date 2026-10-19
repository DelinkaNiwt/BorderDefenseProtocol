namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息解析过程中的独立诊断记录。
    /// </summary>
    public sealed class ChipInformationDiagnostic
    {
        /// <summary>诊断代码。</summary>
        public string Code { get; private set; }

        /// <summary>诊断关联的块键。</summary>
        public string BlockKey { get; private set; }

        /// <summary>诊断关联的字段键。</summary>
        public string FieldKey { get; private set; }

        /// <summary>诊断严重级别。</summary>
        public ChipInformationDiagnosticSeverity Severity { get; private set; }

        /// <summary>诊断参数或非玩家文案说明。</summary>
        public string Detail { get; private set; }

        /// <summary>创建诊断记录。</summary>
        public ChipInformationDiagnostic(
            string code,
            string blockKey,
            string fieldKey,
            ChipInformationDiagnosticSeverity severity,
            string detail)
        {
            Code = code;
            BlockKey = blockKey;
            FieldKey = fieldKey;
            Severity = severity;
            Detail = detail;
        }
    }
}
