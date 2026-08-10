using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体进入前快照状态。
    /// 当前承接原衣物、原背包、Need、Hediff 及其恢复标记。
    /// </summary>
    internal sealed class CombatBodySnapshotState : IExposable, IThingHolder
    {
        private ThingOwner<Apparel> originalApparelContainer;
        private ThingOwner<Thing> originalInventoryContainer;

        /// <summary>
        /// 残缺旧档中无法归属到当前会话的实物隔离容器。
        /// 它不参与正常快照重置或恢复，只等待安全归还 Pawn 背包。
        /// </summary>
        private ThingOwner<Thing> recoveredItemContainer;
        private Dictionary<int, bool> apparelLockedStates = new Dictionary<int, bool>();
        private Dictionary<int, bool> apparelForcedStates = new Dictionary<int, bool>();
        private Dictionary<int, bool> itemNotForSaleStates = new Dictionary<int, bool>();
        private Dictionary<int, bool> itemUnpackedCaravanStates = new Dictionary<int, bool>();
        private List<CombatBodySnapshotNeedRecord> needSnapshots = new List<CombatBodySnapshotNeedRecord>();
        private List<CombatBodySnapshotHediffRecord> hediffSnapshots = new List<CombatBodySnapshotHediffRecord>();

        public bool IsCaptured;
        private IThingHolder holder;

        public void Bind(IThingHolder holder)
        {
            this.holder = holder;
            EnsureRecordedStates();
            if (originalApparelContainer == null)
            {
                originalApparelContainer = new ThingOwner<Apparel>(this);
            }

            if (originalInventoryContainer == null)
            {
                originalInventoryContainer = new ThingOwner<Thing>(this);
            }

            if (recoveredItemContainer == null)
            {
                recoveredItemContainer = new ThingOwner<Thing>(this);
            }
        }

        public ThingOwner<Apparel> OriginalApparelContainer => originalApparelContainer;
        public ThingOwner<Thing> OriginalInventoryContainer => originalInventoryContainer;

        /// <summary>
        /// 旧档残留实物隔离容器。
        /// </summary>
        public ThingOwner<Thing> RecoveredItemContainer => recoveredItemContainer;
        public Dictionary<int, bool> ApparelLockedStates => apparelLockedStates;
        public Dictionary<int, bool> ApparelForcedStates => apparelForcedStates;
        public Dictionary<int, bool> ItemNotForSaleStates => itemNotForSaleStates;
        public Dictionary<int, bool> ItemUnpackedCaravanStates => itemUnpackedCaravanStates;
        public List<CombatBodySnapshotNeedRecord> NeedSnapshots => needSnapshots;
        public List<CombatBodySnapshotHediffRecord> HediffSnapshots => hediffSnapshots;
        public IThingHolder ParentHolder => holder;

        /// <summary>
        /// 修复旧档中尚未存在的快照记录集合。
        /// 这些集合只保存恢复元数据，不据此推断或改写 Pawn 当前状态。
        /// </summary>
        private void EnsureRecordedStates()
        {
            if (apparelLockedStates == null)
            {
                apparelLockedStates = new Dictionary<int, bool>();
            }

            if (apparelForcedStates == null)
            {
                apparelForcedStates = new Dictionary<int, bool>();
            }

            if (itemNotForSaleStates == null)
            {
                itemNotForSaleStates = new Dictionary<int, bool>();
            }

            if (itemUnpackedCaravanStates == null)
            {
                itemUnpackedCaravanStates = new Dictionary<int, bool>();
            }

            if (needSnapshots == null)
            {
                needSnapshots = new List<CombatBodySnapshotNeedRecord>();
            }

            if (hediffSnapshots == null)
            {
                hediffSnapshots = new List<CombatBodySnapshotHediffRecord>();
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            Bind(holder);
            return originalApparelContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            Bind(holder);
            if (originalApparelContainer != null)
            {
                ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, originalApparelContainer);
            }

            if (originalInventoryContainer != null)
            {
                ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, originalInventoryContainer);
            }

            if (recoveredItemContainer != null)
            {
                ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, recoveredItemContainer);
            }
        }

        public void ClearRecordedStates()
        {
            apparelLockedStates.Clear();
            apparelForcedStates.Clear();
            itemNotForSaleStates.Clear();
            itemUnpackedCaravanStates.Clear();
            needSnapshots.Clear();
            hediffSnapshots.Clear();
        }

        /// <summary>
        /// 清空当前这一轮会话使用的原物暂存容器。
        /// 这些容器只服务单轮激活/关闭，不允许跨轮带残留。
        /// </summary>
        public void ClearSessionContainers()
        {
            originalApparelContainer = new ThingOwner<Apparel>(this);
            originalInventoryContainer = new ThingOwner<Thing>(this);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref IsCaptured, "isCaptured", false);
            Scribe_Deep.Look(ref originalApparelContainer, "originalApparelContainer", this);
            Scribe_Deep.Look(ref originalInventoryContainer, "originalInventoryContainer", this);
            Scribe_Deep.Look(ref recoveredItemContainer, "recoveredItemContainer", this);
            Scribe_Collections.Look(ref apparelLockedStates, "apparelLockedStates", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref apparelForcedStates, "apparelForcedStates", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref itemNotForSaleStates, "itemNotForSaleStates", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref itemUnpackedCaravanStates, "itemUnpackedCaravanStates", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref needSnapshots, "needSnapshots", LookMode.Deep);
            Scribe_Collections.Look(ref hediffSnapshots, "hediffSnapshots", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Bind(holder);
            }
        }
    }
}
