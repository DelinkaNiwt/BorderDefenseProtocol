using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>快照准备、唯一待决事件、冷却、尸体和截止时间的统一服务。</summary>
    public static class BlackTriggerFormationWorldService
    {
        private const string NoneSelection = "None";

        /// <summary>解析当前存档组件。</summary>
        public static GameComponent_BlackTriggerWorld Resolve()
        {
            return Current.Game?.GetComponent<GameComponent_BlackTriggerWorld>();
        }

        /// <summary>在人物死亡前完成冻结、匹配、唯一过滤、概率和冷却检查。</summary>
        public static bool TryPrepare(Pawn pawn, BlackTriggerFormationSource source, DamageInfo? dinfo, Hediff culprit, bool debug, bool applyNaturalChance, out BlackTriggerPreparedAttempt attempt, out string failureReasonKey)
        {
            attempt = null;
            failureReasonKey = null;
            GameComponent_BlackTriggerWorld world = Resolve();
            if (pawn == null || world == null || pawn.Dead || !pawn.IsColonistPlayerControlled || BlackTriggerExhaustionUtility.IsExhausted(pawn))
            {
                failureReasonKey = "BDP_BlackTrigger_Formation_SourceInvalid";
                return false;
            }
            if (world.PendingEvent != null)
            {
                failureReasonKey = "BDP_BlackTrigger_Formation_AnotherPending";
                return false;
            }
            BlackTriggerMakerSnapshot snapshot = BlackTriggerMakerSnapshotFactory.Capture(pawn, source, dinfo, culprit);
            BlackTriggerFormationEvaluation evaluation = BlackTriggerFormationMatcher.Evaluate(snapshot);
            evaluation.CandidatePlans.RemoveAll(value => value?.ThingDef == null || IsFinalized(world, value.ThingDef.defName));
            if (evaluation.CandidatePlans.Count == 0)
            {
                failureReasonKey = "BDP_BlackTrigger_Sacrifice_NoCompatibleTrigger";
                return false;
            }
            // 世界唯一过滤会改变候选—侧重二分图，必须在过滤后重新求最大覆盖。
            BlackTriggerEmphasisAssigner.Assign(evaluation.CandidatePlans, snapshot.StableSeed);
            if (!debug && (Verse.Find.TickManager?.TicksGame ?? 0) < world.NextFormationAllowedTick)
            {
                failureReasonKey = "BDP_BlackTrigger_Formation_Cooldown";
                return false;
            }
            if (!debug && applyNaturalChance && Rand.Value > ResolveSettings().naturalFormationChance)
            {
                failureReasonKey = "BDP_BlackTrigger_Formation_ChanceMissed";
                return false;
            }
            attempt = new BlackTriggerPreparedAttempt { Snapshot = snapshot, Evaluation = evaluation, Debug = debug };
            return true;
        }

        /// <summary>尸体生成后把准备结果提交为唯一待决事件。</summary>
        public static bool TryCreatePending(BlackTriggerPreparedAttempt attempt, Corpse corpse)
        {
            GameComponent_BlackTriggerWorld world = Resolve();
            if (world == null || world.PendingEvent != null || attempt?.Snapshot == null || attempt.Evaluation?.CandidatePlans?.Count == 0 || corpse == null || corpse.Destroyed) return false;
            int now = Verse.Find.TickManager?.TicksGame ?? 0;
            string eventId = "BTFE_" + attempt.Snapshot.MakerLoadId + "_" + now;
            world.PendingEvent = new BlackTriggerFormationEvent
            {
                EventId = eventId,
                Snapshot = attempt.Snapshot,
                Corpse = corpse,
                CandidatePlans = attempt.Evaluation.CandidatePlans,
                HeirLoadId = NoneSelection,
                CreatedTick = now,
                DeadlineTick = now + ResolveSettings().pendingEventTicks
            };
            List<string> emphasisNames = world.PendingEvent.CandidatePlans
                .Where(value => value?.AssignedEmphasis != null)
                .Select(value => value.AssignedEmphasis.defName)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (emphasisNames.Count == 1) world.PendingEvent.SelectedEmphasisDefName = emphasisNames[0];
            if (world.PendingEvent.Snapshot.TraitOptions.Count == 0) world.PendingEvent.SelectedTraitOptionKey = "None";
            if (!attempt.Debug) world.NextFormationAllowedTick = now + ResolveSettings().normalCooldownTicks;
            Letter_BlackTriggerFormation letter = new Letter_BlackTriggerFormation { def = LetterDefOf.NeutralEvent, EventId = eventId };
            letter.Configure();
            letter.StartTimeout(ResolveSettings().pendingEventTicks);
            Verse.Find.LetterStack.ReceiveLetter(letter);
            return true;
        }

        /// <summary>取得当前活动事件。</summary>
        public static BlackTriggerFormationEvent Find(string eventId)
        {
            return Resolve()?.FindPending(eventId);
        }

        /// <summary>判断某人物是否正承载当前待决事件。</summary>
        public static bool HasPendingForMaker(Pawn pawn)
        {
            string loadId = pawn?.GetUniqueLoadID();
            return !loadId.NullOrEmpty() && Resolve()?.PendingEvent?.Snapshot?.MakerLoadId == loadId;
        }

        /// <summary>玩家放弃或拒绝本次形成；不销毁尸体、不写耗尽。</summary>
        public static bool Abandon(string eventId)
        {
            GameComponent_BlackTriggerWorld world = Resolve();
            if (world?.PendingEvent == null || world.PendingEvent.EventId != eventId) return false;
            world.PendingEvent = null;
            RemoveLetter(eventId);
            return true;
        }

        /// <summary>确认事务成功后清除唯一待决事件。</summary>
        public static void Complete(string eventId)
        {
            GameComponent_BlackTriggerWorld world = Resolve();
            if (world?.PendingEvent?.EventId == eventId) world.PendingEvent = null;
            RemoveLetter(eventId);
        }

        /// <summary>处理尸体丢失、复活和三小时强制决策。</summary>
        public static void ScanEvent(GameComponent_BlackTriggerWorld world)
        {
            BlackTriggerFormationEvent formationEvent = world?.PendingEvent;
            if (formationEvent == null) return;
            if (formationEvent.Corpse == null || formationEvent.Corpse.Destroyed || formationEvent.Corpse.InnerPawn == null || !formationEvent.Corpse.InnerPawn.Dead)
            {
                Abandon(formationEvent.EventId);
                return;
            }
            if ((Verse.Find.TickManager?.TicksGame ?? 0) < formationEvent.DeadlineTick) return;
            formationEvent.ForceDecision = true;
            Letter_BlackTriggerFormation letter = Verse.Find.LetterStack?.LettersListForReading?
                .OfType<Letter_BlackTriggerFormation>()
                .FirstOrDefault(value => value.EventId == formationEvent.EventId);
            letter?.Configure();
            if (!formationEvent.ForcedWindowOpened)
            {
                formationEvent.ForcedWindowOpened = true;
                Verse.Find.WindowStack.Add(new Window_BlackTriggerFormationDecision(formationEvent.EventId));
            }
        }

        /// <summary>判断某种黑触发器是否已经永久形成。</summary>
        public static bool IsFinalized(GameComponent_BlackTriggerWorld world, string defName)
        {
            return world?.FinalizedThingDefNames?.Contains(defName, StringComparer.Ordinal) == true;
        }

        /// <summary>移除同一事件的旧信件。</summary>
        public static void RemoveLetter(string eventId)
        {
            if (Verse.Find.LetterStack?.LettersListForReading == null) return;
            foreach (Letter_BlackTriggerFormation letter in Verse.Find.LetterStack.LettersListForReading
                .OfType<Letter_BlackTriggerFormation>()
                .Where(value => value.EventId == eventId)
                .ToList())
            {
                Verse.Find.LetterStack.RemoveLetter(letter);
            }
        }

        /// <summary>返回当前全局设置。</summary>
        public static BlackTriggerFormationSettingsDef ResolveSettings()
        {
            return BlackTriggerFormationSettingsDefOf.BDP_BlackTriggerFormationSettings
                ?? DefDatabase<BlackTriggerFormationSettingsDef>.GetNamedSilentFail("BDP_BlackTriggerFormationSettings")
                ?? new BlackTriggerFormationSettingsDef();
        }
    }
}
