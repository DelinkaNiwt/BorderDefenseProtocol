using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Requirements;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>按已确认合法的芯片功能模板组合合成芯片正式配置。</summary>
    public static class ChipConfigurationMergeService
    {
        /// <summary>复制单芯片功能模板配置，不让成品持有可变的 Def 列表。</summary>
        public static ChipDefinitionConfig CloneSingle(ChipManufacturingPresetDef function)
        {
            ChipDefinitionConfig source = function?.config;
            if (source == null)
            {
                return new ChipDefinitionConfig();
            }

            return CloneConfig(source);
        }

        /// <summary>复制一份尚未应用武装构型的有效配置。</summary>
        public static ChipDefinitionConfig CloneConfig(ChipDefinitionConfig source)
        {
            if (source == null)
            {
                return new ChipDefinitionConfig();
            }

            ChipFeatureCloneContext context = new ChipFeatureCloneContext();
            return new ChipDefinitionConfig
            {
                Profile = CloneProfile(source.Profile),
                Loadout = CloneLoadout(source.Loadout),
                Expression = ChipExpressionMergeService.CloneSingle(
                    source.Expression,
                    context,
                    null),
                Features = ChipFeatureMergeService.CloneFeatures(
                    source.Features,
                    context),
                Trion = CloneTrion(source.Trion),
                ActivationRequirements = source.ActivationRequirements != null
                    ? new List<PawnRequirement>(source.ActivationRequirements)
                    : new List<PawnRequirement>(),
                Extensions = source.Extensions != null
                    ? new List<ChipExtensionConfig>(source.Extensions)
                    : new List<ChipExtensionConfig>()
            };
        }

        /// <summary>合成双芯片功能模板：本体成本相加，延迟取大值，集合字段并集。</summary>
        public static ChipDefinitionConfig MergeDual(
            ChipManufacturingPresetDef firstFunction,
            ChipManufacturingPresetDef secondFunction)
        {
            ChipDefinitionConfig first = firstFunction.config;
            ChipDefinitionConfig second = secondFunction.config;
            ChipFeatureCloneContext context = new ChipFeatureCloneContext();
            return new ChipDefinitionConfig
            {
                Profile = new ChipProfileConfig
                {
                    Category = first.Profile.Category,
                    Tags = Union(first.Profile.Tags, second.Profile.Tags)
                },
                Loadout = new ChipLoadoutConfig
                {
                    SlotRegion = first.Loadout.SlotRegion,
                    SlotOccupancy = first.Loadout.SlotOccupancy,
                    ActivationDelayTicks = Math.Max(
                        first.Loadout.ActivationDelayTicks,
                        second.Loadout.ActivationDelayTicks),
                    DeactivationDelayTicks = Math.Max(
                        first.Loadout.DeactivationDelayTicks,
                        second.Loadout.DeactivationDelayTicks),
                    ActivationExclusionGroups = Union(
                        first.Loadout.ActivationExclusionGroups,
                        second.Loadout.ActivationExclusionGroups),
                    ActivationAudio = MergeActivationAudio(
                        first.Loadout.ActivationAudio,
                        second.Loadout.ActivationAudio)
                },
                Trion = new ChipTrionConfig
                {
                    CapacityCost = (first.Trion?.CapacityCost ?? 0f)
                        + (second.Trion?.CapacityCost ?? 0f),
                    ActivationCost = (first.Trion?.ActivationCost ?? 0f)
                        + (second.Trion?.ActivationCost ?? 0f)
                },
                ActivationRequirements = ChipRequirementMergeRegistry.Merge(
                    first.ActivationRequirements,
                    second.ActivationRequirements),
                Extensions = ChipExtensionMergeRegistry.Merge(
                    first.Extensions,
                    second.Extensions),
                Expression = ChipExpressionMergeService.MergeDual(
                    firstFunction,
                    secondFunction,
                    context),
                Features = ChipFeatureMergeService.MergeDual(
                    firstFunction,
                    secondFunction,
                    context)
            };
        }

        /// <summary>复制画像配置。</summary>
        private static ChipProfileConfig CloneProfile(ChipProfileConfig source)
        {
            return source == null
                ? new ChipProfileConfig()
                : new ChipProfileConfig
                {
                    Category = source.Category,
                    Tags = source.Tags != null
                        ? new List<ChipTagDef>(source.Tags)
                        : new List<ChipTagDef>()
                };
        }

        /// <summary>复制装载配置。</summary>
        private static ChipLoadoutConfig CloneLoadout(ChipLoadoutConfig source)
        {
            return source == null
                ? null
                : new ChipLoadoutConfig
                {
                    SlotRegion = source.SlotRegion,
                    SlotOccupancy = source.SlotOccupancy,
                    ActivationDelayTicks = source.ActivationDelayTicks,
                    DeactivationDelayTicks = source.DeactivationDelayTicks,
                    ActivationExclusionGroups = source.ActivationExclusionGroups != null
                        ? new List<ChipExclusionGroupDef>(source.ActivationExclusionGroups)
                        : null,
                    ActivationAudio = CloneActivationAudio(source.ActivationAudio)
                };
        }

        /// <summary>复制激活音效配置，避免成品配置持有芯片功能模板的可变对象。</summary>
        private static ChipActivationAudioConfig CloneActivationAudio(
            ChipActivationAudioConfig source)
        {
            return source == null
                ? null
                : new ChipActivationAudioConfig
                {
                    ActivationWarmupStartSound = source.ActivationWarmupStartSound,
                    ActivationWarmupLoopSound = source.ActivationWarmupLoopSound,
                    ActivationWarmupEndSound = source.ActivationWarmupEndSound
                };
        }

        /// <summary>
        /// 合并双芯片功能模板的激活音效。
        /// 每个位置按功能模板顺序取第一个非空声明，避免一次前摇叠放两套音效。
        /// </summary>
        private static ChipActivationAudioConfig MergeActivationAudio(
            ChipActivationAudioConfig first,
            ChipActivationAudioConfig second)
        {
            if (first == null && second == null)
            {
                return null;
            }

            return new ChipActivationAudioConfig
            {
                ActivationWarmupStartSound = first?.ActivationWarmupStartSound
                    ?? second?.ActivationWarmupStartSound,
                ActivationWarmupLoopSound = first?.ActivationWarmupLoopSound
                    ?? second?.ActivationWarmupLoopSound,
                ActivationWarmupEndSound = first?.ActivationWarmupEndSound
                    ?? second?.ActivationWarmupEndSound
            };
        }

        /// <summary>复制芯片本体 Trion 配置。</summary>
        private static ChipTrionConfig CloneTrion(ChipTrionConfig source)
        {
            return source == null
                ? null
                : new ChipTrionConfig
                {
                    CapacityCost = source.CapacityCost,
                    ActivationCost = source.ActivationCost
                };
        }

        /// <summary>按首次出现顺序合并两个 Def 集合。</summary>
        private static List<T> Union<T>(IList<T> first, IList<T> second)
            where T : class
        {
            List<T> result = new List<T>();
            AppendUnique(result, first);
            AppendUnique(result, second);
            return result;
        }

        /// <summary>把尚未出现的条目追加到目标集合。</summary>
        private static void AppendUnique<T>(List<T> target, IList<T> source)
            where T : class
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                T item = source[index];
                if (item != null && !target.Contains(item))
                {
                    target.Add(item);
                }
            }
        }
    }
}
