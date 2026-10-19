using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>芯片信息模型的 Core（核心层）统一读取入口。</summary>
    public static class ChipInformationSurfaceAccess
    {
        /// <summary>默认通用读取服务。</summary>
        private static readonly ChipInformationService defaultReader =
            ChipInformationService.CreateDefault();

        /// <summary>按统一请求读取包含显式功能语义的模型。</summary>
        public static ChipInformationModel Read(ChipInformationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return defaultReader.Read(request);
        }

        /// <summary>读取请求对应的解析诊断；诊断不属于芯片客观信息树。</summary>
        public static IReadOnlyList<ChipInformationDiagnostic> ReadDiagnostics(
            ChipInformationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return defaultReader.Read(request).Diagnostics;
        }

        /// <summary>读取默认通用解析模型。</summary>
        public static ChipInformationModel Read(ChipInformationParseInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return defaultReader.Read(input);
        }

        /// <summary>读取指定规则集产生的统一解析模型。</summary>
        public static ChipInformationModel Read(
            ChipInformationParseInput input,
            IChipInformationReader reader)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return (reader ?? defaultReader).Read(input);
        }
    }
}
