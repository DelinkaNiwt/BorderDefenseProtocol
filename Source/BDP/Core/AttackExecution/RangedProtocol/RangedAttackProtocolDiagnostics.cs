using BDP.Support.Diagnostics;

namespace BDP.Core.AttackExecution.RangedProtocol
{
    /// <summary>
    /// 远程攻击协议诊断汇总器。
    /// 默认只保留失败日志，避免一枪打印整段协议细节。
    /// </summary>
    internal static class RangedAttackProtocolDiagnostics
    {
        /// <summary>
        /// 记录远程攻击协议构建失败。
        /// 这是少数长期值得保留的协议层日志。
        /// </summary>
        public static void LogFailure(string reason, AttackExecutionPreparedContext request)
        {
            BdpDiagnostics.AttackExecution(
                "event=ranged_protocol_failed"
                + ", reason=" + Safe(reason)
                + ", attackId=" + Safe(request != null ? request.AttackInstanceId : null)
                + ", resultId=" + Safe(request?.Result != null ? request.Result.Id : null));
        }

        /// <summary>
        /// 统一清理空字符串。
        /// </summary>
        private static string Safe(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "<empty>" : text;
        }
    }
}
