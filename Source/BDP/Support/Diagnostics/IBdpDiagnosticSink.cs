namespace BDP.Support.Diagnostics
{
    /// <summary>
    /// 接收开发期诊断消息的中性出口。
    /// Core 不约束消息最终写入日志、面板还是临时文件。
    /// </summary>
    public interface IBdpDiagnosticSink
    {
        /// <summary>
        /// 接收一条已经完成格式化的诊断消息。
        /// </summary>
        void Write(string message);
    }
}
