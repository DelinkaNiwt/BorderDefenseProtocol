using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>芯片信息树及与玩家信息分离的解析诊断。</summary>
    public sealed class ChipInformationModel
    {
        /// <summary>唯一的芯片信息结果根节点。</summary>
        public ChipInformationNode Root { get; private set; }

        /// <summary>解析问题，不属于玩家信息树。</summary>
        internal IReadOnlyList<ChipInformationDiagnostic> Diagnostics { get; private set; }

        /// <summary>创建单一节点树结果。</summary>
        internal ChipInformationModel(
            ChipInformationNode root,
            IEnumerable<ChipInformationDiagnostic> diagnostics)
        {
            Root = root;
            List<ChipInformationDiagnostic> result =
                new List<ChipInformationDiagnostic>();
            if (diagnostics != null)
            {
                foreach (ChipInformationDiagnostic item in diagnostics)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            Diagnostics = result.AsReadOnly();
        }
    }
}
