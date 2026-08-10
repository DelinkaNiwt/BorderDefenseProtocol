namespace BDP.Core.CombatModel
{
    /// <summary>
    /// 一条正式攻击结果携带的执行风格。
    /// 它只声明单条或双侧复合的执行结构，不持有运行时状态。
    /// </summary>
    public sealed class AttackExecutionStyle
    {
        /// <summary>
        /// 当前结果的单条执行风格。
        /// 单条结果使用它，双侧复合结果通常为空。
        /// </summary>
        public SingleAttackExecutionStyle Single;

        /// <summary>
        /// 当前结果的双侧执行风格。
        /// 双侧复合结果使用它，单条结果通常为空。
        /// </summary>
        public DualAttackExecutionStyle Dual;

        /// <summary>
        /// 构造当前执行风格的浅复制对象。
        /// </summary>
        public AttackExecutionStyle Clone()
        {
            return new AttackExecutionStyle
            {
                Single = Single != null ? Single.Clone() : null,
                Dual = Dual != null ? Dual.Clone() : null
            };
        }
    }
}
