using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 第一版默认 Ability 宿主同步器。
    /// 它把正式 Ability 结果同步到原版 AbilityTracker，不把 Ability 真相留在 Trigger 内部。
    /// </summary>
    internal sealed class DefaultExpressionAbilityHostSynchronizer
    {
        /// <summary>
        /// 记录当前会话中由表达系统补入的 Ability 定义。
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> AddedAbilityDefsByPawn =
            new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// 记录当前 Pawn 已发布 AbilityDef 对应的正式表达结果。
        /// Ability 运行时只能从这里读取芯片级参数，不回头读取 AbilityDef 上的 BDP 成本。
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, FormalExpressionResult>> BoundAbilityResultsByPawn =
            new Dictionary<string, Dictionary<string, FormalExpressionResult>>();

        /// <summary>
        /// 同步指定 Pawn 当前成立的 Ability 结果。
        /// </summary>
        public void Sync(Pawn pawn, ExpressionSnapshot snapshot)
        {
            if (pawn?.abilities == null)
            {
                return;
            }

            Dictionary<string, FormalExpressionResult> currentResults = CollectCurrentAbilityResults(snapshot);
            HashSet<string> currentDefNames = new HashSet<string>(
                currentResults.Keys,
                System.StringComparer.OrdinalIgnoreCase);
            HashSet<string> addedDefNames = GetAddedAbilityDefs(pawn, createIfMissing: true);
            HashSet<string> ownedDefNames = CollectOwnedAbilityDefs(pawn, currentDefNames, addedDefNames);

            RefreshBoundAbilityResults(pawn, currentResults);
            RemoveInactiveAbilities(pawn, currentDefNames, ownedDefNames, addedDefNames);
            AddMissingAbilities(pawn, currentDefNames, addedDefNames);
        }

        /// <summary>
        /// 尝试读取指定 Pawn 当前 AbilityDef 绑定到的正式表达结果。
        /// </summary>
        internal static bool TryResolveBoundAbilityResult(
            Pawn pawn,
            AbilityDef abilityDef,
            out FormalExpressionResult result)
        {
            result = null;
            if (pawn == null || abilityDef == null || string.IsNullOrWhiteSpace(abilityDef.defName))
            {
                return false;
            }

            Dictionary<string, FormalExpressionResult> resultsByDefName;
            if (!BoundAbilityResultsByPawn.TryGetValue(pawn.ThingID, out resultsByDefName)
                || resultsByDefName == null)
            {
                return false;
            }

            return resultsByDefName.TryGetValue(abilityDef.defName, out result) && result != null;
        }

        /// <summary>
        /// 追加当前 Ability 结果的发布观察条目。
        /// 这里只记录结果级追踪，不改动宿主同步行为。
        /// </summary>
        internal void AppendPublicationEntries(
            ExpressionSnapshot snapshot,
            List<ExpressionPublicationEntry> entries)
        {
            if (entries == null || snapshot?.Results == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult expressionResult = snapshot.Results[i];
                if (expressionResult == null || expressionResult.ResultKind != ExpressionResultKind.Ability)
                {
                    continue;
                }

                entries.Add(new ExpressionPublicationEntry
                {
                    ResultId = expressionResult.Id,
                    ResultKind = expressionResult.ResultKind,
                    PublishedKey = expressionResult.AbilityDefName,
                    IsPublished = expressionResult.IsAvailable
                        && !string.IsNullOrWhiteSpace(expressionResult.AbilityDefName),
                    SourceResultIds = DefaultExpressionHostSynchronizer.ResolveSourceResultIds(
                        snapshot,
                        expressionResult.Id)
                });
            }
        }

        /// <summary>
        /// 收集当前正式总表中全部应成立的 Ability 定义名。
        /// </summary>
        private static HashSet<string> CollectCurrentAbilityDefs(ExpressionSnapshot snapshot)
        {
            return new HashSet<string>(CollectCurrentAbilityResults(snapshot).Keys);
        }

        /// <summary>
        /// 收集当前正式总表中全部应成立的 Ability 结果。
        /// 原版 AbilityTracker 只能按 AbilityDef 持有单个 Ability，同 Def 多结果时保留第一条稳定结果。
        /// </summary>
        private static Dictionary<string, FormalExpressionResult> CollectCurrentAbilityResults(ExpressionSnapshot snapshot)
        {
            Dictionary<string, FormalExpressionResult> result = new Dictionary<string, FormalExpressionResult>();
            if (snapshot?.Results == null)
            {
                return result;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult expressionResult = snapshot.Results[i];
                if (expressionResult == null
                    || expressionResult.ResultKind != ExpressionResultKind.Ability
                    || !expressionResult.IsAvailable
                    || string.IsNullOrWhiteSpace(expressionResult.AbilityDefName))
                {
                    continue;
                }

                if (!result.ContainsKey(expressionResult.AbilityDefName))
                {
                    result.Add(expressionResult.AbilityDefName, expressionResult);
                }
            }

            return result;
        }

        /// <summary>
        /// 刷新当前 Pawn 的 Ability 表达结果绑定。
        /// </summary>
        private static void RefreshBoundAbilityResults(
            Pawn pawn,
            Dictionary<string, FormalExpressionResult> currentResults)
        {
            if (pawn == null)
            {
                return;
            }

            string pawnKey = pawn.ThingID;
            if (currentResults == null || currentResults.Count == 0)
            {
                BoundAbilityResultsByPawn.Remove(pawnKey);
                return;
            }

            BoundAbilityResultsByPawn[pawnKey] = new Dictionary<string, FormalExpressionResult>(currentResults);
        }

        /// <summary>
        /// 移除当前会话中已由表达系统补入、但现在不再成立的 Ability。
        /// </summary>
        private static void RemoveInactiveAbilities(
            Pawn pawn,
            HashSet<string> currentDefNames,
            HashSet<string> ownedDefNames,
            HashSet<string> addedDefNames)
        {
            List<string> removedDefs = new List<string>();
            foreach (string defName in ownedDefNames)
            {
                if (currentDefNames.Contains(defName))
                {
                    continue;
                }

                AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    removedDefs.Add(defName);
                    continue;
                }

                if (pawn.abilities.GetAbility(def, true) != null)
                {
                    pawn.abilities.RemoveAbility(def);
                }

                removedDefs.Add(defName);
            }

            for (int i = 0; i < removedDefs.Count; i++)
            {
                addedDefNames.Remove(removedDefs[i]);
            }
        }

        /// <summary>
        /// 收集当前同步器应负责对账的 AbilityDef 集合。
        /// 它既包括本局动态补入的 Ability，也包括 Pawn 当前已经持有的表达宿主壳，
        /// 这样旧存档残留或前一轮漏回收的 stale Ability 也会按正式真值被清走。
        /// </summary>
        private static HashSet<string> CollectOwnedAbilityDefs(
            Pawn pawn,
            HashSet<string> currentDefNames,
            HashSet<string> addedDefNames)
        {
            HashSet<string> result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            AppendDefNames(result, currentDefNames);
            AppendDefNames(result, addedDefNames);
            AppendTrackerAbilityDefs(pawn, result);
            return result;
        }

        /// <summary>
        /// 追加一组 AbilityDefName。
        /// </summary>
        private static void AppendDefNames(HashSet<string> target, IEnumerable<string> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (string defName in source)
            {
                if (!string.IsNullOrWhiteSpace(defName))
                {
                    target.Add(defName);
                }
            }
        }

        /// <summary>
        /// 从当前 Pawn 的原版 AbilityTracker 收集表达宿主壳 AbilityDefName。
        /// 这里认的是 AbilityDef 本身的正式宿主特征，而不是“这局是不是刚由同步器加进去”。
        /// </summary>
        private static void AppendTrackerAbilityDefs(Pawn pawn, HashSet<string> target)
        {
            if (pawn?.abilities?.AllAbilitiesForReading == null || target == null)
            {
                return;
            }

            List<Ability> abilities = pawn.abilities.AllAbilitiesForReading;
            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityDef abilityDef = abilities[i]?.def;
                if (!IsExpressionOwnedAbilityDef(abilityDef))
                {
                    continue;
                }

                target.Add(abilityDef.defName);
            }
        }

        /// <summary>
        /// 判断当前 AbilityDef 是否属于表达系统正式 Ability 宿主壳。
        /// 当前识别边界收紧为：
        /// 1. 使用 BDP 正式 Ability Verb。
        /// 2. 带有表达结果扣费组件。
        /// 这样 stale 宿主会被回收，但不会误伤普通原版或其它非表达 Ability。
        /// </summary>
        private static bool IsExpressionOwnedAbilityDef(AbilityDef abilityDef)
        {
            return abilityDef != null
                && !string.IsNullOrWhiteSpace(abilityDef.defName)
                && abilityDef.verbProperties?.verbClass != null
                && typeof(BDP.Core.Abilities.BdpVerb_CastAbility).IsAssignableFrom(
                    abilityDef.verbProperties.verbClass)
                && HasExpressionCostComp(abilityDef);
        }

        /// <summary>
        /// 判断当前 AbilityDef 是否挂了表达结果扣费组件。
        /// </summary>
        private static bool HasExpressionCostComp(AbilityDef abilityDef)
        {
            if (abilityDef?.comps == null)
            {
                return false;
            }

            for (int i = 0; i < abilityDef.comps.Count; i++)
            {
                if (abilityDef.comps[i] is BDP.Core.Abilities.CompProperties_AbilityEffect_BdpTrionCost)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 为当前正式结果补入仍未存在的 Ability 宿主。
        /// </summary>
        private static void AddMissingAbilities(Pawn pawn, HashSet<string> currentDefNames, HashSet<string> addedDefNames)
        {
            foreach (string defName in currentDefNames)
            {
                AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    continue;
                }

                if (pawn.abilities.GetAbility(def, true) != null)
                {
                    continue;
                }

                pawn.abilities.GainAbility(def);
                addedDefNames.Add(defName);
            }
        }

        /// <summary>
        /// 读取当前 Pawn 会话内已补入的 Ability 定义集合。
        /// </summary>
        private static HashSet<string> GetAddedAbilityDefs(Pawn pawn, bool createIfMissing)
        {
            string pawnKey = pawn.ThingID;
            HashSet<string> result;
            if (!AddedAbilityDefsByPawn.TryGetValue(pawnKey, out result) && createIfMissing)
            {
                result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                AddedAbilityDefsByPawn[pawnKey] = result;
            }

            return result;
        }
    }
}
