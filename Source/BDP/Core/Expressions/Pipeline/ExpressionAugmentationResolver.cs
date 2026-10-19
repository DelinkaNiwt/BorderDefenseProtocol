using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Trigger;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 开放式表达增强解析器。
    /// 它把激活被动结果发布的增强按远程能力应用到其它正式结果，完全不绑定目标芯片身份。
    /// </summary>
    internal sealed class ExpressionAugmentationResolver
    {
        /// <summary>
        /// 从三侧最终单侧结果中收集当前有效的被动增强声明。
        /// </summary>
        internal IReadOnlyList<ExpressionAugmentationDeclaration> Collect(
            SingleSideExpressionSet mainSet,
            SingleSideExpressionSet subSet,
            SingleSideExpressionSet specialSet)
        {
            List<ExpressionAugmentationDeclaration> result =
                new List<ExpressionAugmentationDeclaration>();
            int order = 0;
            CollectFromSet(mainSet, result, ref order);
            CollectFromSet(subSet, result, ref order);
            CollectFromSet(specialSet, result, ref order);
            return result;
        }

        /// <summary>
        /// 在复合解析前向所有有效单侧远程结果追加模块。
        /// </summary>
        internal void ApplyModules(
            IReadOnlyList<ExpressionAugmentationDeclaration> augmentations,
            SingleSideExpressionSet mainSet,
            SingleSideExpressionSet subSet,
            SingleSideExpressionSet specialSet)
        {
            ApplyModulesToSet(augmentations, mainSet);
            ApplyModulesToSet(augmentations, subSet);
            ApplyModulesToSet(augmentations, specialSet);
        }

        /// <summary>
        /// 在复合结果形成后向最终可见远程结果追加名称前缀。
        /// </summary>
        internal void ApplyDisplayPrefixes(
            IReadOnlyList<ExpressionAugmentationDeclaration> augmentations,
            SingleSideExpressionSet mainSet,
            SingleSideExpressionSet subSet,
            SingleSideExpressionSet specialSet,
            CompositeExpressionSet compositeSet)
        {
            ApplyDisplayPrefixesToSet(augmentations, mainSet);
            ApplyDisplayPrefixesToSet(augmentations, subSet);
            ApplyDisplayPrefixesToSet(augmentations, specialSet);
            List<FormalExpressionResult> sourceResults = BuildSourceResults(
                mainSet,
                subSet,
                specialSet);
            ApplyDisplayPrefixesToResults(
                augmentations,
                compositeSet != null ? compositeSet.DualWeaponResults : null,
                sourceResults,
                compositeSet != null ? compositeSet.References : null);
            ApplyDisplayPrefixesToResults(
                augmentations,
                compositeSet != null ? compositeSet.ComboResults : null,
                sourceResults,
                compositeSet != null ? compositeSet.References : null);
            ApplyDisplayPrefixesToResults(
                augmentations,
                compositeSet != null ? compositeSet.NonCombatCompositeResults : null,
                sourceResults,
                compositeSet != null ? compositeSet.References : null);
        }

        /// <summary>
        /// 收集一个侧别结果集合里的被动增强声明。
        /// </summary>
        private static void CollectFromSet(
            SingleSideExpressionSet set,
            List<ExpressionAugmentationDeclaration> result,
            ref int order)
        {
            if (set?.Results == null)
            {
                return;
            }

            for (int index = 0; index < set.Results.Count; index++)
            {
                FormalExpressionResult source = set.Results[index];
                if (source == null
                    || source.OutputChannel != ExpressionOutputChannel.Passive
                    || !source.IsAvailable
                    || source.RangedModuleAugmentations == null)
                {
                    continue;
                }

                for (int augmentationIndex = 0;
                     augmentationIndex < source.RangedModuleAugmentations.Count;
                     augmentationIndex++)
                {
                    RangedModuleAugmentationConfig config =
                        source.RangedModuleAugmentations[augmentationIndex];
                    if (config == null)
                    {
                        continue;
                    }

                    result.Add(new ExpressionAugmentationDeclaration
                    {
                        SourceResult = source,
                        Config = config.Clone(),
                        Order = order++
                    });
                }
            }
        }

        /// <summary>
        /// 向一个侧别结果集合追加模块。
        /// </summary>
        private static void ApplyModulesToSet(
            IReadOnlyList<ExpressionAugmentationDeclaration> augmentations,
            SingleSideExpressionSet set)
        {
            if (set?.Results == null)
            {
                return;
            }

            for (int index = 0; index < set.Results.Count; index++)
            {
                ApplyModulesToResult(augmentations, set.Results[index]);
            }
        }

        /// <summary>
        /// 只向未进入复合结果的有效远程 Verb 追加模块。
        /// </summary>
        private static void ApplyModulesToResult(
            IReadOnlyList<ExpressionAugmentationDeclaration> augmentations,
            FormalExpressionResult target)
        {
            if (!IsRangedWeaponResult(target)
                || target.CompositeKind != CompositeExpressionKind.None)
            {
                return;
            }

            List<RangedModuleMountConfig> modules = CloneModules(target.RangedModules);
            if (augmentations != null)
            {
                for (int index = 0; index < augmentations.Count; index++)
                {
                    ExpressionAugmentationDeclaration augmentation = augmentations[index];
                    if (!IsApplicable(
                            augmentation,
                            target,
                            RangedModuleAugmentationTargetCapability.RangedWeapon,
                            null,
                            null))
                    {
                        continue;
                    }

                    IReadOnlyList<RangedModuleMountConfig> additions = augmentation.Config.Modules;
                    if (additions == null)
                    {
                        continue;
                    }

                    for (int moduleIndex = 0; moduleIndex < additions.Count; moduleIndex++)
                    {
                        RangedModuleMountConfig module = additions[moduleIndex];
                        if (module != null)
                        {
                            modules.Add(module.Clone());
                        }
                    }
                }
            }

            target.RangedModules = modules;
        }

        /// <summary>
        /// 向侧别结果追加显示前缀。
        /// </summary>
        private static void ApplyDisplayPrefixesToSet(
            IReadOnlyList<ExpressionAugmentationDeclaration> augmentations,
            SingleSideExpressionSet set)
        {
            ApplyDisplayPrefixesToResults(
                augmentations,
                set != null ? set.Results : null,
                set != null ? set.Results : null,
                null);
        }

        /// <summary>
        /// 向最终结果列表追加显示前缀。
        /// </summary>
        private static void ApplyDisplayPrefixesToResults(
            IReadOnlyList<ExpressionAugmentationDeclaration> augmentations,
            IReadOnlyList<FormalExpressionResult> results,
            IReadOnlyList<FormalExpressionResult> sourceResults,
            IReadOnlyList<CompositeExpressionReference> compositeReferences)
        {
            if (results == null)
            {
                return;
            }

            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                FormalExpressionResult target = results[resultIndex];
                if (!IsRangedWeaponResult(target))
                {
                    continue;
                }

                List<string> prefixes = target.DisplayLabelPrefixes != null
                    ? new List<string>(target.DisplayLabelPrefixes)
                    : new List<string>();
                if (augmentations != null)
                {
                    for (int augmentationIndex = 0;
                         augmentationIndex < augmentations.Count;
                         augmentationIndex++)
                    {
                        ExpressionAugmentationDeclaration augmentation = augmentations[augmentationIndex];
                        if (!IsApplicable(
                                augmentation,
                                target,
                                RangedModuleAugmentationTargetCapability.RangedWeapon,
                                sourceResults,
                                compositeReferences))
                        {
                            continue;
                        }

                        string prefix = ResolvePrefix(
                            augmentation.Config,
                            augmentation.SourceResult);
                        if (!string.IsNullOrWhiteSpace(prefix) && !prefixes.Contains(prefix))
                        {
                            prefixes.Add(prefix);
                        }
                    }
                }

                target.DisplayLabelPrefixes = prefixes;
            }
        }

        /// <summary>
        /// 判断正式结果是否为当前开放增强默认接收的远程武装能力。
        /// </summary>
        private static bool IsRangedWeaponResult(FormalExpressionResult result)
        {
            return result != null
                && result.OutputChannel == ExpressionOutputChannel.Verb
                && result.WeaponMode == WeaponExpressionMode.Ranged
                && result.IsAvailable
                && result.CanProject;
        }

        /// <summary>
        /// 判断增强声明是否匹配指定中性能力。
        /// </summary>
        private static bool IsApplicable(
            ExpressionAugmentationDeclaration augmentation,
            FormalExpressionResult target,
            RangedModuleAugmentationTargetCapability capability,
            IReadOnlyList<FormalExpressionResult> sourceResults,
            IReadOnlyList<CompositeExpressionReference> compositeReferences)
        {
            RangedModuleAugmentationConfig config = augmentation != null
                ? augmentation.Config
                : null;
            if (config == null || config.AppliesToCapability != capability)
            {
                return false;
            }

            if (!IsSourceVariantAllowed(
                    config,
                    target,
                    sourceResults,
                    compositeReferences))
            {
                return false;
            }

            if (config.Scope == RangedModuleAugmentationScope.AllSides)
            {
                return true;
            }

            if (config.Scope != RangedModuleAugmentationScope.OppositeSide
                || augmentation.SourceResult == null
                || target == null)
            {
                return false;
            }

            if (target.CompositeKind == CompositeExpressionKind.None)
            {
                return AreOppositeSides(augmentation.SourceResult, target);
            }

            CompositeExpressionReference reference = FindCompositeReference(
                target,
                compositeReferences);
            if (reference?.SourceResultIds == null || sourceResults == null)
            {
                return false;
            }

            for (int index = 0; index < reference.SourceResultIds.Count; index++)
            {
                FormalExpressionResult sourceResult = FindResultById(
                    sourceResults,
                    reference.SourceResultIds[index]);
                if (AreOppositeSides(augmentation.SourceResult, sourceResult))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断目标结果的来源变体是否命中增强白名单。
        /// 复合结果按已有来源引用逐项追溯，不把复合结果自身的聚合键当成单侧真值。
        /// </summary>
        private static bool IsSourceVariantAllowed(
            RangedModuleAugmentationConfig config,
            FormalExpressionResult target,
            IReadOnlyList<FormalExpressionResult> sourceResults,
            IReadOnlyList<CompositeExpressionReference> compositeReferences)
        {
            if (config == null
                || config.AllowedSourceVariantKeys == null
                || config.AllowedSourceVariantKeys.Count == 0)
            {
                return true;
            }

            if (target == null)
            {
                return false;
            }

            if (target.CompositeKind == CompositeExpressionKind.None)
            {
                return IsSourceVariantKeyAllowed(
                    config,
                    target.SourceVariantKey);
            }

            CompositeExpressionReference reference = FindCompositeReference(
                target,
                compositeReferences);
            if (reference?.SourceResultIds == null || sourceResults == null)
            {
                return false;
            }

            for (int index = 0; index < reference.SourceResultIds.Count; index++)
            {
                FormalExpressionResult sourceResult = FindResultById(
                    sourceResults,
                    reference.SourceResultIds[index]);
                if (sourceResult != null
                    && IsSourceVariantKeyAllowed(
                        config,
                        sourceResult.SourceVariantKey))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 按稳定来源变体键判断单个结果是否命中白名单。
        /// </summary>
        private static bool IsSourceVariantKeyAllowed(
            RangedModuleAugmentationConfig config,
            string sourceVariantKey)
        {
            if (config == null || string.IsNullOrWhiteSpace(sourceVariantKey))
            {
                return false;
            }

            string normalizedSourceVariantKey = sourceVariantKey.Trim();
            for (int index = 0; index < config.AllowedSourceVariantKeys.Count; index++)
            {
                string allowedSourceVariantKey = config.AllowedSourceVariantKeys[index];
                if (!string.IsNullOrWhiteSpace(allowedSourceVariantKey)
                    && string.Equals(
                        normalizedSourceVariantKey,
                        allowedSourceVariantKey.Trim(),
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断两个结果是否分别属于 Main 和 Sub 两侧。
        /// </summary>
        private static bool AreOppositeSides(
            FormalExpressionResult source,
            FormalExpressionResult target)
        {
            TriggerSide sourceSide = source.SourceReference != null
                ? source.SourceReference.Side
                : TriggerSide.Special;
            TriggerSide targetSide = target.SourceReference != null
                ? target.SourceReference.Side
                : TriggerSide.Special;
            return (sourceSide == TriggerSide.Main && targetSide == TriggerSide.Sub)
                || (sourceSide == TriggerSide.Sub && targetSide == TriggerSide.Main);
        }

        /// <summary>
        /// 按复合结果标识读取其来源引用。
        /// </summary>
        private static CompositeExpressionReference FindCompositeReference(
            FormalExpressionResult target,
            IReadOnlyList<CompositeExpressionReference> references)
        {
            if (target == null || references == null || string.IsNullOrWhiteSpace(target.Id))
            {
                return null;
            }

            for (int index = 0; index < references.Count; index++)
            {
                CompositeExpressionReference reference = references[index];
                if (reference != null && reference.CompositeId == target.Id)
                {
                    return reference;
                }
            }

            return null;
        }

        /// <summary>
        /// 按稳定结果标识读取复合结果的单侧来源结果。
        /// </summary>
        private static FormalExpressionResult FindResultById(
            IReadOnlyList<FormalExpressionResult> results,
            string resultId)
        {
            if (results == null || string.IsNullOrWhiteSpace(resultId))
            {
                return null;
            }

            for (int index = 0; index < results.Count; index++)
            {
                FormalExpressionResult result = results[index];
                if (result != null && result.Id == resultId)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 合并三侧结果，供复合结果按来源标识追溯侧别。
        /// </summary>
        private static List<FormalExpressionResult> BuildSourceResults(
            SingleSideExpressionSet mainSet,
            SingleSideExpressionSet subSet,
            SingleSideExpressionSet specialSet)
        {
            List<FormalExpressionResult> result = new List<FormalExpressionResult>();
            AppendResults(result, mainSet != null ? mainSet.Results : null);
            AppendResults(result, subSet != null ? subSet.Results : null);
            AppendResults(result, specialSet != null ? specialSet.Results : null);
            return result;
        }

        /// <summary>
        /// 将结果集合追加到复合来源索引。
        /// </summary>
        private static void AppendResults(
            List<FormalExpressionResult> target,
            IReadOnlyList<FormalExpressionResult> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] != null)
                {
                    target.Add(source[index]);
                }
            }
        }

        /// <summary>
        /// 解析增强声明的名称前缀。
        /// </summary>
        private static string ResolvePrefix(
            RangedModuleAugmentationConfig config,
            FormalExpressionResult source)
        {
            if (config == null)
            {
                return null;
            }

            switch (config.DisplayNamePrefixMode)
            {
                case RangedModuleAugmentationDisplayPrefixMode.SourceExpressionLabel:
                    return source != null ? source.DisplayLabel : null;
                case RangedModuleAugmentationDisplayPrefixMode.Explicit:
                    return config.DisplayNamePrefix;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 复制结果已有的模块列表。
        /// </summary>
        private static List<RangedModuleMountConfig> CloneModules(
            IReadOnlyList<RangedModuleMountConfig> modules)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (modules == null)
            {
                return result;
            }

            for (int index = 0; index < modules.Count; index++)
            {
                RangedModuleMountConfig module = modules[index];
                if (module != null)
                {
                    result.Add(module.Clone());
                }
            }

            return result;
        }
    }
}
