using BDP.Content.Assembly.ChipManufacturing.Bill;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using RimWorld;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>右栏对原版真实账单栈执行的集中操作。</summary>
    public static class ChipBillQueueOperations
    {
        /// <summary>检查配置、正数量和队列容量；材料存量有意不参与。</summary>
        public static bool CanEnqueue(
            ChipCombinationResolution resolution,
            int quantity,
            int queueCount)
        {
            return resolution != null
                && resolution.Status == ChipCombinationResolutionStatus.Valid
                && quantity > 0
                && queueCount < BillStack.MaxCount;
        }

        /// <summary>每次提交都建立一个独立有限账单，产物默认原地落地。</summary>
        public static Bill_ChipProduction Enqueue(
            Building_ChipFabricator building,
            ChipManufacturingDraft draft,
            ChipCombinationResolution resolution)
        {
            if (building == null
                || draft == null
                || !CanEnqueue(resolution, draft.Quantity, building.BillStack.Count))
            {
                return null;
            }

            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(
                "BDP_Recipe_ProduceChip");
            if (recipe == null)
            {
                return null;
            }

            Bill_ChipProduction bill = new Bill_ChipProduction(recipe, draft.Record)
            {
                repeatMode = BillRepeatModeDefOf.RepeatCount,
                repeatCount = draft.Quantity
            };
            bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor, null);
            building.BillStack.AddBill(bill);
            return bill;
        }

        /// <summary>在合法边界内调用原版账单栈重排。</summary>
        public static void Move(BillStack stack, Bill_ChipProduction bill, int offset)
        {
            if (stack == null || bill == null || offset == 0)
            {
                return;
            }

            int current = stack.IndexOf(bill);
            int target = current + offset;
            if (current >= 0 && target >= 0 && target < stack.Count)
            {
                stack.Reorder(bill, offset);
            }
        }

        /// <summary>调用原版账单栈删除指定任务。</summary>
        public static void Delete(BillStack stack, Bill_ChipProduction bill)
        {
            if (stack != null && bill != null && stack.IndexOf(bill) >= 0)
            {
                stack.Delete(bill);
            }
        }

        /// <summary>切换原版账单暂停字段。</summary>
        public static void ToggleSuspended(Bill_ChipProduction bill)
        {
            if (bill != null)
            {
                bill.suspended = !bill.suspended;
            }
        }

        /// <summary>只修改有限账单的剩余迭代次数，零表示已完成但仍保留。</summary>
        public static void SetRemainingCount(Bill_ChipProduction bill, int count)
        {
            if (bill != null && bill.repeatMode == BillRepeatModeDefOf.RepeatCount)
            {
                bill.repeatCount = count < 0 ? 0 : count;
            }
        }

        /// <summary>把账单记录副本载入对应编辑路径，不修改原账单。</summary>
        public static ChipManufacturingDraft LoadConfiguration(
            ChipManufacturingEditorState editorState,
            Bill_ChipProduction bill)
        {
            ChipCombinationRecord record = bill?.CombinationRecord?.Clone();
            if (editorState == null || record == null)
            {
                return null;
            }

            ChipManufacturingDraft draft = editorState.Switch(
                ChipManufacturingDefLookup.FindCategory(record.CategoryDefName),
                ChipManufacturingDefLookup.FindProfession(record.ProfessionDefName));
            draft.LoadFrom(record, bill.repeatCount);
            return draft;
        }
    }
}
