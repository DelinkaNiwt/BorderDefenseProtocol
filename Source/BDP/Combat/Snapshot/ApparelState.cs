using Verse;

namespace BDP.Combat.Snapshot
{
    /// <summary>
    /// 衣物状态记录，用于快照恢复时还原状态标记。
    /// </summary>
    public class ApparelState : IExposable
    {
        public bool wasLocked;   // 是否锁定（不可脱下）
        public bool wasForced;   // 是否强制穿戴（装备策略）

        public void ExposeData()
        {
            Scribe_Values.Look(ref wasLocked, "wasLocked");
            Scribe_Values.Look(ref wasForced, "wasForced");
        }
    }
}
