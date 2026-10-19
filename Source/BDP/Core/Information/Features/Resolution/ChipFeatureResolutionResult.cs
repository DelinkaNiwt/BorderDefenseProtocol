using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 一次功能解析的只读结果和诊断集合。
    /// </summary>
    public sealed class ChipFeatureResolutionResult
    {
        /// <summary>解析成功保留的功能结果。</summary>
        public IReadOnlyList<ChipInformationNode> Features { get; private set; }

        /// <summary>解析诊断。</summary>
        public IReadOnlyList<ChipInformationDiagnostic> Diagnostics { get; private set; }

        /// <summary>创建功能解析结果。</summary>
        public ChipFeatureResolutionResult(
            IEnumerable<ChipInformationNode> features,
            IEnumerable<ChipInformationDiagnostic> diagnostics)
        {
            List<ChipInformationNode> featureResult = new List<ChipInformationNode>();
            if (features != null)
            {
                foreach (ChipInformationNode feature in features)
                {
                    if (feature != null)
                    {
                        featureResult.Add(feature);
                    }
                }
            }

            Features = featureResult.AsReadOnly();
            List<ChipInformationDiagnostic> diagnosticResult =
                new List<ChipInformationDiagnostic>();
            if (diagnostics != null)
            {
                foreach (ChipInformationDiagnostic diagnostic in diagnostics)
                {
                    if (diagnostic != null)
                    {
                        diagnosticResult.Add(diagnostic);
                    }
                }
            }

            Diagnostics = diagnosticResult.AsReadOnly();
        }
    }
}
