using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>把冻结候选与玩家决定原子提交为世界唯一黑触发器。</summary>
    public static class BlackTriggerFormationFinalizationService
    {
        private const string NoneSelection = "None";

        /// <summary>完成指定待决事件；技术失败时保留尸体、事件和选择以便重试。</summary>
        public static bool TryFinalize(string eventId)
        {
            GameComponent_BlackTriggerWorld world = BlackTriggerFormationWorldService.Resolve();
            BlackTriggerFormationEvent formationEvent = world?.FindPending(eventId);
            string validationFailure;
            BlackTriggerCandidatePlan candidate;
            BlackTriggerHeirCandidateSnapshot heir;
            BlackTriggerTraitOptionSnapshot trait;
            if (!TryResolveDecision(world, formationEvent, out candidate, out heir, out trait, out validationFailure))
            {
                if (formationEvent != null) formationEvent.LastTechnicalFailureKey = validationFailure;
                return false;
            }

            Thing result = null;
            Pawn sourcePawn = formationEvent.Corpse.InnerPawn;
            bool ledgerAdded = false;
            bool exhaustionAdded = false;
            bool corpseDestroyed = false;
            try
            {
                result = ThingMaker.MakeThing(candidate.ThingDef);
                CompBlackTriggerImprint imprint = result.TryGetComp<CompBlackTriggerImprint>();
                if (imprint == null) throw new InvalidOperationException("Final black trigger has no CompBlackTriggerImprint.");
                List<BlackTriggerFactSnapshot> evidence = BlackTriggerImprintFormatter.ResolveEvidence(formationEvent.Snapshot, candidate);
                imprint.Configure(formationEvent.Snapshot, candidate, evidence, heir, trait);

                if (!TryPlaceBesideOrWithCorpse(result, formationEvent.Corpse))
                {
                    formationEvent.LastTechnicalFailureKey = "BDP_BlackTrigger_Formation_PlacementFailed";
                    result.Destroy(DestroyMode.Vanish);
                    return false;
                }

                if (BlackTriggerFormationWorldService.IsFinalized(world, candidate.ThingDef.defName))
                {
                    formationEvent.LastTechnicalFailureKey = "BDP_BlackTrigger_Formation_CandidateUnavailable";
                    result.Destroy(DestroyMode.Vanish);
                    return false;
                }

                world.FinalizedThingDefNames.Add(candidate.ThingDef.defName);
                ledgerAdded = true;
                if (!BlackTriggerExhaustionUtility.IsExhausted(sourcePawn))
                {
                    BlackTriggerExhaustionUtility.MarkExhausted(sourcePawn);
                    exhaustionAdded = true;
                }

                formationEvent.Corpse.Destroy(DestroyMode.Vanish);
                corpseDestroyed = true;
                formationEvent.Snapshot.DetachRuntimeReferences();
                BlackTriggerFormationWorldService.Complete(eventId);
                SendSuccessLetter(result, formationEvent.Snapshot, candidate.AssignedEmphasis);
                return true;
            }
            catch (Exception exception)
            {
                if (corpseDestroyed)
                {
                    BlackTriggerFormationWorldService.Complete(eventId);
                    Log.Error("[BDP.BlackTrigger] formation completed after corpse destruction, but post-commit notification failed: " + exception);
                    return true;
                }
                else
                {
                    if (ledgerAdded) world.FinalizedThingDefNames.Remove(candidate.ThingDef.defName);
                    if (exhaustionAdded) BlackTriggerExhaustionUtility.RollbackExhaustion(sourcePawn);
                    if (result != null && !result.Destroyed) result.Destroy(DestroyMode.Vanish);
                    formationEvent.LastTechnicalFailureKey = "BDP_BlackTrigger_Formation_TechnicalFailure";
                }
                Log.Error("[BDP.BlackTrigger] formation transaction failed: " + exception);
                return false;
            }
        }

        /// <summary>验证当前选择并稳定解析一件最终候选。</summary>
        private static bool TryResolveDecision(
            GameComponent_BlackTriggerWorld world,
            BlackTriggerFormationEvent formationEvent,
            out BlackTriggerCandidatePlan candidate,
            out BlackTriggerHeirCandidateSnapshot heir,
            out BlackTriggerTraitOptionSnapshot trait,
            out string failureKey)
        {
            candidate = null;
            heir = null;
            trait = null;
            failureKey = "BDP_BlackTrigger_Formation_TechnicalFailure";
            if (world == null || formationEvent?.Snapshot == null || formationEvent.Corpse == null
                || formationEvent.Corpse.Destroyed || formationEvent.Corpse.InnerPawn == null
                || !formationEvent.Corpse.InnerPawn.Dead) return false;
            if (formationEvent.SelectedEmphasisDefName.NullOrEmpty())
            {
                failureKey = "BDP_BlackTrigger_Formation_SelectEmphasis";
                return false;
            }
            List<BlackTriggerCandidatePlan> available = (formationEvent.CandidatePlans ?? new List<BlackTriggerCandidatePlan>())
                .Where(value => value?.ThingDef != null
                    && value.AssignedEmphasis?.defName == formationEvent.SelectedEmphasisDefName
                    && !BlackTriggerFormationWorldService.IsFinalized(world, value.ThingDef.defName))
                .GroupBy(value => value.ThingDef.defName, StringComparer.Ordinal)
                .Select(value => value.First())
                .ToList();
            if (available.Count == 0)
            {
                failureKey = "BDP_BlackTrigger_Formation_CandidateUnavailable";
                return false;
            }
            if (formationEvent.HeirLoadId.NullOrEmpty())
            {
                failureKey = "BDP_BlackTrigger_Formation_SelectHeir";
                return false;
            }
            if (formationEvent.HeirLoadId != NoneSelection)
            {
                heir = formationEvent.Snapshot.HeirCandidates?.FirstOrDefault(value => value?.LoadId == formationEvent.HeirLoadId);
                if (heir?.Pawn == null || heir.Pawn.Dead)
                {
                    formationEvent.HeirLoadId = null;
                    failureKey = "BDP_BlackTrigger_Formation_HeirUnavailable";
                    return false;
                }
            }
            if (formationEvent.SelectedTraitOptionKey.NullOrEmpty())
            {
                failureKey = "BDP_BlackTrigger_Formation_SelectTrait";
                return false;
            }
            if (formationEvent.SelectedTraitOptionKey != NoneSelection)
            {
                trait = formationEvent.Snapshot.TraitOptions?.FirstOrDefault(value => value?.StableKey == formationEvent.SelectedTraitOptionKey);
                if (trait == null)
                {
                    failureKey = "BDP_BlackTrigger_Formation_TraitUnavailable";
                    return false;
                }
            }
            candidate = BlackTriggerStableRandom.PickWeighted(
                available,
                formationEvent.Snapshot.StableSeed,
                formationEvent.EventId + ":" + formationEvent.SelectedEmphasisDefName);
            return candidate != null;
        }

        /// <summary>优先放在尸体所在地图根位置；商队尸体则放回同一持有者物品栏。</summary>
        private static bool TryPlaceBesideOrWithCorpse(Thing result, Corpse corpse)
        {
            Map map = corpse?.MapHeld;
            if (map != null)
            {
                IntVec3 position = corpse.PositionHeld;
                return position.IsValid && GenPlace.TryPlaceThing(result, position, map, ThingPlaceMode.Near);
            }
            Pawn inventoryHolder = ThingOwnerUtility.GetFirstParentThing(corpse) as Pawn;
            return inventoryHolder?.inventory?.innerContainer != null
                && inventoryHolder.inventory.innerContainer.TryAdd(result);
        }

        /// <summary>发送原版蓝色正面事件信件。</summary>
        private static void SendSuccessLetter(Thing result, BlackTriggerMakerSnapshot snapshot, BlackTriggerEmphasisDef emphasis)
        {
            ChoiceLetter letter = LetterMaker.MakeLetter(
                "BDP_BlackTrigger_Success_Label".Translate(),
                "BDP_BlackTrigger_Success_Text".Translate(
                    snapshot.MakerLabel,
                    result.LabelCap.ToString(),
                    emphasis?.LabelCap.ToString() ?? "BDP_BlackTrigger_Imprint_None".Translate().ToString()),
                LetterDefOf.PositiveEvent,
                new LookTargets(result));
            Find.LetterStack.ReceiveLetter(letter);
        }
    }
}
