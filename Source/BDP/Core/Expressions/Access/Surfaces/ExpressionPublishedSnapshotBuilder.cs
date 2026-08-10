using System.Collections.Generic;
using BDP.Core.Trigger.Runtime;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 公开表达投影快照构建器。
    /// 它只负责把内部已发布投影翻译成对外稳定 DTO，不参与表达裁定与宿主同步。
    /// </summary>
    internal static class ExpressionPublishedSnapshotBuilder
    {
        /// <summary>
        /// 从内部已发布战斗投影构建一份公开快照。
        /// </summary>
        internal static ExpressionPublishedProjectionSnapshot Build(TriggerCombatProjectionState projection)
        {
            if (projection == null || projection.IsEmpty)
            {
                return ExpressionPublishedProjectionSnapshot.Empty();
            }

            IReadOnlyDictionary<string, ExpressionPublishedCompositeReference> compositeReferenceIndex =
                BuildCompositeReferenceIndex(projection);
            List<ExpressionPublishedResultSnapshot> allResults = new List<ExpressionPublishedResultSnapshot>();
            List<ExpressionPublishedResultSnapshot> verbResults = new List<ExpressionPublishedResultSnapshot>();
            List<ExpressionPublishedResultSnapshot> abilityResults = new List<ExpressionPublishedResultSnapshot>();
            List<ExpressionPublishedResultSnapshot> hediffResults = new List<ExpressionPublishedResultSnapshot>();
            List<ExpressionPublishedResultSnapshot> passiveResults = new List<ExpressionPublishedResultSnapshot>();
            Dictionary<string, ExpressionPublishedResultSnapshot> resultIndex =
                new Dictionary<string, ExpressionPublishedResultSnapshot>();
            Dictionary<string, List<ExpressionPublishedResultSnapshot>> verbResultsBySlotKey =
                new Dictionary<string, List<ExpressionPublishedResultSnapshot>>();
            Dictionary<string, List<ExpressionPublishedResultSnapshot>> abilityResultsByDefName =
                new Dictionary<string, List<ExpressionPublishedResultSnapshot>>();
            Dictionary<string, List<ExpressionPublishedResultSnapshot>> hediffResultsByDefName =
                new Dictionary<string, List<ExpressionPublishedResultSnapshot>>();
            Dictionary<string, List<ExpressionPublishedResultSnapshot>> passiveResultsByKey =
                new Dictionary<string, List<ExpressionPublishedResultSnapshot>>();

            IReadOnlyList<FormalExpressionResult> publishedResults = projection.Snapshot != null
                ? projection.Snapshot.Results
                : null;
            if (publishedResults != null)
            {
                for (int index = 0; index < publishedResults.Count; index++)
                {
                    FormalExpressionResult result = publishedResults[index];
                    if (result == null)
                    {
                        continue;
                    }

                    compositeReferenceIndex.TryGetValue(
                        result.Id ?? string.Empty,
                        out ExpressionPublishedCompositeReference compositeReference);
                    ExpressionPublishedResultSnapshot snapshot = BuildResultSnapshot(result, compositeReference);
                    allResults.Add(snapshot);

                    if (!string.IsNullOrWhiteSpace(snapshot.ResultId) && !resultIndex.ContainsKey(snapshot.ResultId))
                    {
                        resultIndex.Add(snapshot.ResultId, snapshot);
                    }

                    if (!snapshot.IsPublished)
                    {
                        continue;
                    }

                    switch (snapshot.ChannelKind)
                    {
                        case ExpressionPublishedChannelKind.Verb:
                            verbResults.Add(snapshot);
                            AddToLookup(verbResultsBySlotKey, snapshot.ExecutionSlotKey, snapshot);
                            break;
                        case ExpressionPublishedChannelKind.Ability:
                            abilityResults.Add(snapshot);
                            AddToLookup(abilityResultsByDefName, snapshot.AbilityDefName, snapshot);
                            break;
                        case ExpressionPublishedChannelKind.Hediff:
                            hediffResults.Add(snapshot);
                            AddToLookup(hediffResultsByDefName, snapshot.HediffDefName, snapshot);
                            break;
                        case ExpressionPublishedChannelKind.Passive:
                            passiveResults.Add(snapshot);
                            AddToLookup(passiveResultsByKey, snapshot.PassiveKey, snapshot);
                            break;
                    }
                }
            }

            return new ExpressionPublishedProjectionSnapshot
            {
                ProjectionVersion = projection.ProjectionVersion,
                PrimaryRangedResultId = projection.Snapshot?.PrimaryRanged?.Id,
                PrimaryMeleeResultId = projection.Snapshot?.PrimaryMelee?.Id,
                CurrentExecutingResultId = projection.Snapshot?.CurrentExecuting?.Id,
                HasSpecialWeaponOverride = projection.Snapshot != null && projection.Snapshot.HasSpecialWeaponOverride,
                Results = allResults,
                VerbResults = verbResults,
                AbilityResults = abilityResults,
                HediffResults = hediffResults,
                PassiveResults = passiveResults,
                ResultIndex = resultIndex,
                VerbResultsBySlotKey = FreezeLookup(verbResultsBySlotKey),
                AbilityResultsByDefName = FreezeLookup(abilityResultsByDefName),
                HediffResultsByDefName = FreezeLookup(hediffResultsByDefName),
                PassiveResultsByKey = FreezeLookup(passiveResultsByKey),
                CompositeReferenceIndex = compositeReferenceIndex
            };
        }

        /// <summary>
        /// 构建一条公开结果快照。
        /// </summary>
        private static ExpressionPublishedResultSnapshot BuildResultSnapshot(
            FormalExpressionResult result,
            ExpressionPublishedCompositeReference compositeReference)
        {
            return new ExpressionPublishedResultSnapshot
            {
                ResultId = result.Id,
                ChannelKind = MapChannel(result.ResultKind),
                WeaponModeKey = result.WeaponMode.ToString(),
                OriginKindKey = result.OriginKind.ToString(),
                CompositeKindKey = result.CompositeKind.ToString(),
                ComboDefName = result.ComboDefName,
                DisplayLabel = result.DisplayLabel,
                PublishedKey = ResolvePublishedKey(result),
                ExecutionSlotKey = result.ExecutionSlotKey,
                AbilityDefName = result.AbilityDefName,
                HediffDefName = result.HediffDefName,
                HediffApplyModeKey = result.HediffApplyModeKey,
                PassiveKey = result.PassiveKey,
                RoleKey = result.RoleKey,
                VerbAttackRole = result.VerbAttackRole,
                Tags = CloneStrings(result.Tags),
                ModeKey = result.ModeKey,
                IsSecondaryAttack = result.IsSecondaryAttack,
                IsAvailable = result.IsAvailable,
                CanProject = result.CanProject,
                IsPublished = ResolveIsPublished(result),
                SourceReference = CloneSourceReference(result.SourceReference),
                SourceResultIds = compositeReference != null
                    ? CloneStrings(compositeReference.SourceResultIds)
                    : new List<string>(),
                MainSourceResultId = compositeReference != null ? compositeReference.MainSourceResultId : null,
                SubSourceResultId = compositeReference != null ? compositeReference.SubSourceResultId : null,
                TrionUseCost = result.Trion != null ? result.Trion.UseCost : 0f,
                TrionMinimumRequired = result.Trion != null ? result.Trion.MinimumRequired : 0f,
                TrionSustainCostBySourceCount = CloneSustainCostBySourceCount(
                    result.Trion != null ? result.Trion.SustainCostBySourceCount : null),
                ExposedData = CloneExposedData(result.ExposedData)
            };
        }

        /// <summary>
        /// 把内部来源引用转换为公开快照，避免内部对象跨程序集泄漏。
        /// </summary>
        private static ExpressionPublishedSourceReference CloneSourceReference(
            ExpressionSourceReference source)
        {
            if (source == null)
            {
                return null;
            }

            return new ExpressionPublishedSourceReference
            {
                ChipThingId = source.ChipThingId,
                ChipDefName = source.ChipDefName,
                Side = source.Side,
                SlotIndex = source.SlotIndex
            };
        }

        /// <summary>
        /// 为公开快照深复制表达持续费用档位。
        /// </summary>
        private static IReadOnlyList<ExpressionSustainCostBySourceCountConfig> CloneSustainCostBySourceCount(
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> source)
        {
            List<ExpressionSustainCostBySourceCountConfig> result =
                new List<ExpressionSustainCostBySourceCountConfig>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ExpressionSustainCostBySourceCountConfig row = source[i];
                if (row == null)
                {
                    continue;
                }

                result.Add(new ExpressionSustainCostBySourceCountConfig
                {
                    SourceCount = row.SourceCount,
                    TotalPerSecond = row.TotalPerSecond
                });
            }

            return result;
        }

        /// <summary>
        /// 构建公开复合引用索引。
        /// </summary>
        private static IReadOnlyDictionary<string, ExpressionPublishedCompositeReference> BuildCompositeReferenceIndex(
            TriggerCombatProjectionState projection)
        {
            Dictionary<string, ExpressionPublishedCompositeReference> result =
                new Dictionary<string, ExpressionPublishedCompositeReference>();
            IReadOnlyDictionary<string, CompositeExpressionReference> source = projection.CompositeReferenceIndex;
            if (source == null)
            {
                return result;
            }

            foreach (KeyValuePair<string, CompositeExpressionReference> pair in source)
            {
                CompositeExpressionReference reference = pair.Value;
                if (reference == null || string.IsNullOrWhiteSpace(pair.Key) || result.ContainsKey(pair.Key))
                {
                    continue;
                }

                result.Add(pair.Key, new ExpressionPublishedCompositeReference
                {
                    CompositeId = reference.CompositeId,
                    CompositeKindKey = reference.CompositeKind.ToString(),
                    SourceResultIds = CloneStrings(reference.SourceResultIds),
                    MainSourceResultId = reference.MainSourceResultId,
                    SubSourceResultId = reference.SubSourceResultId
                });
            }

            return result;
        }

        /// <summary>
        /// 把一条结果追加到分组索引表。
        /// </summary>
        private static void AddToLookup(
            Dictionary<string, List<ExpressionPublishedResultSnapshot>> lookup,
            string key,
            ExpressionPublishedResultSnapshot result)
        {
            if (lookup == null || result == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            List<ExpressionPublishedResultSnapshot> bucket;
            if (!lookup.TryGetValue(key, out bucket))
            {
                bucket = new List<ExpressionPublishedResultSnapshot>();
                lookup.Add(key, bucket);
            }

            bucket.Add(result);
        }

        /// <summary>
        /// 冻结一张按键分组的可写表。
        /// </summary>
        private static IReadOnlyDictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>> FreezeLookup(
            Dictionary<string, List<ExpressionPublishedResultSnapshot>> source)
        {
            Dictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>> result =
                new Dictionary<string, IReadOnlyList<ExpressionPublishedResultSnapshot>>();
            if (source == null)
            {
                return result;
            }

            foreach (KeyValuePair<string, List<ExpressionPublishedResultSnapshot>> pair in source)
            {
                result.Add(pair.Key, pair.Value ?? new List<ExpressionPublishedResultSnapshot>());
            }

            return result;
        }

        /// <summary>
        /// 克隆一组字符串列表。
        /// </summary>
        private static IReadOnlyList<string> CloneStrings(IReadOnlyList<string> source)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                string item = source[index];
                if (!string.IsNullOrWhiteSpace(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// 克隆一组公开暴露数据。
        /// </summary>
        private static IReadOnlyList<ExpressionPublishedDatum> CloneExposedData(
            IReadOnlyList<PassiveExpressionExposedDatum> source)
        {
            List<ExpressionPublishedDatum> result = new List<ExpressionPublishedDatum>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                PassiveExpressionExposedDatum datum = source[index];
                if (datum == null)
                {
                    continue;
                }

                result.Add(new ExpressionPublishedDatum
                {
                    Key = datum.Key,
                    Value = datum.Value
                });
            }

            return result;
        }

        /// <summary>
        /// 把内部结果类别映射成公开通道类别。
        /// </summary>
        private static ExpressionPublishedChannelKind MapChannel(ExpressionResultKind resultKind)
        {
            switch (resultKind)
            {
                case ExpressionResultKind.Ability:
                    return ExpressionPublishedChannelKind.Ability;
                case ExpressionResultKind.Hediff:
                    return ExpressionPublishedChannelKind.Hediff;
                case ExpressionResultKind.Passive:
                    return ExpressionPublishedChannelKind.Passive;
                default:
                    return ExpressionPublishedChannelKind.Verb;
            }
        }

        /// <summary>
        /// 解析当前结果在公开通道上的稳定发布键。
        /// </summary>
        private static string ResolvePublishedKey(FormalExpressionResult result)
        {
            if (result == null)
            {
                return null;
            }

            switch (result.ResultKind)
            {
                case ExpressionResultKind.Ability:
                    return result.AbilityDefName;
                case ExpressionResultKind.Hediff:
                    return result.HediffDefName;
                case ExpressionResultKind.Passive:
                    return result.PassiveKey;
                default:
                    return result.ExecutionSlotKey;
            }
        }

        /// <summary>
        /// 判断一条结果是否已经满足最小公开发布条件。
        /// </summary>
        private static bool ResolveIsPublished(FormalExpressionResult result)
        {
            if (result == null || !result.IsAvailable)
            {
                return false;
            }

            // Ability 按钮即使受阻也必须保留；其它通道不向自动消费者发布受阻 Combo。
            if (result.ResultKind != ExpressionResultKind.Ability
                && result.UseRequirementCheck != null
                && !result.UseRequirementCheck.Satisfied)
            {
                return false;
            }

            switch (result.ResultKind)
            {
                case ExpressionResultKind.Ability:
                    return !string.IsNullOrWhiteSpace(result.AbilityDefName);
                case ExpressionResultKind.Hediff:
                    return !string.IsNullOrWhiteSpace(result.HediffDefName);
                case ExpressionResultKind.Passive:
                    return !string.IsNullOrWhiteSpace(result.PassiveKey);
                default:
                    return result.CanProject && !string.IsNullOrWhiteSpace(result.ExecutionSlotKey);
            }
        }
    }
}
