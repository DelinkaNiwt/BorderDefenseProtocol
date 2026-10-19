using BDP.Support.Diagnostics;

namespace BDP.Compatibility
{
    /// <summary>
    /// 兼容层统一诊断入口。
    /// 正式版本只输出一次性简短信息，具体适配器自行提供稳定 key。
    /// </summary>
    public static class CompatibilityDiagnostics
    {
        /// <summary>输出一次兼容层诊断。</summary>
        public static void Once(string adapterId, string message)
        {
            BdpDiagnostics.Once(
                "compatibility." + adapterId + "." + message,
                "外部兼容：" + adapterId + "：" + message);
        }
    }
}
