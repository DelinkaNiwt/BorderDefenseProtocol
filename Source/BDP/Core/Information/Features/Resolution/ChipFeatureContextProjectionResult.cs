using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 功能上下文投影结果。
    /// </summary>
    public sealed class ChipFeatureContextProjectionResult
    {
        /// <summary>应放在芯片根节点的唯一归属功能。</summary>
        public IReadOnlyList<ChipInformationNode> RootFeatures { get; private set; }

        /// <summary>按形态/姿态组织的信息上下文。</summary>
        public IReadOnlyList<ChipInformationNode> Contexts { get; private set; }

        /// <summary>上下文投影诊断。</summary>
        public IReadOnlyList<ChipInformationDiagnostic> Diagnostics { get; private set; }

        /// <summary>创建上下文投影结果。</summary>
        public ChipFeatureContextProjectionResult(
            IEnumerable<ChipInformationNode> contexts,
            IEnumerable<ChipInformationDiagnostic> diagnostics)
            : this(null, contexts, diagnostics)
        {
        }

        /// <summary>创建包含根级功能与形态/姿态上下文的唯一归属结果。</summary>
        public ChipFeatureContextProjectionResult(
            IEnumerable<ChipInformationNode> rootFeatures,
            IEnumerable<ChipInformationNode> contexts,
            IEnumerable<ChipInformationDiagnostic> diagnostics)
        {
            RootFeatures = CopyNodes(rootFeatures);
            Contexts = CopyNodes(contexts);

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

        /// <summary>复制并过滤空信息节点。</summary>
        private static IReadOnlyList<ChipInformationNode> CopyNodes(
            IEnumerable<ChipInformationNode> source)
        {
            List<ChipInformationNode> nodeResult =
                new List<ChipInformationNode>();
            if (source != null)
            {
                foreach (ChipInformationNode context in source)
                {
                    if (context != null)
                    {
                        nodeResult.Add(context);
                    }
                }
            }

            return nodeResult.AsReadOnly();
        }
    }
}
