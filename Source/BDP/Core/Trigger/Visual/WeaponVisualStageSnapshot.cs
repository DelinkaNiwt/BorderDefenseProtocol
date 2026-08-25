namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 单次绘制读取到的武器视觉阶段快照。
    /// 它不持久化，只携带原版时序结果与正式会话诊断信息。
    /// </summary>
    internal sealed class WeaponVisualStageSnapshot
    {
        /// <summary>
        /// 当前动作阶段。
        /// </summary>
        internal WeaponVisualActionStage Stage { get; }

        /// <summary>
        /// 当前阶段归一化进度，范围为 0 至 1。
        /// </summary>
        internal float Progress01 { get; }

        /// <summary>
        /// 当前阶段剩余的原版游戏刻数。
        /// </summary>
        internal int StageTicksRemaining { get; }

        /// <summary>
        /// 与目标视觉来源芯片匹配的正式结果标识。
        /// </summary>
        internal string MatchedSourceResultId { get; }

        /// <summary>
        /// 当前正式攻击宿主结果标识。
        /// </summary>
        internal string HostResultId { get; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        internal string AttackInstanceId { get; }

        /// <summary>
        /// 当前快照对应的战斗投影版本。
        /// </summary>
        internal int ProjectionVersion { get; }

        /// <summary>
        /// 创建只读阶段快照。
        /// </summary>
        internal WeaponVisualStageSnapshot(
            WeaponVisualActionStage stage,
            float progress01,
            int stageTicksRemaining,
            string matchedSourceResultId,
            string hostResultId,
            string attackInstanceId,
            int projectionVersion)
        {
            Stage = stage;
            Progress01 = progress01;
            StageTicksRemaining = stageTicksRemaining;
            MatchedSourceResultId = matchedSourceResultId;
            HostResultId = hostResultId;
            AttackInstanceId = attackInstanceId;
            ProjectionVersion = projectionVersion;
        }
    }
}
