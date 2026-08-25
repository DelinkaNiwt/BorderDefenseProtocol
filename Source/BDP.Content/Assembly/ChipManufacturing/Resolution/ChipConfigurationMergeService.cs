using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Requirements;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>按已确认合法的动作组合合成芯片正式配置。</summary>
    public static class ChipConfigurationMergeService
    {
        /// <summary>复制单动作配置，不让成品持有可变的 Def 列表。</summary>
        public static ChipDefinitionConfig CloneSingle(ChipActionPresetDef action)
        {
            ChipDefinitionConfig source = action?.config;
            if (source == null)
            {
                return new ChipDefinitionConfig();
            }

            return new ChipDefinitionConfig
            {
                Profile = CloneProfile(source.Profile),
                Loadout = CloneLoadout(source.Loadout),
                Expression = ChipExpressionMergeService.CloneSingle(source.Expression),
                Trion = CloneTrion(source.Trion),
                ActivationRequirements = source.ActivationRequirements != null
                    ? new List<PawnRequirement>(source.ActivationRequirements)
                    : new List<PawnRequirement>(),
                Extensions = source.Extensions != null
                    ? new List<ChipExtensionConfig>(source.Extensions)
                    : new List<ChipExtensionConfig>()
            };
        }

        /// <summary>合成双动作：本体成本相加，延迟取大值，集合字段并集。</summary>
        public static ChipDefinitionConfig MergeDual(
            ChipActionPresetDef firstAction,
            ChipActionPresetDef secondAction)
        {
            ChipDefinitionConfig first = firstAction.config;
            ChipDefinitionConfig second = secondAction.config;
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
                    firstAction,
                    secondAction)
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

        /// <summary>复制激活音效配置，避免成品配置持有动作预设的可变对象。</summary>
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
        /// 合并双动作的激活音效。
        /// 每个位置按动作顺序取第一个非空声明，避免一次前摇叠放两套音效。
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
