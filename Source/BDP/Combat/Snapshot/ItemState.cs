using Verse;

namespace BDP.Combat.Snapshot
{
    /// <summary>
    /// 物品状态记录，用于快照恢复时还原状态标记。
    /// </summary>
    public class ItemState : IExposable
    {
        public bool wasNotForSale;        // 是否标记为"不出售"
        public bool wasUnpackedCaravan;   // 是否为商队解包物品

        public void ExposeData()
        {
            Scribe_Values.Look(ref wasNotForSale, "wasNotForSale");
            Scribe_Values.Look(ref wasUnpackedCaravan, "wasUnpackedCaravan");
        }
    }
}
