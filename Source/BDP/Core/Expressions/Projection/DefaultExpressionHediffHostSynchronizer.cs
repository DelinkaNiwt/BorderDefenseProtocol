using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达系统默认 Hediff 宿主同步器。
    /// 它只负责把正式 Hediff 结果同步到原版 Hediff 宿主，不把副作用回写到 Trigger。
    /// </summary>
    internal sealed class DefaultExpressionHediffHostSynchronizer
    {
        /// <summary>
        /// 当前 Pawn 会话里已经发布到原版宿主的表达 HediffDef 集合。
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> PublishedHediffDefsByPawn =
            new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// 正式“结果数量映射强度”应用方式键。
        /// </summary>
        private const string CountToSeverityApplyModeKey = "countToSeverity";

        /// <summary>
        /// 同步指定 Pawn 当前成立的 Hediff 结果。
        /// </summary>
        public void Sync(Pawn pawn, ExpressionSnapshot snapshot)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            Dictionary<string, HediffExpressionDemand> demands = CollectDemands(snapshot);
            HashSet<string> manualControlledDefs = CollectManualControlledDefs(snapshot);
            HashSet<string> publishedDefs = GetPublishedHediffDefs(pawn, createIfMissing: true);

            RemoveInactiveHediffs(pawn, demands, manualControlledDefs, publishedDefs);
            ApplyCurrentHediffs(pawn, demands, publishedDefs);
            SyncExistingManualHediffs(pawn, snapshot, manualControlledDefs, publishedDefs);
        }

        /// <summary>
        /// 追加当前 Hediff 结果的发布观察条目。
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
                if (expressionResult == null || expressionResult.OutputChannel != ExpressionOutputChannel.Hediff)
                {
                    continue;
                }

                entries.Add(new ExpressionPublicationEntry
                {
                    ResultId = expressionResult.Id,
                    OutputChannel = expressionResult.OutputChannel,
                    PublishedKey = expressionResult.HediffDefName,
                    IsPublished = expressionResult.IsAvailable
                        && (expressionResult.UseRequirementCheck == null
                            || expressionResult.UseRequirementCheck.Satisfied)
                        && !string.IsNullOrWhiteSpace(expressionResult.HediffDefName),
                    SourceResultIds = DefaultExpressionHostSynchronizer.ResolveSourceResultIds(
                        snapshot,
                        expressionResult.Id)
                });
            }
        }

        /// <summary>
        /// 收集当前正式总表里各 Hediff 的最小宿主需求。
        /// </summary>
        private static Dictionary<string, HediffExpressionDemand> CollectDemands(ExpressionSnapshot snapshot)
        {
            Dictionary<string, HediffExpressionDemand> result =
                new Dictionary<string, HediffExpressionDemand>(StringComparer.OrdinalIgnoreCase);
            if (snapshot?.Results == null)
            {
                return result;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult expressionResult = snapshot.Results[i];
                if (expressionResult == null
                    || expressionResult.OutputChannel != ExpressionOutputChannel.Hediff
                    || !expressionResult.IsAvailable
                    || (expressionResult.UseRequirementCheck != null
                        && !expressionResult.UseRequirementCheck.Satisfied)
                    || string.IsNullOrWhiteSpace(expressionResult.HediffDefName))
                {
                    continue;
                }

                if (HediffControlModeKeys.IsAbilityButton(expressionResult.HediffControlMode))
                {
                    continue;
                }

                HediffExpressionDemand demand;
                if (!result.TryGetValue(expressionResult.HediffDefName, out demand))
                {
                    demand = new HediffExpressionDemand
                    {
                        DefName = expressionResult.HediffDefName,
                        HediffApplyModeKey = expressionResult.HediffApplyModeKey,
                        ResultCount = 0,
                        Results = new List<FormalExpressionResult>()
                    };
                    result[expressionResult.HediffDefName] = demand;
                }

                demand.ResultCount++;
                demand.Results.Add(expressionResult);
            }

            return result;
        }

        /// <summary>
        /// 收集由 Ability 按钮控制的 HediffDef。
        /// 这类结果只允许同步器维护已存在宿主的绑定，不允许同步器主动添加 Hediff。
        /// </summary>
        private static HashSet<string> CollectManualControlledDefs(ExpressionSnapshot snapshot)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (snapshot?.Results == null)
            {
                return result;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult expressionResult = snapshot.Results[i];
                if (expressionResult == null
                    || expressionResult.OutputChannel != ExpressionOutputChannel.Hediff
                    || !expressionResult.IsAvailable
                    || (expressionResult.UseRequirementCheck != null
                        && !expressionResult.UseRequirementCheck.Satisfied)
                    || string.IsNullOrWhiteSpace(expressionResult.HediffDefName)
                    || !HediffControlModeKeys.IsAbilityButton(expressionResult.HediffControlMode))
                {
                    continue;
                }

                result.Add(expressionResult.HediffDefName);
            }

            return result;
        }

        /// <summary>
        /// 移除当前不再成立的表达宿主 Hediff。
        /// </summary>
        private static void RemoveInactiveHediffs(
            Pawn pawn,
            Dictionary<string, HediffExpressionDemand> demands,
            HashSet<string> manualControlledDefs,
            HashSet<string> publishedDefs)
        {
            List<string> removedDefs = new List<string>();
            foreach (string defName in publishedDefs)
            {
                if (demands.ContainsKey(defName) || manualControlledDefs.Contains(defName))
                {
                    continue;
                }

                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                if (IsExpressionHostHediffDef(def))
                {
                    RemoveAllHediffsOfDef(pawn, def);
                }

                removedDefs.Add(defName);
            }

            for (int i = 0; i < removedDefs.Count; i++)
            {
                publishedDefs.Remove(removedDefs[i]);
            }

            // 按钮新增的 Hediff 可能尚未进入本会话发布集合，仍需在芯片停用后清理。
            List<Hediff> expressionHediffs = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs(
                ref expressionHediffs,
                hediff => hediff is BdpExpressionHostHediff);
            for (int i = 0; i < expressionHediffs.Count; i++)
            {
                Hediff hediff = expressionHediffs[i];
                string defName = hediff?.def?.defName;
                if (string.IsNullOrWhiteSpace(defName)
                    || demands.ContainsKey(defName)
                    || manualControlledDefs.Contains(defName))
                {
                    continue;
                }

                pawn.health.RemoveHediff(hediff);
                publishedDefs.Remove(defName);
            }
        }

        /// <summary>
        /// 应用当前仍然成立的表达宿主 Hediff。
        /// </summary>
        private static void ApplyCurrentHediffs(
            Pawn pawn,
            Dictionary<string, HediffExpressionDemand> demands,
            HashSet<string> publishedDefs)
        {
            foreach (KeyValuePair<string, HediffExpressionDemand> pair in demands)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(pair.Key);
                if (def == null)
                {
                    continue;
                }

                if (!IsExpressionHostHediffDef(def))
                {
                    Log.Error("[BDP] Hediff expression host sync rejected non-expression host HediffDef: " + pair.Key);
                    continue;
                }

                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def, false);
                if (hediff == null)
                {
                    hediff = pawn.health.AddHediff(def);
                }

                if (hediff != null
                    && string.Equals(
                        pair.Value.HediffApplyModeKey,
                        CountToSeverityApplyModeKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    hediff.Severity = Math.Max(1, pair.Value.ResultCount);
                }

                BdpExpressionHostHediff hostHediff = hediff as BdpExpressionHostHediff;
                if (hostHediff != null)
                {
                    hostHediff.SyncExpressionResults(pair.Value.Results);
                }

                publishedDefs.Add(pair.Key);
            }
        }

        /// <summary>
        /// 同步按钮已经打开的 Hediff 宿主绑定，但不负责创建宿主。
        /// </summary>
        private static void SyncExistingManualHediffs(
            Pawn pawn,
            ExpressionSnapshot snapshot,
            HashSet<string> manualControlledDefs,
            HashSet<string> publishedDefs)
        {
            foreach (string defName in manualControlledDefs)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                if (!IsExpressionHostHediffDef(def))
                {
                    continue;
                }

                BdpExpressionHostHediff hostHediff =
                    pawn.health.hediffSet.GetFirstHediffOfDef(def, false) as BdpExpressionHostHediff;
                if (hostHediff == null)
                {
                    continue;
                }

                hostHediff.SyncExpressionResults(CollectActiveResultsForDef(snapshot, defName));
                publishedDefs.Add(defName);
            }
        }

        /// <summary>
        /// 收集指定 HediffDef 当前仍有效的全部正式结果。
        /// </summary>
        private static List<FormalExpressionResult> CollectActiveResultsForDef(
            ExpressionSnapshot snapshot,
            string defName)
        {
            List<FormalExpressionResult> result = new List<FormalExpressionResult>();
            if (snapshot?.Results == null || string.IsNullOrWhiteSpace(defName))
            {
                return result;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult expressionResult = snapshot.Results[i];
                if (expressionResult == null
                    || expressionResult.OutputChannel != ExpressionOutputChannel.Hediff
                    || !expressionResult.IsAvailable
                    || (expressionResult.UseRequirementCheck != null
                        && !expressionResult.UseRequirementCheck.Satisfied)
                    || !string.Equals(expressionResult.HediffDefName, defName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(expressionResult);
            }

            return result;
        }

        /// <summary>
        /// 移除指定 Pawn 身上全部同 Def 的表达宿主 Hediff。
        /// </summary>
        private static void RemoveAllHediffsOfDef(Pawn pawn, HediffDef def)
        {
            List<Hediff> removed = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs(ref removed, hediff => hediff != null && hediff.def == def);
            for (int i = 0; i < removed.Count; i++)
            {
                pawn.health.RemoveHediff(removed[i]);
            }
        }

        /// <summary>
        /// 读取当前 Pawn 会话内已经发布到原版宿主的表达 HediffDef 集合。
        /// </summary>
        private static HashSet<string> GetPublishedHediffDefs(Pawn pawn, bool createIfMissing)
        {
            string pawnKey = pawn.ThingID;
            HashSet<string> result;
            if (!PublishedHediffDefsByPawn.TryGetValue(pawnKey, out result) && createIfMissing)
            {
                result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                PublishedHediffDefsByPawn[pawnKey] = result;
            }

            return result;
        }

        /// <summary>
        /// 判断当前 HediffDef 是否属于 BDP 表达宿主 Def。
        /// 正式边界看 hediffClass，不再看命名前缀。
        /// </summary>
        private static bool IsExpressionHostHediffDef(HediffDef def)
        {
            Type hediffClass = def != null ? def.hediffClass : null;
            return hediffClass != null
                && typeof(BdpExpressionHostHediff).IsAssignableFrom(hediffClass);
        }

        /// <summary>
        /// 一条 Hediff 宿主最小需求。
        /// </summary>
        private sealed class HediffExpressionDemand
        {
            /// <summary>
            /// Hediff 定义名。
            /// </summary>
            public string DefName;

            /// <summary>
            /// 当前 Hediff 应用方式键。
            /// </summary>
            public string HediffApplyModeKey;

            /// <summary>
            /// 当前正式结果数量。
            /// </summary>
            public int ResultCount;

            /// <summary>
            /// 当前 Hediff 宿主需要绑定的正式表达结果集合。
            /// </summary>
            public List<FormalExpressionResult> Results;
        }
    }
}
