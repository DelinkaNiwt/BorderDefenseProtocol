using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Bill;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Assembly.ChipManufacturing.Patches
{
    /// <summary>让同地图任意合格角色续作相同组合的芯片半成品。</summary>
    public static class Patch_WorkGiver_DoBill_ChipUnfinishedThing
    {
        /// <summary>只替换芯片账单的最近半成品查找；普通账单回退原版。</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorkGiver_DoBill), "ClosestUnfinishedThingForBill")]
        private static bool ClosestUnfinishedThingForBillPrefix(
            Pawn pawn,
            Bill_ProductionWithUft bill,
            ref UnfinishedThing __result)
        {
            if (!(bill is Bill_ChipProduction chipBill))
            {
                return true;
            }

            Predicate<Verse.Thing> validator = candidate =>
            {
                Thing_UnfinishedChip unfinished = candidate as Thing_UnfinishedChip;
                return unfinished != null
                    && !candidate.IsForbidden(pawn)
                    && unfinished.Recipe == bill.recipe
                    && unfinished.CombinationRecord != null
                    && unfinished.CombinationRecord.SameConfigurationAs(
                        chipBill.CombinationRecord)
                    && unfinished.ingredients.TrueForAll(
                        ingredient => bill.IsFixedOrAllowedIngredient(ingredient.def))
                    && pawn.CanReserve(candidate);
            };

            __result = (UnfinishedThing)GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(bill.recipe.unfinishedThingDef),
                PathEndMode.InteractionCell,
                TraverseParms.For(pawn, pawn.NormalMaxDanger()),
                9999f,
                validator);
            return false;
        }

        /// <summary>复制原版续作 Job，只去掉 Creator 相等限制。</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorkGiver_DoBill), "FinishUftJob")]
        private static bool FinishUftJobPrefix(
            Pawn pawn,
            UnfinishedThing uft,
            Bill_ProductionWithUft bill,
            ref Job __result)
        {
            if (!(bill is Bill_ChipProduction))
            {
                return true;
            }

            Job haulOff = WorkGiverUtility.HaulStuffOffBillGiverJob(
                pawn,
                bill.billStack.billGiver,
                uft);
            if (haulOff != null && haulOff.targetA.Thing != uft)
            {
                __result = haulOff;
                return false;
            }

            Job job = JobMaker.MakeJob(
                JobDefOf.DoBill,
                (Verse.Thing)bill.billStack.billGiver);
            job.bill = bill;
            job.targetQueueB = new List<LocalTargetInfo> { uft };
            job.countQueue = new List<int> { 1 };
            job.haulMode = HaulMode.ToCellNonStorage;
            __result = job;
            return false;
        }
    }
}
