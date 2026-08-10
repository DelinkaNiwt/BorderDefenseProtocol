using System.Collections.Generic;
using BDP.Core.Requirements;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 默认芯片定义契约解释器。
    /// 它把作者写在 Def 上的总配置翻译成主模组承认的正式声明结果。
    /// </summary>
    internal sealed class ChipDefinitionContractResolver
    {
        /// <summary>
        /// 解释指定 ThingDef 的芯片定义契约。
        /// </summary>
        /// <summary>解析指定 ThingDef 的芯片定义契约（仅用于静态 Def 路径，不处理制造期动态芯片）。</summary>
        public ChipDefinitionContract Resolve(ThingDef thingDef)
        {
            ChipDefinitionConfig config = thingDef != null
                ? thingDef.GetModExtension<ChipDefinitionConfig>()
                : null;
            ChipLoadoutConfig loadoutConfig = config != null ? config.Loadout : null;
            ChipExpressionConfig expressionConfig = config != null ? config.Expression : null;

            ChipDefinitionContract contract = new ChipDefinitionContract
            {
                ThingDef = thingDef,
                Profile = TranslateProfile(config != null ? config.Profile : null),
                Loadout = TranslateLoadout(loadoutConfig),
                Expression = TranslateExpression(expressionConfig),
                Trion = TranslateTrion(config != null ? config.Trion : null),
                ActivationRequirements = TranslateActivationRequirements(
                    config != null ? config.ActivationRequirements : null),
                Extensions = TranslateExtensions(config != null ? config.Extensions : null)
            };
            contract.DeclaredBlocks = BuildDeclaredBlocks(contract);
            return contract;
        }

        /// <summary>
        /// 翻译画像声明块。
        /// </summary>
        private static ChipProfileContract TranslateProfile(ChipProfileConfig config)
        {
            if (config == null)
            {
                return null;
            }

            return new ChipProfileContract
            {
                Category = config.Category,
                Tags = config.Tags != null
                    ? new List<ChipTagDef>(config.Tags)
                    : new List<ChipTagDef>()
            };
        }

        /// <summary>
        /// 翻译装载声明块。
        /// </summary>
        private static ChipLoadoutContract TranslateLoadout(ChipLoadoutConfig config)
        {
            if (config == null)
            {
                return null;
            }

            return new ChipLoadoutContract
            {
                SlotRegion = config.SlotRegion,
                SlotOccupancy = config.SlotOccupancy,
                ActivationExclusionGroups = config.ActivationExclusionGroups != null
                    ? new List<ChipExclusionGroupDef>(config.ActivationExclusionGroups)
                    : new List<ChipExclusionGroupDef>(),
                ActivationDelayTicks = config.ActivationDelayTicks,
                DeactivationDelayTicks = config.DeactivationDelayTicks
            };
        }

        /// <summary>
        /// 翻译表达声明句柄。
        /// </summary>
        private static ChipExpressionContractHandle TranslateExpression(ChipExpressionConfig config)
        {
            return new ChipExpressionContractHandle
            {
                HasExpressionBlock = config != null,
                Config = config,
                StructureKey = config != null ? "Entries+Modes" : null
            };
        }

        /// <summary>
        /// 翻译 Trion 声明块。
        /// </summary>
        private static ChipTrionContract TranslateTrion(ChipTrionConfig config)
        {
            if (config == null)
            {
                return null;
            }

            return new ChipTrionContract
            {
                CapacityCost = config.CapacityCost,
                ActivationCost = config.ActivationCost
            };
        }

        /// <summary>
        /// 浅复制激活条件集合边界，同时保留作者声明顺序。
        /// </summary>
        private static IReadOnlyList<PawnRequirement> TranslateActivationRequirements(
            List<PawnRequirement> requirements)
        {
            return requirements != null
                ? new List<PawnRequirement>(requirements)
                : new List<PawnRequirement>();
        }

        /// <summary>
        /// 翻译强类型静态扩展集合。
        /// Def 扩展加载后保持不变，这里只浅复制集合边界，不复制具体业务对象。
        /// </summary>
        private static IReadOnlyList<ChipExtensionConfig> TranslateExtensions(
            List<ChipExtensionConfig> configs)
        {
            return configs != null
                ? new List<ChipExtensionConfig>(configs)
                : new List<ChipExtensionConfig>();
        }

        /// <summary>
        /// 根据已解释结果构建已声明块清单。
        /// </summary>
        private static IReadOnlyList<ChipDefinitionDeclaredBlock> BuildDeclaredBlocks(ChipDefinitionContract contract)
        {
            List<ChipDefinitionDeclaredBlock> blocks = new List<ChipDefinitionDeclaredBlock>();
            if (contract == null)
            {
                return blocks;
            }

            if (contract.Profile != null)
            {
                blocks.Add(ChipDefinitionDeclaredBlock.Profile);
            }

            if (contract.Loadout != null)
            {
                blocks.Add(ChipDefinitionDeclaredBlock.Loadout);
            }

            if (contract.Expression != null && contract.Expression.HasExpressionBlock)
            {
                blocks.Add(ChipDefinitionDeclaredBlock.Expression);
            }

            if (contract.Trion != null)
            {
                blocks.Add(ChipDefinitionDeclaredBlock.Trion);
            }

            if (HasDeclaredActivationRequirement(contract.ActivationRequirements))
            {
                blocks.Add(ChipDefinitionDeclaredBlock.ActivationRequirements);
            }

            if (HasDeclaredExtension(contract.Extensions))
            {
                blocks.Add(ChipDefinitionDeclaredBlock.Extensions);
            }

            return blocks;
        }

        /// <summary>
        /// 判断条件集合里是否至少存在一个真实声明。
        /// 空项仍由校验器负责报告。
        /// </summary>
        private static bool HasDeclaredActivationRequirement(
            IReadOnlyList<PawnRequirement> requirements)
        {
            if (requirements == null)
            {
                return false;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断扩展集合里是否至少存在一个真实声明。
        /// 空条目仍交给校验器报错，但不能单独把扩展块登记为有效声明。
        /// </summary>
        private static bool HasDeclaredExtension(
            IReadOnlyList<ChipExtensionConfig> extensions)
        {
            if (extensions == null)
            {
                return false;
            }

            for (int i = 0; i < extensions.Count; i++)
            {
                if (extensions[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
