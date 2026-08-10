using BDP.Content.Assembly.ChipManufacturing.Bill;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Assembly.ChipManufacturing.Thing
{
    /// <summary>唯一成品芯片物品，负责动态名称和原版产物回调接线。</summary>
    public sealed class Thing_ManufacturedChip : ThingWithComps
    {
        /// <summary>优先显示当前组合动态名称。</summary>
        public override string LabelNoCount
        {
            get
            {
                string label = GetComp()?.CurrentLabel;
                return !label.NullOrEmpty() ? label : base.LabelNoCount;
            }
        }

        /// <summary>同步动态首字母大写名称。</summary>
        public override string LabelCap
        {
            get
            {
                string label = GetComp()?.CurrentLabel;
                return !label.NullOrEmpty() ? label.CapitalizeFirst() : base.LabelCap;
            }
        }

        /// <summary>原版生成产物时，从当前芯片账单复制组合记录到成品组件。</summary>
        public override void Notify_RecipeProduced(Pawn pawn)
        {
            base.Notify_RecipeProduced(pawn);
            Job job = pawn?.CurJob;
            Bill_ChipProduction bill = job?.bill as Bill_ChipProduction;
            if (bill != null)
            {
                GetComp()?.InitializeFromBill(bill.CombinationRecord);
            }
        }

        /// <summary>读取本物品的制造芯片组件。</summary>
        private CompManufacturedChip GetComp()
        {
            return this.TryGetComp<CompManufacturedChip>();
        }
    }
}
