namespace BDP.Trigger
{
    /// <summary>
    /// 生命周期检查上下文——模块通过写入字段表达"意图"，宿主统一执行。
    /// </summary>
    public struct LifecycleContext
    {
        /// <summary>当前飞行阶段（只读）。</summary>
        public readonly FlightPhase CurrentPhase;

        /// <summary>上一tick是否有模块产出了飞行意图（由宿主注入）。</summary>
        public readonly bool PreviousTickHadIntent;

        /// <summary>请求销毁子弹。</summary>
        public bool RequestDestroy;

        /// <summary>销毁原因（诊断日志用）。</summary>
        public string DestroyReason;

        /// <summary>请求Phase转换（null=不请求）。</summary>
        public FlightPhase? RequestPhaseChange;

        public LifecycleContext(FlightPhase phase, bool previousTickHadIntent)
        {
            CurrentPhase = phase;
            PreviousTickHadIntent = previousTickHadIntent;
            RequestDestroy = false;
            DestroyReason = null;
            RequestPhaseChange = null;
        }
    }

    /// <summary>
    /// 生命周期策略管线接口——每tick检查模块是否需要销毁子弹或转换Phase。
    /// 执行顺序：管线第1阶段（LifecycleCheck）。
    /// </summary>
    public interface IBDPLifecyclePolicy
    {
        /// <summary>检查生命周期状态，通过ctx表达意图。</summary>
        void CheckLifecycle(Bullet_BDP host, ref LifecycleContext ctx);
    }
}
