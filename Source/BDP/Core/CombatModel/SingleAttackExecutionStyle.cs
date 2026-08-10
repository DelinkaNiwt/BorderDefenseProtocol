namespace BDP.Core.CombatModel
{
    /// <summary>
    /// 单条攻击结果的正式执行风格。
    /// 它只描述单条结果的一次攻击如何展开，不持有运行时状态。
    /// </summary>
    public sealed class SingleAttackExecutionStyle
    {
        /// <summary>
        /// 当前单条结果的远程节奏。
        /// 非远程结果应保持为 None。
        /// </summary>
        public RangedExecutionRhythm RangedRhythm;

        /// <summary>
        /// 当前单条结果的近战节奏。
        /// 非近战结果应保持为 None。
        /// </summary>
        public MeleeExecutionRhythm MeleeRhythm;

        /// <summary>
        /// 当前单条近战结果一次攻击计划打出多少击。
        /// </summary>
        public int meleeHitCount;

        /// <summary>
        /// 当前单条近战结果相邻两击之间的 tick 间隔。
        /// </summary>
        public int meleeHitIntervalTicks;

        /// <summary>
        /// 当前远程结果是否声明了发射点随机散布区间。
        /// </summary>
        public bool HasOriginSpreadRange;

        /// <summary>
        /// 发射点横向最小随机偏移。负值偏左，正值偏右。
        /// </summary>
        public float OriginSpreadLateralMin;

        /// <summary>
        /// 发射点横向最大随机偏移。负值偏左，正值偏右。
        /// </summary>
        public float OriginSpreadLateralMax;

        /// <summary>
        /// 发射点前后最小随机偏移。负值靠后，正值靠前。
        /// </summary>
        public float OriginSpreadForwardMin;

        /// <summary>
        /// 发射点前后最大随机偏移。负值靠后，正值靠前。
        /// </summary>
        public float OriginSpreadForwardMax;

        /// <summary>
        /// 构造当前执行风格的浅复制对象。
        /// </summary>
        public SingleAttackExecutionStyle Clone()
        {
            return new SingleAttackExecutionStyle
            {
                RangedRhythm = RangedRhythm,
                MeleeRhythm = MeleeRhythm,
                meleeHitCount = meleeHitCount,
                meleeHitIntervalTicks = meleeHitIntervalTicks,
                HasOriginSpreadRange = HasOriginSpreadRange,
                OriginSpreadLateralMin = OriginSpreadLateralMin,
                OriginSpreadLateralMax = OriginSpreadLateralMax,
                OriginSpreadForwardMin = OriginSpreadForwardMin,
                OriginSpreadForwardMax = OriginSpreadForwardMax
            };
        }
    }
}
