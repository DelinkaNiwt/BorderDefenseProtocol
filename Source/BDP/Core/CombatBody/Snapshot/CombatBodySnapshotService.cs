using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体快照服务。
    /// 它只负责捕获与恢复，不承担主链编排职责。
    /// </summary>
    internal sealed class CombatBodySnapshotService
    {
        private readonly CombatBodySnapshotPolicy policy;

        public CombatBodySnapshotService(CombatBodySnapshotPolicy policy)
        {
            this.policy = policy;
        }

        public void Capture(Pawn pawn, HostState hostState)
        {
            if (pawn == null || hostState == null)
            {
                return;
            }

            if (hostState.SnapshotState == null)
            {
                hostState.SnapshotState = new CombatBodySnapshotState();
            }

            ResetSessionContainersForCapture(pawn, hostState.SnapshotState);
            hostState.SnapshotState.ClearRecordedStates();
            CaptureHediffs(pawn, hostState.SnapshotState);
            CaptureNeeds(pawn, hostState.SnapshotState);
            CaptureApparel(pawn, hostState.SnapshotState);
            CaptureInventory(pawn, hostState.SnapshotState);
            hostState.SnapshotState.IsCaptured = true;
            hostState.HasSnapshot = true;
        }

        public void Restore(Pawn pawn, HostState hostState)
        {
            if (pawn == null || hostState == null || hostState.SnapshotState == null)
            {
                return;
            }

            RestoreApparel(pawn, hostState.SnapshotState);
            RestoreInventory(pawn, hostState.SnapshotState);
            RestoreNeeds(pawn, hostState.SnapshotState);
            RestoreHediffs(pawn, hostState.SnapshotState);
            hostState.SnapshotState.ClearSessionContainers();
            hostState.SnapshotState.ClearRecordedStates();
            hostState.SnapshotState.IsCaptured = false;
            hostState.HasSnapshot = false;
        }

        /// <summary>
        /// 在进入新一轮抓取前重置原物暂存容器。
        /// 这些容器属于单轮会话资源，不允许跨轮残留。
        /// </summary>
        private static void ResetSessionContainersForCapture(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn == null || snapshotState == null)
            {
                return;
            }

            snapshotState.ClearSessionContainers();
        }

        private static void CaptureApparel(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn?.apparel == null || snapshotState?.OriginalApparelContainer == null)
            {
                return;
            }

            List<Apparel> wornApparel = pawn.apparel.WornApparel.ToList();
            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];
                snapshotState.ApparelLockedStates[apparel.thingIDNumber] = pawn.apparel.IsLocked(apparel);
                snapshotState.ApparelForcedStates[apparel.thingIDNumber] = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false;
                pawn.apparel.Remove(apparel);
                snapshotState.OriginalApparelContainer.TryAdd(apparel);
            }
        }

        private static void CaptureInventory(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn?.inventory?.innerContainer == null || snapshotState?.OriginalInventoryContainer == null)
            {
                return;
            }

            List<Thing> items = pawn.inventory.innerContainer.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                Thing item = items[i];
                snapshotState.ItemNotForSaleStates[item.thingIDNumber] = pawn.inventory.NotForSale(item);
                snapshotState.ItemUnpackedCaravanStates[item.thingIDNumber] = IsUnpackedCaravanItem(pawn, item);
                pawn.inventory.innerContainer.TryTransferToContainer(item, snapshotState.OriginalInventoryContainer);
            }
        }

        private static void CaptureNeeds(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn?.needs == null)
            {
                return;
            }

            List<Need> allNeeds = pawn.needs.AllNeeds;
            for (int i = 0; i < allNeeds.Count; i++)
            {
                Need need = allNeeds[i];
                if (need?.def == null)
                {
                    continue;
                }

                snapshotState.NeedSnapshots.Add(new CombatBodySnapshotNeedRecord
                {
                    needDefName = need.def.defName,
                    curLevel = need.CurLevel
                });
            }
        }

        private void CaptureHediffs(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            HashSet<string> excludedHediffDefNames = policy.GetExcludedHediffDefNames();
            List<Type> excludedHediffTypes = policy.GetExcludedHediffTypes();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff == null || IsExcludedHediff(hediff, excludedHediffDefNames, excludedHediffTypes))
                {
                    continue;
                }

                if (hediff.def == HediffDefOf.MissingBodyPart && hediff.Part?.parent != null && AnyAncestorIsUnavailable(pawn, hediff.Part))
                {
                    continue;
                }

                int bodyPartIndex = 0;
                if (hediff.Part != null)
                {
                    bodyPartIndex = pawn.RaceProps.body.AllParts.Where(p => p.def == hediff.Part.def).ToList().IndexOf(hediff.Part);
                    if (bodyPartIndex < 0)
                    {
                        bodyPartIndex = 0;
                    }
                }

                CombatBodySnapshotHediffRecord record = new CombatBodySnapshotHediffRecord
                {
                    defName = hediff.def.defName,
                    severity = hediff.Severity,
                    bodyPartDefName = hediff.Part?.def?.defName ?? string.Empty,
                    bodyPartIndex = bodyPartIndex,
                    ageTicks = hediff.ageTicks,
                    sourceLabel = hediff.sourceLabel,
                    sourceDefName = hediff.sourceDef?.defName,
                    sourceToolLabel = hediff.sourceToolLabel
                };

                if (hediff is Hediff_Level levelHediff)
                {
                    record.level = levelHediff.level;
                }

                HediffComp_GetsPermanent permComp = hediff.TryGetComp<HediffComp_GetsPermanent>();
                if (permComp != null)
                {
                    record.isPermanent = permComp.IsPermanent;
                    record.painCategory = (int)permComp.PainCategory;
                }

                if (hediff is Hediff_MissingPart missingPart)
                {
                    record.isFresh = missingPart.IsFresh;
                    record.lastInjuryDefName = missingPart.lastInjury?.defName;
                }

                snapshotState.HediffSnapshots.Add(record);
            }
        }

        private static void RestoreApparel(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn?.apparel == null || snapshotState?.OriginalApparelContainer == null)
            {
                return;
            }

            List<Apparel> originalApparels = snapshotState.OriginalApparelContainer.InnerListForReading.ToList();
            for (int i = 0; i < originalApparels.Count; i++)
            {
                Apparel apparel = originalApparels[i];

                if (pawn.apparel.WornApparel.Contains(apparel))
                {
                    snapshotState.OriginalApparelContainer.Remove(apparel);
                    continue;
                }

                snapshotState.OriginalApparelContainer.Remove(apparel);

                bool wasLocked = snapshotState.ApparelLockedStates.TryGetValue(apparel.thingIDNumber, out bool locked) && locked;
                pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: wasLocked);

                if (snapshotState.ApparelForcedStates.TryGetValue(apparel.thingIDNumber, out bool wasForced)
                    && wasForced
                    && pawn.outfits?.forcedHandler != null)
                {
                    pawn.outfits.forcedHandler.SetForced(apparel, true);
                }
            }
        }

        private static void RestoreInventory(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn?.inventory?.innerContainer == null || snapshotState?.OriginalInventoryContainer == null)
            {
                return;
            }

            List<Thing> items = snapshotState.OriginalInventoryContainer.InnerListForReading.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                Thing item = items[i];

                if (item.holdingOwner == pawn.inventory.innerContainer)
                {
                    snapshotState.OriginalInventoryContainer.Remove(item);
                }
                else
                {
                    snapshotState.OriginalInventoryContainer.TryTransferToContainer(item, pawn.inventory.innerContainer);
                }

                if (snapshotState.ItemNotForSaleStates.TryGetValue(item.thingIDNumber, out bool wasNotForSale) && wasNotForSale)
                {
                    pawn.inventory.TryAddItemNotForSale(item);
                }

                if (snapshotState.ItemUnpackedCaravanStates.TryGetValue(item.thingIDNumber, out bool wasUnpackedCaravan) && wasUnpackedCaravan)
                {
                    AddToUnpackedCaravanItems(pawn, item);
                }
            }
        }

        private static void RestoreNeeds(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn?.needs == null)
            {
                return;
            }

            for (int i = 0; i < snapshotState.NeedSnapshots.Count; i++)
            {
                CombatBodySnapshotNeedRecord record = snapshotState.NeedSnapshots[i];
                NeedDef needDef = DefDatabase<NeedDef>.GetNamedSilentFail(record.needDefName);
                Need need = needDef != null ? pawn.needs.TryGetNeed(needDef) : null;
                if (need != null)
                {
                    need.CurLevel = record.curLevel;
                }
            }
        }

        private void RestoreHediffs(Pawn pawn, CombatBodySnapshotState snapshotState)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            HashSet<string> excludedHediffDefNames = policy.GetExcludedHediffDefNames();
            List<Type> excludedHediffTypes = policy.GetExcludedHediffTypes();
            List<Hediff> toRemove = pawn.health.hediffSet.hediffs.Where(h => !IsExcludedHediff(h, excludedHediffDefNames, excludedHediffTypes)).ToList();
            for (int i = 0; i < toRemove.Count; i++)
            {
                pawn.health.RemoveHediff(toRemove[i]);
            }

            for (int i = 0; i < snapshotState.HediffSnapshots.Count; i++)
            {
                CombatBodySnapshotHediffRecord record = snapshotState.HediffSnapshots[i];
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(record.defName);
                if (def == null)
                {
                    continue;
                }

                BodyPartRecord part = null;
                if (!string.IsNullOrEmpty(record.bodyPartDefName))
                {
                    BodyPartDef partDef = DefDatabase<BodyPartDef>.GetNamedSilentFail(record.bodyPartDefName);
                    if (partDef != null)
                    {
                        List<BodyPartRecord> candidates = pawn.RaceProps.body.AllParts.Where(p => p.def == partDef).ToList();
                        if (record.bodyPartIndex < candidates.Count)
                        {
                            part = candidates[record.bodyPartIndex];
                        }
                    }
                }

                Hediff hediff = pawn.health.AddHediff(def, part);
                hediff.sourceLabel = record.sourceLabel;
                if (!string.IsNullOrEmpty(record.sourceDefName))
                {
                    hediff.sourceDef = DefDatabase<ThingDef>.GetNamedSilentFail(record.sourceDefName);
                }

                hediff.sourceToolLabel = record.sourceToolLabel;

                HediffComp_GetsPermanent permComp = hediff.TryGetComp<HediffComp_GetsPermanent>();
                if (permComp != null && record.isPermanent.HasValue)
                {
                    permComp.IsPermanent = record.isPermanent.Value;
                    if (record.painCategory.HasValue)
                    {
                        permComp.SetPainCategory((PainCategory)record.painCategory.Value);
                    }
                }

                if (hediff is Hediff_Level levelHediff && record.level.HasValue)
                {
                    levelHediff.level = record.level.Value;
                    levelHediff.Severity = levelHediff.def.initialSeverity;
                }
                else
                {
                    hediff.Severity = record.severity;
                }

                hediff.ageTicks = record.ageTicks;

                if (hediff is Hediff_MissingPart missingPart)
                {
                    if (record.isFresh.HasValue)
                    {
                        missingPart.IsFresh = record.isFresh.Value;
                    }

                    if (!string.IsNullOrEmpty(record.lastInjuryDefName))
                    {
                        missingPart.lastInjury = DefDatabase<HediffDef>.GetNamedSilentFail(record.lastInjuryDefName);
                    }
                }

                pawn.health.Notify_HediffChanged(hediff);
            }

            pawn.health.hediffSet.DirtyCache();
        }

        private static bool IsUnpackedCaravanItem(Pawn pawn, Thing item)
        {
            FieldInfo field = typeof(Pawn_InventoryTracker).GetField("unpackedCaravanItems", BindingFlags.NonPublic | BindingFlags.Instance);
            System.Collections.IList list = field?.GetValue(pawn.inventory) as System.Collections.IList;
            return list?.Contains(item) ?? false;
        }

        private static void AddToUnpackedCaravanItems(Pawn pawn, Thing item)
        {
            FieldInfo field = typeof(Pawn_InventoryTracker).GetField("unpackedCaravanItems", BindingFlags.NonPublic | BindingFlags.Instance);
            System.Collections.IList list = field?.GetValue(pawn.inventory) as System.Collections.IList;
            if (list != null && !list.Contains(item))
            {
                list.Add(item);
            }
        }

        private static bool AnyAncestorIsUnavailable(Pawn pawn, BodyPartRecord part)
        {
            BodyPartRecord current = part.parent;
            while (current != null)
            {
                bool unavailable = pawn.health.hediffSet.hediffs.Any(h => h.Part == current && (h.def == HediffDefOf.MissingBodyPart || h.def.addedPartProps != null));
                if (unavailable)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsExcludedHediff(Hediff hediff, HashSet<string> excludedHediffDefNames, List<Type> excludedHediffTypes)
        {
            if (hediff == null)
            {
                return true;
            }

            if (excludedHediffDefNames.Contains(hediff.def.defName))
            {
                return true;
            }

            Type hediffType = hediff.GetType();
            for (int i = 0; i < excludedHediffTypes.Count; i++)
            {
                if (excludedHediffTypes[i].IsAssignableFrom(hediffType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
