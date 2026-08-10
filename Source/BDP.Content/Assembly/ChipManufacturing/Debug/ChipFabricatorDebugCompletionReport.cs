namespace BDP.Content.Assembly.ChipManufacturing.Debug
{
    /// <summary>一次上帝模式批量完成操作的汇总结果。</summary>
    public sealed class ChipFabricatorDebugCompletionReport
    {
        /// <summary>扫描到的芯片生产账单数量。</summary>
        public int EncounteredBillCount { get; private set; }

        /// <summary>成功生成的成品芯片数量。</summary>
        public int ProducedChipCount { get; private set; }

        /// <summary>已从队列移除的完成账单数量。</summary>
        public int CompletedBillCount { get; private set; }

        /// <summary>因来源缺失或生成失败而保留的账单数量。</summary>
        public int SkippedBillCount { get; private set; }

        /// <summary>记录扫描到一条芯片账单。</summary>
        public void RecordEncounteredBill()
        {
            EncounteredBillCount++;
        }

        /// <summary>记录成功生成的一枚成品。</summary>
        public void RecordProducedChip()
        {
            ProducedChipCount++;
        }

        /// <summary>记录完成并移除一条账单。</summary>
        public void RecordCompletedBill()
        {
            CompletedBillCount++;
        }

        /// <summary>记录保留了一条无法完成的账单。</summary>
        public void RecordSkippedBill()
        {
            SkippedBillCount++;
        }
    }
}
