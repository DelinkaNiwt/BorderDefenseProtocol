using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体宿主事务状态。
    /// 它只保存宿主切换链自己需要记住的事实，不新增第四个真值 owner。
    /// </summary>
    internal sealed class HostState : IExposable
    {
        /// <summary>
        /// 当前是否已经捕获过进入前快照。
        /// </summary>
        public bool HasSnapshot;

        /// <summary>
        /// 当前是否已经应用过战斗体宿主变换。
        /// </summary>
        public bool TransformationApplied;

        /// <summary>
        /// 当前宿主持有的进入前快照状态。
        /// </summary>
        public CombatBodySnapshotState SnapshotState;

        /// <summary>
        /// 当前宿主持有的前台衣物层状态。
        /// </summary>
        public CombatBodyFrontState FrontState;

        /// <summary>
        /// 确保当前宿主持有可用的快照状态，并绑定持有者。
        /// </summary>
        public void EnsureSnapshotState(IThingHolder holder)
        {
            if (SnapshotState == null)
            {
                SnapshotState = new CombatBodySnapshotState();
            }

            SnapshotState.Bind(holder);
        }

        /// <summary>
        /// 确保当前宿主持有可用的前台状态，并绑定持有者。
        /// </summary>
        public void EnsureFrontState(IThingHolder holder)
        {
            if (FrontState == null)
            {
                FrontState = new CombatBodyFrontState();
            }

            FrontState.Bind(holder);
        }

        /// <summary>
        /// 存读档宿主事务状态。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref HasSnapshot, "hasSnapshot", false);
            Scribe_Values.Look(ref TransformationApplied, "transformationApplied", false);
            Scribe_Deep.Look(ref SnapshotState, "snapshotState");
            Scribe_Deep.Look(ref FrontState, "frontState");
        }
    }
}
