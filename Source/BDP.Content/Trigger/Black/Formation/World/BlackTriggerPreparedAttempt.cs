namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>人物真正死亡前已经完成快照和匹配的短生命周期准备结果。</summary>
    public sealed class BlackTriggerPreparedAttempt
    {
        /// <summary>冻结的来源人物资料。</summary>
        public BlackTriggerMakerSnapshot Snapshot;

        /// <summary>冻结的完整候选与侧重分配。</summary>
        public BlackTriggerFormationEvaluation Evaluation;

        /// <summary>是否来自 Development 调试入口。</summary>
        public bool Debug;
    }
}
