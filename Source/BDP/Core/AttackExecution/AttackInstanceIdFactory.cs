using System.Threading;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击实例标识生成器。
    /// 它只负责产出会话内可追踪的轻量标识，不承担业务语义。
    /// </summary>
    internal static class AttackInstanceIdFactory
    {
        /// <summary>
        /// 进程内单调递增的攻击序号。
        /// </summary>
        private static int nextSequence;

        /// <summary>
        /// 生成一条新的攻击实例标识。
        /// </summary>
        public static string Create()
        {
            int sequence = Interlocked.Increment(ref nextSequence);
            return "atk_" + sequence;
        }
    }
}
