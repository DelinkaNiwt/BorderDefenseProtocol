namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 投射物在原版发射解析阶段得到的首发分支。
    /// 该结果描述发射时的命中语义，不等同于最终碰撞结果。
    /// </summary>
    public enum ProjectileLaunchDisposition
    {
        /// <summary>旧计划或未接入发射解析时的未知结果。</summary>
        Unknown,

        /// <summary>原版强制未命中分支。</summary>
        ForcedMiss,

        /// <summary>原版基于精度的偏射分支。</summary>
        WildMiss,

        /// <summary>原版基于终端掩体的拦截分支。</summary>
        CoverIntercept,

        /// <summary>原版允许以原定目标为命中对象的分支。</summary>
        IntendedTarget
    }
}
