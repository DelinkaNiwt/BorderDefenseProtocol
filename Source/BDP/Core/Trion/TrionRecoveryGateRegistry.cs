using Verse;

namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 自然恢复门控的最小注册表。
    /// 没有业务程序集接入时保持基准速率，确保 Core 独立运行。
    /// </summary>
    public static class TrionRecoveryGateRegistry
    {
        /// <summary>
        /// 当前唯一恢复门控提供器。
        /// </summary>
        private static ITrionRecoveryGate current;

        /// <summary>
        /// 注册或替换当前恢复门控提供器。
        /// </summary>
        /// <param name="gate">新的门控提供器；传入 null 表示清空。</param>
        public static void Register(ITrionRecoveryGate gate)
        {
            current = gate;
        }

        /// <summary>
        /// 只清除指定实例，避免旧程序集清理时误删新提供器。
        /// </summary>
        /// <param name="gate">准备清除的许可提供器。</param>
        public static void Clear(ITrionRecoveryGate gate)
        {
            if (ReferenceEquals(current, gate))
            {
                current = null;
            }
        }

        /// <summary>
        /// 查询一次自然恢复的倍率。
        /// 没有注册提供器时返回 1，保持 Core 原有行为。
        /// </summary>
        /// <param name="pawn">当前进行自然恢复的 Pawn。</param>
        /// <returns>本次恢复相对于基准速率的倍率。</returns>
        public static float GetRecoveryMultiplier(Pawn pawn)
        {
            if (current == null)
            {
                return 1f;
            }

            return current.GetRecoveryMultiplier(pawn);
        }
    }
}
