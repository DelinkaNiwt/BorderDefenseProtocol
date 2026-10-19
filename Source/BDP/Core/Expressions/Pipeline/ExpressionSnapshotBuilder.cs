using System.Collections.Generic;
using BDP.Core.Permissions;
using BDP.Core.Trigger;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达总表构建器。
    /// 它作为流程协调器，串联来源收集、单侧生成、Special 覆写、复合重算与总表装配。
    /// </summary>
    internal sealed class ExpressionSnapshotBuilder
    {
        /// <summary>
        /// 来源材料收集器。
        /// </summary>
        private readonly ExpressionSourceCollector sourceCollector;

        /// <summary>
        /// 单侧结果构建器。
        /// </summary>
        private readonly SingleSideExpressionBuilder singleSideExpressionBuilder;

        /// <summary>
        /// 复合结果解析器。
        /// </summary>
        private readonly CompositeExpressionResolver compositeExpressionResolver;

        /// <summary>
        /// 开放式表达增强解析器。
        /// </summary>
        private readonly ExpressionAugmentationResolver expressionAugmentationResolver;

        /// <summary>
        /// 使用指定依赖构造总表构建器。
        /// </summary>
        public ExpressionSnapshotBuilder(
            IExpressionSourceDeclarationProvider declarationProvider,
            DefaultExpressionConditionEvaluator conditionEvaluator)
        {
            sourceCollector = new ExpressionSourceCollector(declarationProvider, conditionEvaluator);
            singleSideExpressionBuilder = new SingleSideExpressionBuilder();
            compositeExpressionResolver = new CompositeExpressionResolver();
            expressionAugmentationResolver = new ExpressionAugmentationResolver();
        }

        /// <summary>
        /// 为当前 Pawn 构建正式表达总表。
        /// 每次调用都会按现有行为重建内部对象图，本次修正只做结构收拢，不在这里优化分配。
        /// </summary>
        public ExpressionSnapshot Build(Pawn pawn, ITriggerLoadoutReader triggerLoadoutReader)
        {
            long totalStart = BdpDiagnostics.BeginTiming();
            long sourceCollectionStart = BdpDiagnostics.BeginTiming();
            IReadOnlyList<ExpressionSourceMaterial> collected = sourceCollector.Collect(pawn, triggerLoadoutReader);
            BdpDiagnostics.TraceTiming(
                "表达快照构建 | stage=collect_sources"
                + " | pawn=" + (pawn != null ? pawn.ThingID : "null")
                + " | collected=" + (collected != null ? collected.Count.ToString() : "null"),
                sourceCollectionStart);

            long resolveStart = BdpDiagnostics.BeginTiming();
            IReadOnlyList<ExpressionSourceMaterial> filtered = Filter(collected);
            IReadOnlyDictionary<string, ExpressionSourceMaterial> materialIndex = BuildMaterialIndex(filtered);

            SingleSideExpressionSet mainSet = singleSideExpressionBuilder.Build(TriggerSide.Main, filtered);
            SingleSideExpressionSet subSet = singleSideExpressionBuilder.Build(TriggerSide.Sub, filtered);
            SingleSideExpressionSet specialSet = singleSideExpressionBuilder.Build(TriggerSide.Special, filtered);

            SpecialWeaponOverrideResolution specialOverride = ResolveSpecialOverride(mainSet, subSet, specialSet);
            SingleSideExpressionSet resolvedMainSet = specialOverride != null ? specialOverride.MainSet : mainSet;
            SingleSideExpressionSet resolvedSubSet = specialOverride != null ? specialOverride.SubSet : subSet;
            SingleSideExpressionSet resolvedSpecialSet = specialOverride != null ? specialOverride.SpecialSet : specialSet;
            IReadOnlyList<ExpressionAugmentationDeclaration> augmentations =
                expressionAugmentationResolver.Collect(
                    resolvedMainSet,
                    resolvedSubSet,
                    resolvedSpecialSet);
            expressionAugmentationResolver.ApplyModules(
                augmentations,
                resolvedMainSet,
                resolvedSubSet,
                resolvedSpecialSet);
            CompositeExpressionSet compositeSet = compositeExpressionResolver.Resolve(
                pawn,
                resolvedMainSet,
                resolvedSubSet,
                triggerLoadoutReader,
                materialIndex,
                collected);
            expressionAugmentationResolver.ApplyDisplayPrefixes(
                augmentations,
                resolvedMainSet,
                resolvedSubSet,
                resolvedSpecialSet,
                compositeSet);
            BdpDiagnostics.TraceTiming(
                "表达快照构建 | stage=resolve_expression_sets"
                + " | pawn=" + (pawn != null ? pawn.ThingID : "null")
                + " | filtered=" + (filtered != null ? filtered.Count.ToString() : "null")
                + " | augmentations=" + (augmentations != null ? augmentations.Count.ToString() : "null"),
                resolveStart);

            long assembleStart = BdpDiagnostics.BeginTiming();
            ExpressionSnapshot snapshot = Assemble(
                resolvedMainSet,
                resolvedSubSet,
                resolvedSpecialSet,
                compositeSet,
                specialOverride);
            NotifyPermissionReceivers(pawn, snapshot);
            BdpDiagnostics.TraceTiming(
                "表达快照构建 | stage=assemble_and_notify"
                + " | pawn=" + (pawn != null ? pawn.ThingID : "null")
                + " | results=" + (snapshot?.Results != null ? snapshot.Results.Count.ToString() : "null"),
                assembleStart);
            BdpDiagnostics.TraceTiming(
                "表达快照构建 | stage=total | pawn=" + (pawn != null ? pawn.ThingID : "null"),
                totalStart);
            return snapshot;
        }

        /// <summary>
        /// 将当前 Pawn 的全部正式芯片表达结果交给可选业务接收器。
        /// Core 只传递上下文，不解释任何许可效果。
        /// </summary>
        private static void NotifyPermissionReceivers(Pawn pawn, ExpressionSnapshot snapshot)
        {
            if (snapshot?.Results == null)
            {
                return;
            }

            for (int index = 0; index < snapshot.Results.Count; index++)
            {
                FormalExpressionResult result = snapshot.Results[index];
                if (result == null)
                {
                    continue;
                }

                ChipFunctionPermissionReceiverRegistry.Notify(
                    new MapPermissionContext(pawn, result.Id, result.DeclaredPermissionKeys));
            }
        }

        /// <summary>
        /// 过滤来源材料，剔除未启用项。
        /// </summary>
        private static IReadOnlyList<ExpressionSourceMaterial> Filter(IReadOnlyList<ExpressionSourceMaterial> sourceMaterials)
        {
            if (sourceMaterials == null || sourceMaterials.Count == 0)
            {
                return new List<ExpressionSourceMaterial>();
            }

            List<ExpressionSourceMaterial> result = new List<ExpressionSourceMaterial>();
            for (int i = 0; i < sourceMaterials.Count; i++)
            {
                ExpressionSourceMaterial material = sourceMaterials[i];
                if (material == null || !material.IsEnabled)
                {
                    continue;
                }

                result.Add(material);
            }

            return result;
        }

        /// <summary>
        /// 为复合构建阶段建立来源材料索引。
        /// 这里继续以单侧正式结果 Id 为键，避免 combo 反向从正式结果抠内部参数。
        /// </summary>
        private static IReadOnlyDictionary<string, ExpressionSourceMaterial> BuildMaterialIndex(
            IReadOnlyList<ExpressionSourceMaterial> sourceMaterials)
        {
            Dictionary<string, ExpressionSourceMaterial> result = new Dictionary<string, ExpressionSourceMaterial>();
            if (sourceMaterials == null)
            {
                return result;
            }

            for (int i = 0; i < sourceMaterials.Count; i++)
            {
                ExpressionSourceMaterial material = sourceMaterials[i];
                if (material == null
                    || string.IsNullOrWhiteSpace(material.Id)
                    || result.ContainsKey(material.Id))
                {
                    continue;
                }

                result.Add(material.Id, material);
            }

            return result;
        }

        /// <summary>
        /// 按当前规则裁定三侧武器类结果。
        /// </summary>
        private static SpecialWeaponOverrideResolution ResolveSpecialOverride(
            SingleSideExpressionSet mainSet,
            SingleSideExpressionSet subSet,
            SingleSideExpressionSet specialSet)
        {
            bool hasSpecialWeapon = specialSet != null
                && specialSet.WeaponResults != null
                && specialSet.WeaponResults.Count > 0;

            if (!hasSpecialWeapon)
            {
                return new SpecialWeaponOverrideResolution
                {
                    MainSet = mainSet,
                    SubSet = subSet,
                    SpecialSet = specialSet,
                    HasSpecialWeaponOverride = false
                };
            }

            return new SpecialWeaponOverrideResolution
            {
                MainSet = ReplaceWeaponResults(mainSet, new List<FormalExpressionResult>()),
                SubSet = ReplaceWeaponResults(subSet, new List<FormalExpressionResult>()),
                SpecialSet = ReplaceWeaponResults(specialSet, KeepSingleWeaponResult(specialSet)),
                HasSpecialWeaponOverride = true
            };
        }

        /// <summary>
        /// 装配当前完整表达结果总表。
        /// </summary>
        private static ExpressionSnapshot Assemble(
            SingleSideExpressionSet mainSet,
            SingleSideExpressionSet subSet,
            SingleSideExpressionSet specialSet,
            CompositeExpressionSet compositeSet,
            SpecialWeaponOverrideResolution specialOverride)
        {
            List<FormalExpressionResult> results = new List<FormalExpressionResult>();
            Append(results, mainSet);
            Append(results, subSet);
            Append(results, specialSet);
            Append(results, compositeSet != null ? compositeSet.DualWeaponResults : null);
            Append(results, compositeSet != null ? compositeSet.ComboResults : null);
            Append(results, compositeSet != null ? compositeSet.NonCombatCompositeResults : null);

            return new ExpressionSnapshot
            {
                Results = results,
                CompositeReferences = compositeSet != null ? compositeSet.References : new List<CompositeExpressionReference>(),
                HasSpecialWeaponOverride = specialOverride != null && specialOverride.HasSpecialWeaponOverride
            };
        }

        /// <summary>
        /// 保留 Special 当前第一条武器类结果，丢弃其余武器类结果。
        /// </summary>
        private static IReadOnlyList<FormalExpressionResult> KeepSingleWeaponResult(SingleSideExpressionSet set)
        {
            List<FormalExpressionResult> result = new List<FormalExpressionResult>();
            if (set == null || set.WeaponResults == null || set.WeaponResults.Count == 0)
            {
                return result;
            }

            result.Add(set.WeaponResults[0]);
            return result;
        }

        /// <summary>
        /// 在不碰非武器类结果的前提下，替换当前侧武器类结果集合。
        /// </summary>
        private static SingleSideExpressionSet ReplaceWeaponResults(
            SingleSideExpressionSet set,
            IReadOnlyList<FormalExpressionResult> newWeaponResults)
        {
            if (set == null)
            {
                return null;
            }

            List<FormalExpressionResult> mergedResults = new List<FormalExpressionResult>();
            if (set.Results != null)
            {
                for (int i = 0; i < set.Results.Count; i++)
                {
                    FormalExpressionResult result = set.Results[i];
                    if (result == null || result.OutputChannel == ExpressionOutputChannel.Verb)
                    {
                        continue;
                    }

                    mergedResults.Add(result);
                }
            }

            if (newWeaponResults != null)
            {
                for (int i = 0; i < newWeaponResults.Count; i++)
                {
                    if (newWeaponResults[i] != null)
                    {
                        mergedResults.Add(newWeaponResults[i]);
                    }
                }
            }

            return new SingleSideExpressionSet
            {
                Side = set.Side,
                Results = mergedResults,
                WeaponResults = newWeaponResults ?? new List<FormalExpressionResult>()
            };
        }

        /// <summary>
        /// 追加单侧结果集合。
        /// </summary>
        private static void Append(List<FormalExpressionResult> target, SingleSideExpressionSet set)
        {
            Append(target, set != null ? set.Results : null);
        }

        /// <summary>
        /// 追加结果列表。
        /// </summary>
        private static void Append(List<FormalExpressionResult> target, IReadOnlyList<FormalExpressionResult> results)
        {
            if (results == null)
            {
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] != null)
                {
                    target.Add(results[i]);
                }
            }
        }
    }
}
