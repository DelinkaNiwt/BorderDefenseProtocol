using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>让每件黑触发器用自己的路线评估完整来源快照。</summary>
    public static class BlackTriggerFormationMatcher
    {
        /// <summary>返回所有不同 ThingDef 候选及全部路线诊断。</summary>
        public static BlackTriggerFormationEvaluation Evaluate(BlackTriggerMakerSnapshot snapshot)
        {
            BlackTriggerFormationEvaluation result = new BlackTriggerFormationEvaluation { Snapshot = snapshot };
            if (snapshot == null)
            {
                result.FailureReasonKey = "BDP_BlackTrigger_Formation_NoCandidate";
                return result;
            }
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(value => value?.GetModExtension<BlackTriggerFormationExtension>()?.formationRoutes?.Count > 0
                    && BlackTriggerFormationDefValidator.IsValid(value))
                .OrderBy(value => value.defName, StringComparer.Ordinal)
                .ToList();
            for (int defIndex = 0; defIndex < defs.Count; defIndex++)
            {
                ThingDef def = defs[defIndex];
                BlackTriggerFormationExtension extension = def.GetModExtension<BlackTriggerFormationExtension>();
                BlackTriggerCandidatePlan plan = new BlackTriggerCandidatePlan { ThingDef = def };
                for (int routeIndex = 0; routeIndex < extension.formationRoutes.Count; routeIndex++)
                {
                    BlackTriggerRouteEvaluation routeResult = EvaluateRoute(def, extension.formationRoutes[routeIndex], snapshot.Facts, routeIndex);
                    result.RouteEvaluations.Add(routeResult);
                    if (!routeResult.Passed) continue;
                    BlackTriggerMatchedRouteSnapshot passed = new BlackTriggerMatchedRouteSnapshot
                    {
                        RouteId = routeResult.RouteId,
                        MatchedFactKeys = routeResult.MatchedFactKeys.Distinct(StringComparer.Ordinal).ToList(),
                        Emphasis = routeResult.Emphasis
                    };
                    plan.MatchedRoutes.Add(passed);
                    if (passed.Emphasis != null) plan.AvailableEmphases.Add(passed.Emphasis);
                }
                plan.AvailableEmphases = plan.AvailableEmphases
                    .Where(value => value != null)
                    .GroupBy(value => value.defName, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .OrderBy(value => value.defName, StringComparer.Ordinal)
                    .ToList();
                int traitWeight = BlackTriggerTraitWeightCalculator.Resolve(
                    extension.adaptedTraits,
                    snapshot.SourceTraits,
                    plan.MatchedTraitKeys);
                int abilityWeight = BlackTriggerAbilityWeightCalculator.Resolve(
                    extension.adaptedAbilities,
                    snapshot.SourceAbilities,
                    plan.MatchedAbilityKeys);
                plan.FormationWeight = traitWeight + abilityWeight - BlackTriggerTraitWeightCalculator.BaseWeight;
                if (plan.MatchedRoutes.Count > 0 && plan.AvailableEmphases.Count > 0) result.CandidatePlans.Add(plan);
            }
            if (result.CandidatePlans.Count == 0)
            {
                result.FailureReasonKey = "BDP_BlackTrigger_Formation_NoCandidate";
                return result;
            }
            BlackTriggerEmphasisAssigner.Assign(result.CandidatePlans, snapshot.StableSeed);
            result.CandidatePlans.RemoveAll(value => value.AssignedEmphasis == null);
            if (result.CandidatePlans.Count == 0) result.FailureReasonKey = "BDP_BlackTrigger_Formation_NoCandidate";
            return result;
        }

        /// <summary>判断快照是否至少匹配一件完整黑触发器。</summary>
        public static bool HasAnyCandidate(BlackTriggerMakerSnapshot snapshot)
        {
            return Evaluate(snapshot).Passed;
        }

        /// <summary>求值一条路线并保留开发诊断信息。</summary>
        private static BlackTriggerRouteEvaluation EvaluateRoute(ThingDef def, BlackTriggerFormationRoute route, List<BlackTriggerFactSnapshot> facts, int routeIndex)
        {
            BlackTriggerRouteEvaluation result = new BlackTriggerRouteEvaluation
            {
                ThingDef = def,
                RouteId = route?.routeId.NullOrEmpty() == false ? route.routeId : def.defName + "#" + (routeIndex + 1),
                Emphasis = route?.emphasis
            };
            if (route == null)
            {
                result.FailureDetails.Add("route:null");
                return result;
            }
            List<BlackTriggerFactSnapshot> sourceFacts = facts ?? new List<BlackTriggerFactSnapshot>();
            List<BlackTriggerFactReference> all = route.allOf ?? new List<BlackTriggerFactReference>();
            List<BlackTriggerFactReference> any = route.anyOf ?? new List<BlackTriggerFactReference>();
            List<BlackTriggerFactReference> none = route.noneOf ?? new List<BlackTriggerFactReference>();
            if (all.Count == 0 && any.Count == 0 && none.Count == 0)
            {
                result.FailureDetails.Add("route:no-conditions");
                return result;
            }
            bool allPassed = true;
            for (int index = 0; index < all.Count; index++)
            {
                BlackTriggerFactSnapshot match = FindMatch(all[index], sourceFacts);
                if (match == null) { allPassed = false; result.FailureDetails.Add("missing:" + (all[index]?.Key ?? "null")); }
                else result.MatchedFactKeys.Add(match.StableKey);
            }
            int anyMatches = 0;
            for (int index = 0; index < any.Count; index++)
            {
                BlackTriggerFactSnapshot match = FindMatch(any[index], sourceFacts);
                if (match == null) continue;
                anyMatches++;
                result.MatchedFactKeys.Add(match.StableKey);
            }
            int requiredAny = any.Count == 0 ? 0 : Math.Max(1, route.anyOfMinimum);
            bool anyPassed = anyMatches >= requiredAny;
            if (!anyPassed) result.FailureDetails.Add("anyOf:" + anyMatches + "/" + requiredAny);
            bool nonePassed = true;
            for (int index = 0; index < none.Count; index++)
            {
                BlackTriggerFactSnapshot match = FindMatch(none[index], sourceFacts);
                if (match == null) continue;
                nonePassed = false;
                result.FailureDetails.Add("excluded:" + match.StableKey);
            }
            result.Passed = allPassed && anyPassed && nonePassed && result.Emphasis != null;
            if (result.Emphasis == null) result.FailureDetails.Add("route:no-emphasis");
            return result;
        }

        /// <summary>在冻结事实中查找引用的实际命中项。</summary>
        private static BlackTriggerFactSnapshot FindMatch(BlackTriggerFactReference reference, List<BlackTriggerFactSnapshot> facts)
        {
            return reference == null ? null : facts.FirstOrDefault(reference.Matches);
        }
    }

    /// <summary>保留单一诊断入口，语义与正式匹配完全一致。</summary>
    public static class BlackTriggerFormationEvaluator
    {
        /// <summary>评估完整冻结快照。</summary>
        public static BlackTriggerFormationEvaluation Evaluate(BlackTriggerMakerSnapshot snapshot)
        {
            return BlackTriggerFormationMatcher.Evaluate(snapshot);
        }
    }
}
