namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 描述一条攻击请求要以什么派单方式进入正式执行系统。
    /// 它只回答“怎么派单”，不回答“请求从哪条入口来”。
    /// </summary>
    public enum AttackDispatchIntent
    {
        /// <summary>
        /// 立即按当前解析出的 Verb 做一次直接施放。
        /// 这条语义保留给现有非手动的即时执行路径。
        /// </summary>
        ImmediateCast = 0,

        /// <summary>
        /// 把目标确认结果翻译成正式强制攻击命令。
        /// 这条语义用于需要进入 BDP 正式持续攻击 / Job 驱动链的入口。
        /// </summary>
        ForceTargetOrder = 1,

        /// <summary>
        /// 把原版自动战斗决定的攻击起手翻译成正式自动攻击命令。
        /// 这条语义用于自动远程和自动近战统一接回 BDP 正式执行边界。
        /// </summary>
        AutoAttackOrder = 2
    }
}
