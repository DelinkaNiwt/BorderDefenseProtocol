using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 芯片半成品只在原版 UnfinishedThing 上增加组合身份与开工工作量快照。
    /// 材料持有、账单绑定、工作进度、取消返还和制作者记录均继续使用原版实现。
    /// </summary>
    public class Thing_UnfinishedChip : UnfinishedThing, IChipCombinationRecordHolder
    {
        /// <summary>开工时从账单复制的组合记录。</summary>
        private ChipCombinationRecord combinationRecord;

        /// <summary>开工时锁定的总工作量，只用于描述该半成品的原始规模。</summary>
        private float startingWorkAmount;

        /// <summary>读取半成品持有的组合记录。</summary>
        public ChipCombinationRecord CombinationRecord => combinationRecord;

        /// <summary>读取开工时锁定的总工作量。</summary>
        public float StartingWorkAmount => startingWorkAmount;

        /// <summary>新半成品首次开工时写入组合与总工作量；续作不得覆盖。</summary>
        public void InitializeFromBill(ChipCombinationRecord record, float workAmount)
        {
            if (combinationRecord != null || record == null)
            {
                return;
            }

            combinationRecord = record.Clone();
            startingWorkAmount = workAmount;
        }

        /// <summary>显示当前动态芯片名称；来源缺失时保留最后成功名称。</summary>
        public override string LabelNoCount
        {
            get
            {
                if (combinationRecord == null)
                {
                    return base.LabelNoCount;
                }

                ChipCombinationResolution resolution =
                    new ChipCombinationResolver().Resolve(combinationRecord);
                string chipLabel = !resolution.ResolvedLabel.NullOrEmpty()
                    ? resolution.ResolvedLabel
                    : combinationRecord.LastResolvedLabel;
                return chipLabel.NullOrEmpty()
                    ? base.LabelNoCount
                    : "BDP_UnfinishedChip_Incomplete".Translate().ToString() + "：" + chipLabel;
            }
        }

        /// <summary>在原版半成品存档基础上保存组合记录和开工工作量。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref combinationRecord, "chipCombinationRecord");
            Scribe_Values.Look(ref startingWorkAmount, "startingWorkAmount", 0f);
        }
    }
}
