using System;
using System.Collections.Generic;

namespace BDP.Support.Diagnostics
{
    /// <summary>
    /// 保存当前可选诊断接收器的中性注册口。
    /// 缺少接收器或接收器自身失败时均保持静默，避免影响正式业务。
    /// </summary>
    public static class BdpDiagnosticSinkRegistry
    {
        /// <summary>启动早期接收器尚未注册时最多暂存的诊断消息数。</summary>
        private const int MaxPendingMessages = 128;

        /// <summary>
        /// 当前接收器；后注册的实例替换先前实例。
        /// </summary>
        private static IBdpDiagnosticSink currentSink;

        /// <summary>
        /// Development 还未加载时暂存启动诊断；接收器注册后一次性补发。
        /// </summary>
        private static readonly List<string> pendingMessages = new List<string>();

        /// <summary>
        /// 注册当前诊断接收器；传入空值等同清空。
        /// </summary>
        public static void Register(IBdpDiagnosticSink sink)
        {
            currentSink = sink;
            if (sink == null || pendingMessages.Count == 0)
            {
                return;
            }

            List<string> pending = new List<string>(pendingMessages);
            pendingMessages.Clear();
            foreach (string message in pending)
            {
                WriteToSink(sink, message);
            }
        }

        /// <summary>
        /// 仅当传入实例正是当前接收器时才注销，避免旧实例误清除新注册。
        /// </summary>
        public static void Unregister(IBdpDiagnosticSink sink)
        {
            if (ReferenceEquals(currentSink, sink))
            {
                currentSink = null;
            }
        }

        /// <summary>
        /// 把消息交给当前接收器，并隔离接收器内部异常。
        /// </summary>
        internal static void Write(string message)
        {
            IBdpDiagnosticSink sink = currentSink;
            if (sink == null)
            {
                if (pendingMessages.Count < MaxPendingMessages)
                {
                    pendingMessages.Add(message);
                }
                return;
            }

            WriteToSink(sink, message);
        }

        /// <summary>隔离接收器异常；诊断设施不能反向破坏正式业务调用链。</summary>
        private static void WriteToSink(IBdpDiagnosticSink sink, string message)
        {
            try
            {
                sink.Write(message);
            }
            catch
            {
                // 诊断设施不得反向破坏正式业务调用链。
            }
        }
    }
}
