using Verse;

namespace BDP.Projectiles.Pipeline
{
    /// <summary>
    /// 生命周期检查上下文——模块通过写入字段表达"意图"，宿主统一执行。
    /// 注入三层目标供模块读取，模块通过NewLockedTarget请求修改目标。
    /// </summary>
    public struct LifecycleContext
    {
        /// <summary>上一tick是否有模块产出了飞行意图（由宿主注入）。</summary>
        public readonly bool PreviousTickHadIntent;

        /// <summary>瞄准目标——发射时锁定，不变（只读）。</summary>
        public readonly LocalTargetInfo AimTarget;

        /// <summary>锁定目标——通常=AimTarget，仅"重定向"机制可改（只读）。</summary>
        public readonly LocalTargetInfo LockedTarget;

        /// <summary>当前目标——此刻飞向谁（只读）。</summary>
        public readonly LocalTargetInfo CurrentTarget;

        /// <summary>请求销毁子弹。</summary>
        public bool RequestDestroy;

        /// <summary>销毁原因（诊断日志用）。</summary>
        public string DestroyReason;

        /// <summary>请求修改LockedTarget（追踪模块重搜索后切换目标）。</summary>
        public LocalTargetInfo? NewLockedTarget;

        public LifecycleContext(
            bool previousTickHadIntent,
            LocalTargetInfo aimTarget,
            LocalTargetInfo lockedTarget,
            LocalTargetInfo currentTarget)
        {
            PreviousTickHadIntent = previousTickHadIntent;
            AimTarget = aimTarget;
            LockedTarget = lockedTarget;
            CurrentTarget = currentTarget;
            RequestDestroy = false;
            DestroyReason = null;
            NewLockedTarget = null;
        }
    }

    /// <summary>
    /// 生命周期策略管线接口——每tick检查模块是否需要销毁子弹或修改目标。
    /// 执行顺序：管线第1阶段（LifecycleCheck）。
    /// </summary>
    public interface IBDPLifecyclePolicy
    {
        /// <summary>检查生命周期状态，通过ctx表达意图。</summary>
        void CheckLifecycle(Bullet_BDP host, ref LifecycleContext ctx);
    }
}
