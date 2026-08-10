using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Recipe;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Assembly;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Assembly.ChipManufacturing.Bill
{
    /// <summary>
    /// 芯片生产账单在原版半成品生产账单上增加不可编辑的组合记录。
    /// </summary>
    public class Bill_ChipProduction : Bill_ProductionWithUft, IChipCombinationRecordHolder
    {
        /// <summary>当前账单只读持有的芯片组合记录。</summary>
        private ChipCombinationRecord combinationRecord;

        /// <summary>供原版读档反射建立空实例。</summary>
        public Bill_ChipProduction()
        {
        }

        /// <summary>用通用配方和玩家选择建立芯片生产账单。</summary>
        public Bill_ChipProduction(RecipeDef recipe, ChipCombinationRecord record)
            : base(recipe)
        {
            combinationRecord = record?.Clone();
        }

        /// <summary>读取当前账单持有的组合记录副本，防止外部改写任务身份。</summary>
        public ChipCombinationRecord CombinationRecord => combinationRecord?.Clone();

        /// <summary>只有当前组合有效且原版有限账单仍需执行时才允许工作。</summary>
        public override bool ShouldDoNow()
        {
            ChipCombinationResolution resolution =
                new ChipCombinationResolver().Resolve(combinationRecord);
            return resolution.Status == ChipCombinationResolutionStatus.Valid
                && base.ShouldDoNow();
        }

        /// <summary>新开工读取当前动态工作量；已初始化半成品继续使用自身 workLeft。</summary>
        public override float GetWorkAmount(Verse.Thing thing = null)
        {
            Thing_UnfinishedChip unfinished = thing as Thing_UnfinishedChip;
            if (unfinished != null && unfinished.Initialized)
            {
                return unfinished.StartingWorkAmount > 0f
                    ? unfinished.StartingWorkAmount
                    : unfinished.workLeft;
            }

            ChipManufacturingCost cost =
                ChipManufacturingCostCalculator.Calculate(recipe, combinationRecord);
            return cost != null ? cost.WorkAmount : base.GetWorkAmount(thing);
        }

        /// <summary>首次开工后把组合与本轮起始总工作量写入原版已生成的半成品。</summary>
        public override void Notify_BillWorkStarted(Pawn billDoer)
        {
            base.Notify_BillWorkStarted(billDoer);
            Job job = billDoer?.CurJob;
            Thing_UnfinishedChip unfinished = job != null
                ? job.GetTarget(TargetIndex.B).Thing as Thing_UnfinishedChip
                : null;
            if (unfinished != null && unfinished.CombinationRecord == null)
            {
                float startingAmount = unfinished.Initialized
                    ? unfinished.workLeft
                    : GetWorkAmount(unfinished);
                unfinished.InitializeFromBill(combinationRecord, startingAmount);
            }
        }

        /// <summary>在原版账单存档基础上保存组合记录。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref combinationRecord, "chipCombinationRecord");
        }

        /// <summary>复制原版账单参数，并深复制组合记录。</summary>
        public override RimWorld.Bill Clone()
        {
            Bill_ChipProduction clone = (Bill_ChipProduction)base.Clone();
            clone.combinationRecord = combinationRecord?.Clone();
            return clone;
        }
    }
}
