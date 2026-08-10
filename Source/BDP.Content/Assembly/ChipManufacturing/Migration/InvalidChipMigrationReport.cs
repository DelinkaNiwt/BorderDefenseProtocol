namespace BDP.Content.Assembly.ChipManufacturing.Migration
{
    /// <summary>一次非法芯片迁移的汇总结果。</summary>
    public sealed class InvalidChipMigrationReport
    {
        /// <summary>已替换为统一遗留物的实体数量。</summary>
        public int ReplacedItemCount { get; private set; }

        /// <summary>已删除的非法制造账单数量。</summary>
        public int DeletedBillCount { get; private set; }

        /// <summary>因来源缺失而保留的实体或账单数量。</summary>
        public int PreservedMissingSourceCount { get; private set; }

        /// <summary>是否存在值得告知玩家的扫描结果。</summary>
        public bool HasAnythingToReport =>
            ReplacedItemCount > 0
            || DeletedBillCount > 0
            || PreservedMissingSourceCount > 0;

        /// <summary>记录一次实体替换。</summary>
        public void RecordReplacedItem()
        {
            ReplacedItemCount++;
        }

        /// <summary>记录一次非法账单删除。</summary>
        public void RecordDeletedBill()
        {
            DeletedBillCount++;
        }

        /// <summary>记录一次来源缺失保留。</summary>
        public void RecordPreservedMissingSource()
        {
            PreservedMissingSourceCount++;
        }
    }
}
