using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>启动期校验形成路线和适格条件，错误定义不会进入形成池。</summary>
    public static class BlackTriggerFormationDefValidator
    {
        private static readonly HashSet<string> invalidThingDefs = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>返回某件物品的形成路线是否通过启动校验。</summary>
        public static bool IsValid(ThingDef def)
        {
            return def != null && !invalidThingDefs.Contains(def.defName);
        }

        /// <summary>校验所有带形成扩展的 ThingDef。</summary>
        public static void ValidateAll()
        {
            invalidThingDefs.Clear();
            List<string> errors = new List<string>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                BlackTriggerFormationExtension extension = def?.GetModExtension<BlackTriggerFormationExtension>();
                if (extension == null) continue;
                int before = errors.Count;
                ValidateThing(def, extension, errors);
                if (errors.Count > before) invalidThingDefs.Add(def.defName);
            }
            if (errors.Count > 0) Log.Error("[BDP.BlackTrigger] formation definition validation failed:\n" + string.Join("\n", errors.ToArray()));
        }

        /// <summary>校验一件黑触发器的形成路线与适格声明。</summary>
        private static void ValidateThing(ThingDef def, BlackTriggerFormationExtension extension, List<string> errors)
        {
            if (extension.formationRoutes != null)
            {
                HashSet<string> routeIds = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < extension.formationRoutes.Count; index++)
                {
                    BlackTriggerFormationRoute route = extension.formationRoutes[index];
                    if (route == null) { errors.Add(def.defName + ":empty formation route"); continue; }
                    if (!route.routeId.NullOrEmpty() && !routeIds.Add(route.routeId)) errors.Add(def.defName + ":duplicate route id " + route.routeId);
                    int conditionCount = (route.allOf?.Count ?? 0) + (route.anyOf?.Count ?? 0) + (route.noneOf?.Count ?? 0);
                    if (conditionCount == 0) errors.Add(def.defName + ":route has no conditions");
                    if (route.emphasis == null) errors.Add(def.defName + ":route has no valid emphasis");
                    ValidateFactList(def.defName, route.allOf, errors, "allOf");
                    ValidateFactList(def.defName, route.anyOf, errors, "anyOf");
                    ValidateFactList(def.defName, route.noneOf, errors, "noneOf");
                    HashSet<string> positiveKeys = new HashSet<string>(
                        (route.allOf ?? new List<BlackTriggerFactReference>())
                            .Concat(route.anyOf ?? new List<BlackTriggerFactReference>())
                            .Where(value => value?.IsValid == true)
                            .Select(value => value.Key),
                        StringComparer.Ordinal);
                    if ((route.noneOf ?? new List<BlackTriggerFactReference>())
                        .Any(value => value?.IsValid == true && positiveKeys.Contains(value.Key)))
                    {
                        errors.Add(def.defName + ":route " + route.routeId + " requires and excludes the same fact");
                    }
                    int anyCount = route.anyOf?.Count ?? 0;
                    if (anyCount == 0 && route.anyOfMinimum > 1) errors.Add(def.defName + ":anyOfMinimum exists without anyOf");
                    if (anyCount > 0 && (route.anyOfMinimum < 1 || route.anyOfMinimum > anyCount)) errors.Add(def.defName + ":anyOfMinimum out of range");
                }
            }
            ValidateAdaptedTraits(def.defName, extension.adaptedTraits, errors);
            ValidateAdaptedAbilities(def.defName, extension.adaptedAbilities, errors);
            ValidateEligibility(def.defName, extension.eligibilityGroups, errors);
        }

        /// <summary>校验隐藏适应特性声明的 Def、degree 和重复项。</summary>
        private static void ValidateAdaptedTraits(string defName, List<BlackTriggerAdaptedTrait> traits, List<string> errors)
        {
            if (traits == null) return;
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < traits.Count; index++)
            {
                BlackTriggerAdaptedTrait adapted = traits[index];
                if (adapted == null || adapted.trait == null)
                {
                    errors.Add(defName + ":adaptedTraits contains an invalid TraitDef");
                    continue;
                }
                if (adapted.HasSpecificDegree && adapted.trait.DataAtDegree(adapted.degree) == null)
                {
                    errors.Add(defName + ":adaptedTraits contains an invalid degree for " + adapted.trait.defName);
                }
                if (!keys.Add(adapted.StableKey))
                {
                    errors.Add(defName + ":duplicate adapted trait " + adapted.StableKey);
                }
            }
        }

        /// <summary>校验隐藏适应能力声明；缺少可选能力时只跳过该项。</summary>
        private static void ValidateAdaptedAbilities(string defName, List<BlackTriggerAdaptedAbility> abilities, List<string> errors)
        {
            if (abilities == null) return;
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < abilities.Count; index++)
            {
                BlackTriggerAdaptedAbility adapted = abilities[index];
                if (adapted == null || !adapted.IsValid)
                {
                    errors.Add(defName + ":adaptedAbilities contains an invalid AbilityDefName");
                    continue;
                }
                if (!keys.Add(adapted.StableKey))
                {
                    errors.Add(defName + ":duplicate adapted ability " + adapted.StableKey);
                }
                // 可选 DLC 或模组缺席时返回空；这不使整个形成定义失效。
                AbilityDef loaded = adapted.ResolveLoadedDef();
                if (loaded != null && !adapted.sourcePackageId.NullOrEmpty()
                    && (loaded.modContentPack == null
                        || !string.Equals(loaded.modContentPack.PackageId, adapted.sourcePackageId, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add(defName + ":adaptedAbilities source package mismatch for " + adapted.abilityDefName);
                }
            }
        }

        /// <summary>校验一组形成事实引用。</summary>
        private static void ValidateFactList(string defName, List<BlackTriggerFactReference> list, List<string> errors, string label)
        {
            if (list == null) return;
            for (int index = 0; index < list.Count; index++)
            {
                BlackTriggerFactReference reference = list[index];
                if (reference == null || !reference.IsValid) errors.Add(defName + ":" + label + " contains an invalid fact reference");
                if (reference?.record != null && !BlackTriggerRecordCatalog.IsAllowed(reference.record)) errors.Add(defName + ":references a disallowed RecordDef " + reference.record.defName);
            }
        }

        /// <summary>校验适格条件组。</summary>
        private static void ValidateEligibility(string defName, List<BlackTriggerEligibilityConditionGroup> groups, List<string> errors)
        {
            if (groups == null) return;
            for (int index = 0; index < groups.Count; index++)
            {
                BlackTriggerEligibilityConditionGroup group = groups[index];
                if (group == null) { errors.Add(defName + ":empty eligibility group"); continue; }
                int count = (group.allOf?.Count ?? 0) + (group.anyOf?.Count ?? 0) + (group.noneOf?.Count ?? 0);
                if (count == 0) errors.Add(defName + ":empty eligibility group");
                int anyCount = group.anyOf?.Count ?? 0;
                if (anyCount == 0 && group.anyOfMinimum > 1) errors.Add(defName + ":eligibility anyOfMinimum exists without anyOf");
                if (anyCount > 0 && (group.anyOfMinimum < 1 || group.anyOfMinimum > anyCount)) errors.Add(defName + ":eligibility anyOfMinimum out of range");
                ValidateEligibilityList(group.allOf, defName, errors);
                ValidateEligibilityList(group.anyOf, defName, errors);
                ValidateEligibilityList(group.noneOf, defName, errors);
            }
        }

        /// <summary>校验适格条件条目不为空。</summary>
        private static void ValidateEligibilityList(List<BlackTriggerEligibilityCondition> list, string defName, List<string> errors)
        {
            if (list == null) return;
            for (int index = 0; index < list.Count; index++)
            {
                BlackTriggerEligibilityCondition condition = list[index];
                if (condition == null) { errors.Add(defName + ":empty eligibility condition"); continue; }
                switch (condition.kind)
                {
                    case BlackTriggerEligibilityConditionKind.SkillInTopThree:
                    case BlackTriggerEligibilityConditionKind.SkillLevelAtLeast:
                    case BlackTriggerEligibilityConditionKind.SkillDisabled:
                        if (condition.skill == null) errors.Add(defName + ":skill eligibility has no SkillDef");
                        break;
                    case BlackTriggerEligibilityConditionKind.TraitDegree:
                        if (condition.trait == null) errors.Add(defName + ":trait eligibility has no TraitDef");
                        break;
                    case BlackTriggerEligibilityConditionKind.ActiveGene:
                        if (condition.gene == null) errors.Add(defName + ":gene eligibility has no GeneDef");
                        break;
                    case BlackTriggerEligibilityConditionKind.RecordNonZero:
                        if (condition.record == null) errors.Add(defName + ":record eligibility has no RecordDef");
                        break;
                    case BlackTriggerEligibilityConditionKind.Custom:
                        if (condition.customWorkerType.NullOrEmpty()) errors.Add(defName + ":custom eligibility has no worker type");
                        break;
                }
            }
        }
    }
}
