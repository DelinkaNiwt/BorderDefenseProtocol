using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Bill;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Assembly.ChipManufacturing.Thing;
using RimWorld;
using Verse;
using Verse.AI;
using VerseThing = Verse.Thing;

namespace BDP.Content.Assembly.ChipManufacturing.Debug
{
    /// <summary>上帝模式下无材料、工时和制作者地完成制造台全部有效芯片账单。</summary>
    public static class ChipFabricatorDebugCompletionService
    {
        /// <summary>完成当前全部有效芯片账单，并保留意外的来源缺失账单。</summary>
        public static ChipFabricatorDebugCompletionReport CompleteAll(
            Building_ChipFabricator building)
        {
            ChipFabricatorDebugCompletionReport report =
                new ChipFabricatorDebugCompletionReport();
            BillStack stack = building?.BillStack;
            if (stack == null || building.Map == null || !building.Spawned)
            {
                return report;
            }

            List<Bill_ChipProduction> bills = CollectChipBills(stack);
            ChipCombinationResolver resolver = new ChipCombinationResolver();
            for (int index = 0; index < bills.Count; index++)
            {
                Bill_ChipProduction bill = bills[index];
                report.RecordEncounteredBill();
                ChipCombinationRecord record = bill.CombinationRecord;
                ChipCombinationResolution resolution = resolver.Resolve(record);
                if (resolution.Status != ChipCombinationResolutionStatus.Valid
                    || bill.repeatMode != BillRepeatModeDefOf.RepeatCount)
                {
                    report.RecordSkippedBill();
                    continue;
                }

                if (!TryReclaimBoundUnfinished(building, bill))
                {
                    bill.suspended = true;
                    report.RecordSkippedBill();
                    continue;
                }

                int targetCount = Math.Max(0, bill.repeatCount);
                int completedCount = 0;
                while (completedCount < targetCount
                    && TryProduceOne(building, bill, record))
                {
                    completedCount++;
                    report.RecordProducedChip();
                }

                bill.repeatCount = targetCount - completedCount;
                if (bill.repeatCount <= 0)
                {
                    stack.Delete(bill);
                    report.RecordCompletedBill();
                }
                else
                {
                    report.RecordSkippedBill();
                }
            }

            return report;
        }

        /// <summary>按当前账单顺序建立稳定快照，避免删除时修改遍历集合。</summary>
        private static List<Bill_ChipProduction> CollectChipBills(BillStack stack)
        {
            List<Bill_ChipProduction> result = new List<Bill_ChipProduction>();
            for (int index = 0; index < stack.Bills.Count; index++)
            {
                if (stack.Bills[index] is Bill_ChipProduction bill)
                {
                    result.Add(bill);
                }
            }

            return result;
        }

        /// <summary>直接建立一枚正式成品，复制组合记录并落在制造台交互格附近。</summary>
        private static bool TryProduceOne(
            Building_ChipFabricator building,
            Bill_ChipProduction bill,
            ChipCombinationRecord record)
        {
            ThingDef productDef = bill?.recipe?.ProducedThingDef;
            if (productDef == null || record == null)
            {
                return false;
            }

            VerseThing product = ThingMaker.MakeThing(productDef);
            CompManufacturedChip comp = product.TryGetComp<CompManufacturedChip>();
            if (comp == null)
            {
                product.Destroy(DestroyMode.Vanish);
                return false;
            }

            comp.InitializeFromBill(record);
            if (GenPlace.TryPlaceThing(
                product,
                building.InteractionCell,
                building.Map,
                ThingPlaceMode.Near))
            {
                return true;
            }

            product.Destroy(DestroyMode.Vanish);
            return false;
        }

        /// <summary>终止相关工作，完整退回已投入材料并移除不再需要的半成品。</summary>
        private static bool TryReclaimBoundUnfinished(
            Building_ChipFabricator building,
            Bill_ChipProduction bill)
        {
            UnfinishedThing unfinished = bill?.BoundUft;
            if (unfinished == null)
            {
                return true;
            }

            InterruptRelatedJobs(building.Map, bill, unfinished);
            for (int index = unfinished.ingredients.Count - 1; index >= 0; index--)
            {
                VerseThing ingredient = unfinished.ingredients[index];
                if (ingredient != null && !ingredient.Destroyed && !ingredient.Spawned)
                {
                    if (!GenPlace.TryPlaceThing(
                        ingredient,
                        building.InteractionCell,
                        building.Map,
                        ThingPlaceMode.Near))
                    {
                        return false;
                    }
                }

                unfinished.ingredients.RemoveAt(index);
            }

            unfinished.BoundBill = null;
            if (!unfinished.Destroyed)
            {
                unfinished.Destroy(DestroyMode.Vanish);
            }

            return true;
        }

        /// <summary>结束当前或排队中直接引用目标账单、半成品的 Pawn 工作。</summary>
        private static void InterruptRelatedJobs(
            Map map,
            Bill_ChipProduction bill,
            UnfinishedThing unfinished)
        {
            if (map?.mapPawns?.AllPawns == null)
            {
                return;
            }

            List<Pawn> pawns = map.mapPawns.AllPawns;
            for (int pawnIndex = 0; pawnIndex < pawns.Count; pawnIndex++)
            {
                Pawn pawn = pawns[pawnIndex];
                if (pawn?.jobs == null)
                {
                    continue;
                }

                List<Job> jobs = new List<Job>(pawn.jobs.AllJobs());
                for (int jobIndex = 0; jobIndex < jobs.Count; jobIndex++)
                {
                    Job job = jobs[jobIndex];
                    if (job != null
                        && (ReferenceEquals(job.bill, bill)
                            || job.AnyTargetIs(unfinished)))
                    {
                        pawn.jobs.EndCurrentOrQueuedJob(
                            job,
                            JobCondition.Incompletable,
                            false,
                            false);
                    }
                }
            }
        }
    }
}
