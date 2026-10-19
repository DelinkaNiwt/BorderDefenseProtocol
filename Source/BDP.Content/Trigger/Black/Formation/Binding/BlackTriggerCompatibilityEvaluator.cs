using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>按绑定约束、传承人旁路和物品自声明条件判断二元适格。</summary>
    public static class BlackTriggerCompatibilityEvaluator
    {
        /// <summary>返回可用或不可用，不计算共鸣分数。</summary>
        public static BlackTriggerCompatibilityResult Evaluate(Thing trigger, Pawn pawn)
        {
            BlackTriggerFormationExtension extension = trigger?.def?.GetModExtension<BlackTriggerFormationExtension>();
            if (trigger == null || pawn == null || extension == null) return Reject("BDP_BlackTrigger_Compatibility_NotBlackTrigger");
            CompBlackTriggerBiocodable code = trigger.TryGetComp<CompBlackTriggerBiocodable>();
            if (code != null && code.Biocoded && code.CodedPawn != pawn) return Reject("BDP_BlackTrigger_Compatibility_BoundToOther");
            if (BlackTriggerBindingService.HasOtherBoundBlackTrigger(pawn, trigger)) return Reject("BDP_BlackTrigger_Compatibility_AlreadyHasBlackTrigger");
            if (code != null && code.Biocoded && code.CodedPawn == pawn) return Accept();
            CompBlackTriggerImprint imprint = trigger.TryGetComp<CompBlackTriggerImprint>();
            if (imprint != null && !imprint.HeirLoadId.NullOrEmpty() && imprint.HeirLoadId == pawn.GetUniqueLoadID()) return Accept();
            List<string> reasons = new List<string>();
            bool accepted = MatchesAnyGroup(extension.eligibilityGroups, pawn, reasons);
            return accepted ? Accept() : new BlackTriggerCompatibilityResult
            {
                Accepted = false,
                ReasonKey = "BDP_BlackTrigger_Compatibility_ConditionsFailed",
                FailedReasons = reasons.Distinct().ToList()
            };
        }

        /// <summary>适格条件组之间为或关系。</summary>
        private static bool MatchesAnyGroup(List<BlackTriggerEligibilityConditionGroup> groups, Pawn pawn, List<string> reasons)
        {
            if (groups == null || groups.Count == 0) return true;
            for (int index = 0; index < groups.Count; index++)
            {
                BlackTriggerEligibilityConditionGroup group = groups[index];
                if (group == null) continue;
                bool all = (group.allOf ?? new List<BlackTriggerEligibilityCondition>()).All(condition => EvaluateCondition(condition, pawn));
                int any = (group.anyOf ?? new List<BlackTriggerEligibilityCondition>()).Count(condition => EvaluateCondition(condition, pawn));
                int anyCount = group.anyOf?.Count ?? 0;
                int anyMinimum = anyCount == 0 ? 0 : Math.Max(1, group.anyOfMinimum);
                bool none = !(group.noneOf ?? new List<BlackTriggerEligibilityCondition>()).Any(condition => EvaluateCondition(condition, pawn));
                if (all && any >= anyMinimum && none) return true;
                AddFailureReasons(group, pawn, reasons);
            }
            return false;
        }

        /// <summary>把配置提供的条件说明转换为玩家可读失败原因。</summary>
        private static void AddFailureReasons(BlackTriggerEligibilityConditionGroup group, Pawn pawn, List<string> reasons)
        {
            foreach (BlackTriggerEligibilityCondition condition in (group.allOf ?? new List<BlackTriggerEligibilityCondition>()).Concat(group.anyOf ?? new List<BlackTriggerEligibilityCondition>()))
            {
                if (condition != null && !EvaluateCondition(condition, pawn) && !condition.explanationKey.NullOrEmpty()) reasons.Add(condition.explanationKey.Translate().ToString());
            }
            foreach (BlackTriggerEligibilityCondition condition in group.noneOf ?? new List<BlackTriggerEligibilityCondition>())
            {
                if (condition != null && EvaluateCondition(condition, pawn) && !condition.explanationKey.NullOrEmpty()) reasons.Add(condition.explanationKey.Translate().ToString());
            }
        }

        /// <summary>求值一条原版数据或自定义 Worker 条件。</summary>
        private static bool EvaluateCondition(BlackTriggerEligibilityCondition condition, Pawn pawn)
        {
            if (condition == null || pawn == null) return false;
            switch (condition.kind)
            {
                case BlackTriggerEligibilityConditionKind.MinimumTrionPotential:
                    ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
                    return reader != null && reader.TrionCapacityPotential >= condition.minimumValue;
                case BlackTriggerEligibilityConditionKind.SkillInTopThree:
                    return pawn.skills?.skills != null && pawn.skills.skills
                        .Where(value => value != null && value.Level > 0 && !value.TotallyDisabled)
                        .OrderByDescending(value => value.Level)
                        .ThenByDescending(value => value.xpSinceLastLevel)
                        .ThenBy(value => value.def?.defName ?? string.Empty, StringComparer.Ordinal)
                        .Take(3)
                        .Any(value => value.def == condition.skill);
                case BlackTriggerEligibilityConditionKind.SkillLevelAtLeast:
                    SkillRecord skill = pawn.skills?.GetSkill(condition.skill);
                    return skill != null && skill.Level >= condition.minimumValue;
                case BlackTriggerEligibilityConditionKind.SkillDisabled:
                    SkillRecord disabledSkill = pawn.skills?.GetSkill(condition.skill);
                    return disabledSkill != null && disabledSkill.TotallyDisabled;
                case BlackTriggerEligibilityConditionKind.TraitDegree:
                    return pawn.story?.traits?.allTraits?.Any(value => value?.def == condition.trait && value.Degree == (int)condition.minimumValue) == true;
                case BlackTriggerEligibilityConditionKind.ActiveGene:
                    return pawn.genes?.GetGene(condition.gene)?.Active == true;
                case BlackTriggerEligibilityConditionKind.RecordNonZero:
                    return pawn.records != null && condition.record != null && pawn.records.GetValue(condition.record) > 0f;
                case BlackTriggerEligibilityConditionKind.Custom:
                    return EvaluateCustom(condition, pawn);
                default:
                    return false;
            }
        }

        /// <summary>实例化开发者显式声明的特殊适格 Worker。</summary>
        private static bool EvaluateCustom(BlackTriggerEligibilityCondition condition, Pawn pawn)
        {
            if (condition.customWorkerType.NullOrEmpty()) return false;
            Type type = GenTypes.GetTypeInAnyAssembly(condition.customWorkerType);
            if (type == null || !typeof(BlackTriggerEligibilityWorker).IsAssignableFrom(type)) return false;
            try { return ((BlackTriggerEligibilityWorker)Activator.CreateInstance(type)).Matches(pawn, condition); }
            catch (Exception exception) { Log.Error("[BDP.BlackTrigger] eligibility worker failed: " + exception); return false; }
        }

        /// <summary>构造通过结果。</summary>
        private static BlackTriggerCompatibilityResult Accept()
        {
            return new BlackTriggerCompatibilityResult { Accepted = true, ReasonKey = "BDP_BlackTrigger_Compatibility_Accepted" };
        }

        /// <summary>构造拒绝结果。</summary>
        private static BlackTriggerCompatibilityResult Reject(string key)
        {
            return new BlackTriggerCompatibilityResult { Accepted = false, ReasonKey = key };
        }
    }
}
