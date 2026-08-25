using System;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Core（核心程序集）的中性攻击动作完成广播器。
    /// 它只发布原版执行事实，不知道任何具体芯片或 Hediff 业务。
    /// </summary>
    public static class AttackActionSuccessDispatcher
    {
        /// <summary>
        /// 攻击动作已完成事件。
        /// </summary>
        public static event Action<AttackActionSuccess> AttackActionSucceeded;

        /// <summary>
        /// 发布一次攻击动作完成事实。
        /// </summary>
        public static void Publish(AttackActionSuccess attack)
        {
            if (attack == null)
            {
                return;
            }

            AttackActionSucceeded?.Invoke(attack);
        }
    }
}
