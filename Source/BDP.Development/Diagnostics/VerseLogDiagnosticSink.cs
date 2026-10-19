using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Development.Diagnostics
{
    /// <summary>
    /// 把中性诊断消息写入 RimWorld 开发日志的 Development 实现。
    /// </summary>
    public sealed class VerseLogDiagnosticSink : IBdpDiagnosticSink
    {
        /// <summary>Development 诊断日志统一前缀。</summary>
        private const string Prefix = "[BDP Development] ";

        /// <summary>
        /// 将一条诊断消息交给 Verse 日志系统。
        /// </summary>
        public void Write(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Log.Message(Prefix + message);
        }
    }
}
