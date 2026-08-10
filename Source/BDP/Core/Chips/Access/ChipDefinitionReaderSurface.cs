using System.Collections.Generic;
using BDP.Core.Expressions.Runtime;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义读取表面。
    /// 它把解释和校验收成一次稳定读取结果。
    /// </summary>
    internal sealed class ChipDefinitionService : IChipDefinitionReader
    {
        /// <summary>
        /// 当前正式服务依赖的契约解释器。
        /// </summary>
        private static readonly ChipDefinitionContractResolver ContractResolver =
            new ChipDefinitionContractResolver();

        /// <summary>
        /// 当前正式服务依赖的最低合法性校验器。
        /// </summary>
        private static readonly ChipDefinitionValidator Validator =
            new ChipDefinitionValidator();

        /// <summary>
        /// 当前正式服务依赖的定义读取缓存。
        /// </summary>
        private readonly ChipDefinitionCache definitionCache;

        /// <summary>
        /// 使用默认缓存构造正式服务。
        /// </summary>
        public ChipDefinitionService()
            : this(new ChipDefinitionCache())
        {
        }

        /// <summary>
        /// 使用指定缓存构造正式服务。
        /// </summary>
        public ChipDefinitionService(ChipDefinitionCache definitionCache)
        {
            this.definitionCache = definitionCache ?? new ChipDefinitionCache();
        }

        /// <summary>
        /// 读取指定 ThingDef 的芯片定义结果。
        /// </summary>
        public ChipDefinitionReadResult Read(ThingDef thingDef)
        {
            return definitionCache.GetOrAdd(thingDef, targetThingDef =>
            {
                ChipDefinitionContract contract = ContractResolver.Resolve(targetThingDef);
                ChipDefinitionValidationResult validation = Validator.Validate(contract);
                return new ChipDefinitionReadResult
                {
                    ThingDef = targetThingDef,
                    Contract = contract,
                    Validation = validation
                };
            });
        }

        /// <summary>
        /// 读取指定 Thing 的芯片定义结果。
        /// 芯片配置优先来自中性实例提供器，缺少时回退静态 Def。
        /// </summary>
        public ChipDefinitionReadResult Read(Thing thing)
        {
            if (thing == null)
            {
                return Read((ThingDef)null);
            }

            ChipDefinitionConfig manufactured;
            if (ChipInstanceSurfaceAccess.TryGetDefinition(thing, out manufactured))
            {
                return BuildManufacturedResult(thing.def, manufactured);
            }

            return Read(thing.def);
        }

        /// <summary>
        /// 从实例配置构建一份合法的 ChipDefinitionReadResult。
        /// 不走 Def 静态契约解释器，直接由提供器数据合成。
        /// </summary>
        private static ChipDefinitionReadResult BuildManufacturedResult(
            ThingDef thingDef,
            ChipDefinitionConfig config)
        {
            ChipLoadoutConfig loadout = config.Loadout;
            ChipTrionConfig trion = config.Trion;
            IReadOnlyList<PawnRequirement> activationRequirements = config.ActivationRequirements != null
                ? new List<PawnRequirement>(config.ActivationRequirements).AsReadOnly()
                : new List<PawnRequirement>().AsReadOnly();

            ChipDefinitionContract contract = new ChipDefinitionContract
            {
                ThingDef = thingDef,
                Profile = new ChipProfileContract
                {
                    Category = config.Profile?.Category,
                    Tags = config.Profile?.Tags
                },
                Loadout = new ChipLoadoutContract
                {
                    SlotRegion = loadout != null ? loadout.SlotRegion : ChipSlotRegion.Unspecified,
                    SlotOccupancy = loadout != null ? loadout.SlotOccupancy : ChipSlotOccupancy.Unspecified,
                    ActivationDelayTicks = loadout != null ? loadout.ActivationDelayTicks : -1,
                    DeactivationDelayTicks = loadout != null ? loadout.DeactivationDelayTicks : -1
                },
                Trion = new ChipTrionContract
                {
                    CapacityCost = trion != null ? trion.CapacityCost : 0f,
                    ActivationCost = trion != null ? trion.ActivationCost : 0f
                },
                ActivationRequirements = activationRequirements,
                Expression = new ChipExpressionContractHandle
                {
                    HasExpressionBlock = config.Expression != null,
                    Config = config.Expression
                }
            };

            ChipDefinitionValidationResult validation = new ChipDefinitionValidationResult
            {
                IsValid = true,
                Errors = new List<ChipDefinitionValidationMessage>().AsReadOnly(),
                Warnings = new List<ChipDefinitionValidationMessage>().AsReadOnly()
            };

            return new ChipDefinitionReadResult
            {
                ThingDef = thingDef,
                Contract = contract,
                Validation = validation
            };
        }
    }
}
