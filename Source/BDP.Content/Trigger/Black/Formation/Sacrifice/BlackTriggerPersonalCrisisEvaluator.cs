using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>
    /// 个人危机判定。它只负责回答“此刻是否真的来不及了”，不执行献祭。
    /// </summary>
    public static class BlackTriggerPersonalCrisisEvaluator
    {
        /// <summary>返回当前是否满足个人危机门槛。</summary>
        public static bool IsCrisis(Pawn pawn, out string reasonKey)
        {
            reasonKey = null;
            if (!CanUseSacrifice(pawn))
            {
                reasonKey = "BDP_BlackTrigger_Sacrifice_Unavailable";
                return false;
            }

            BlackTriggerMakerSnapshot snapshot = BlackTriggerMakerSnapshotFactory.Capture(
                pawn, BlackTriggerFormationSource.Sacrifice, null, null);
            GameComponent_BlackTriggerWorld world = BlackTriggerFormationWorldService.Resolve();
            BlackTriggerFormationEvaluation evaluation = BlackTriggerFormationMatcher.Evaluate(snapshot);
            evaluation.CandidatePlans.RemoveAll(value => value?.ThingDef == null
                || BlackTriggerFormationWorldService.IsFinalized(world, value.ThingDef.defName));
            if (evaluation.CandidatePlans.Count == 0)
            {
                reasonKey = "BDP_BlackTrigger_Sacrifice_NoCompatibleTrigger";
                return false;
            }

            BlackTriggerFormationSettingsDef settings = BlackTriggerFormationWorldService.ResolveSettings();
            if (HealthUtility.TicksUntilDeathDueToBloodLoss(pawn) <= settings.crisisBleedoutTicks)
            {
                reasonKey = "BDP_BlackTrigger_Sacrifice_Bleedout";
                return true;
            }

            List<Hediff> hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs != null)
            {
                for (int index = 0; index < hediffs.Count; index++)
                {
                    Hediff hediff = hediffs[index];
                    if (hediff?.def == null || !hediff.Visible)
                    {
                        continue;
                    }
                    if (hediff.IsLethal && hediff.def.lethalSeverity > 0f
                        && hediff.Severity >= hediff.def.lethalSeverity * settings.crisisLethalSeverityRatio)
                    {
                        reasonKey = "BDP_BlackTrigger_Sacrifice_LethalHediff";
                        return true;
                    }
                }
            }

            if (pawn.Downed && HasLifeThreateningHediff(pawn))
            {
                reasonKey = "BDP_BlackTrigger_Sacrifice_LifeThreatening";
                return true;
            }
            reasonKey = "BDP_BlackTrigger_Sacrifice_NotCritical";
            return false;
        }

        /// <summary>只检查献祭基础资格，不检查“正在死亡”的危机条件。</summary>
        public static bool CanUseSacrifice(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && !pawn.Dead
                && pawn.IsColonistPlayerControlled
                && pawn.DevelopmentalStage.Adult()
                && !pawn.InMentalState
                && !BlackTriggerExhaustionUtility.IsExhausted(pawn)
                && pawn.health != null
                && pawn.health.hediffSet != null
                && pawn.health.capacities != null
                && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness)
                && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) >= 0.5f
                && pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic, false) == null
                && !pawn.health.InPainShock
                && !IsSleeping(pawn)
                && !HasPendingFormation()
                && (Find.TickManager?.TicksGame ?? 0) >= (BlackTriggerFormationWorldService.Resolve()?.NextFormationAllowedTick ?? 0);
        }

        /// <summary>睡眠与休息岗位不提供主动献祭按钮。</summary>
        private static bool IsSleeping(Pawn pawn)
        {
            JobDef jobDef = pawn.CurJobDef;
            return jobDef == JobDefOf.Wait_Asleep
                || jobDef == JobDefOf.Wait_AsleepDormancy
                || jobDef == JobDefOf.Wait_WithSleeping;
        }

        /// <summary>已有全局待决事件时不再向任何人物开放燃尽入口。</summary>
        private static bool HasPendingFormation()
        {
            return BlackTriggerFormationWorldService.Resolve()?.PendingEvent != null;
        }

        /// <summary>使用当前程序集稳定可见的 Hediff 字段做保守生命危险判断。</summary>
        private static bool HasLifeThreateningHediff(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                return false;
            }
            for (int index = 0; index < hediffs.Count; index++)
            {
                Hediff hediff = hediffs[index];
                if (hediff?.def == null || !hediff.Visible)
                {
                    continue;
                }
                if (hediff.IsLethal && (hediff.def.lethalSeverity <= 0f
                    || hediff.Severity >= hediff.def.lethalSeverity * 0.5f))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
