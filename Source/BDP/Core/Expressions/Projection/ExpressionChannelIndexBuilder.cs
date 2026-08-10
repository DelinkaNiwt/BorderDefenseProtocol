using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 四类表达并联索引构建器。
    /// 它只从已成立的正式结果总表建立读取索引，不参与任何结果裁定。
    /// </summary>
    internal static class ExpressionChannelIndexBuilder
    {
        /// <summary>
        /// 为指定快照建立四类并联索引。
        /// </summary>
        internal static ExpressionChannelIndex Build(ExpressionSnapshot snapshot)
        {
            if (snapshot?.Results == null || snapshot.Results.Count == 0)
            {
                return ExpressionChannelIndex.Empty();
            }

            List<FormalExpressionResult> allResults = new List<FormalExpressionResult>();
            List<FormalExpressionResult> verbResults = new List<FormalExpressionResult>();
            List<FormalExpressionResult> abilityResults = new List<FormalExpressionResult>();
            List<FormalExpressionResult> hediffResults = new List<FormalExpressionResult>();
            List<FormalExpressionResult> passiveResults = new List<FormalExpressionResult>();
            Dictionary<string, List<FormalExpressionResult>> abilityResultsByDefName =
                new Dictionary<string, List<FormalExpressionResult>>();
            Dictionary<string, List<FormalExpressionResult>> hediffResultsByDefName =
                new Dictionary<string, List<FormalExpressionResult>>();
            Dictionary<string, List<FormalExpressionResult>> passiveResultsByKey =
                new Dictionary<string, List<FormalExpressionResult>>();

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult result = snapshot.Results[i];
                if (result == null)
                {
                    continue;
                }

                allResults.Add(result);

                // Ability 必须保留给原版按钮宿主；其它自动消费通道只收录当前可使用结果。
                bool useAllowed = result.UseRequirementCheck == null
                    || result.UseRequirementCheck.Satisfied;
                if (!useAllowed && result.ResultKind != ExpressionResultKind.Ability)
                {
                    continue;
                }

                switch (result.ResultKind)
                {
                    case ExpressionResultKind.Verb:
                        verbResults.Add(result);
                        break;
                    case ExpressionResultKind.Ability:
                        abilityResults.Add(result);
                        AddToLookup(abilityResultsByDefName, result.AbilityDefName, result);
                        break;
                    case ExpressionResultKind.Hediff:
                        hediffResults.Add(result);
                        AddToLookup(hediffResultsByDefName, result.HediffDefName, result);
                        break;
                    case ExpressionResultKind.Passive:
                        passiveResults.Add(result);
                        AddToLookup(passiveResultsByKey, result.PassiveKey, result);
                        break;
                }
            }

            return new ExpressionChannelIndex
            {
                AllResults = allResults,
                VerbResults = verbResults,
                AbilityResults = abilityResults,
                HediffResults = hediffResults,
                PassiveResults = passiveResults,
                AbilityResultsByDefName = FreezeLookup(abilityResultsByDefName),
                HediffResultsByDefName = FreezeLookup(hediffResultsByDefName),
                PassiveResultsByKey = FreezeLookup(passiveResultsByKey)
            };
        }

        /// <summary>
        /// 把单条结果按键追加到索引表。
        /// </summary>
        private static void AddToLookup(
            Dictionary<string, List<FormalExpressionResult>> lookup,
            string key,
            FormalExpressionResult result)
        {
            if (lookup == null || result == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            List<FormalExpressionResult> bucket;
            if (!lookup.TryGetValue(key, out bucket))
            {
                bucket = new List<FormalExpressionResult>();
                lookup.Add(key, bucket);
            }

            bucket.Add(result);
        }

        /// <summary>
        /// 把可写查找表冻结成只读接口形态。
        /// </summary>
        private static IReadOnlyDictionary<string, IReadOnlyList<FormalExpressionResult>> FreezeLookup(
            Dictionary<string, List<FormalExpressionResult>> source)
        {
            Dictionary<string, IReadOnlyList<FormalExpressionResult>> result =
                new Dictionary<string, IReadOnlyList<FormalExpressionResult>>();
            if (source == null)
            {
                return result;
            }

            foreach (KeyValuePair<string, List<FormalExpressionResult>> pair in source)
            {
                result.Add(pair.Key, pair.Value ?? new List<FormalExpressionResult>());
            }

            return result;
        }
    }
}
