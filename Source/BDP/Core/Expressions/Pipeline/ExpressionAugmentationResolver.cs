using System.Collections.Generic;
using BDP.Core.AttackExecution;

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
            ApplyDisplayPrefixesToResults(
                augmentations,
                compositeSet != null ? compositeSet.DualWeaponResults : null);
            ApplyDisplayPrefixesToResults(
                augmentations,
                compositeSet != null ? compositeSet.ComboResults : null);
            ApplyDisplayPrefixesToResults(
                augmentations,
                compositeSet != null ? compositeSet.NonCombatCompositeResults : null);
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
                    || source.ResultKind != ExpressionResultKind.Passive
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
                    if (!IsApplicable(augmentation?.Config, RangedModuleAugmentationTargetCapability.RangedWeapon))
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
            ApplyDisplayPrefixesToResults(augmentations, set != null ? set.Results : null);
        }

        /// <summary>
        /// 向最终结果列表追加显示前缀。
        /// </summary>
        private static void ApplyDisplayPrefixesToResults(
            IReadOnlyList<ExpressionAugmentationDeclaration> augmentations,
            IReadOnlyList<FormalExpressionResult> results)
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
                        RangedModuleAugmentationConfig config = augmentations[augmentationIndex]?.Config;
                        if (!IsApplicable(config, RangedModuleAugmentationTargetCapability.RangedWeapon))
                        {
                            continue;
                        }

                        string prefix = ResolvePrefix(config, augmentations[augmentationIndex].SourceResult);
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
                && result.ResultKind == ExpressionResultKind.Verb
                && result.WeaponMode == WeaponExpressionMode.Ranged
                && result.IsAvailable
                && result.CanProject;
        }

        /// <summary>
        /// 判断增强声明是否匹配指定中性能力。
        /// </summary>
        private static bool IsApplicable(
            RangedModuleAugmentationConfig config,
            RangedModuleAugmentationTargetCapability capability)
        {
            return config != null && config.AppliesToCapability == capability;
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
