using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义公开读取面。
    /// Content 和其它外部程序集只能通过这里读取芯片快照。
    /// </summary>
    public static class ChipSnapshotAccess
    {
        /// <summary>
        /// 读取指定芯片 Thing 的正式快照。
        /// 优先检查中性实例定义提供器，回退到 ThingDef 上的静态配置。
        /// </summary>
        public static ChipDefinitionSnapshot Read(Thing chip)
        {
            if (chip == null)
            {
                return Read((ThingDef)null);
            }

            // 优先从中性实例提供器读取动态配置。
            ChipDefinitionConfig manufactured;
            if (ChipInstanceSurfaceAccess.TryGetDefinition(chip, out manufactured))
            {
                ChipLoadoutConfig loadout = manufactured.Loadout;
                ChipTrionConfig trion = manufactured.Trion;
                return new ChipDefinitionSnapshot
                {
                    ThingDef = chip.def,
                    IsValid = true,
                    SlotRegion = loadout != null ? loadout.SlotRegion : ChipSlotRegion.Unspecified,
                    SlotOccupancy = loadout != null ? loadout.SlotOccupancy : ChipSlotOccupancy.Unspecified,
                    CapacityCost = trion != null ? trion.CapacityCost : 0f,
                    ActivationCost = trion != null ? trion.ActivationCost : 0f,
                    ActivationDelayTicks = loadout != null ? loadout.ActivationDelayTicks : -1,
                    DeactivationDelayTicks = loadout != null ? loadout.DeactivationDelayTicks : -1
                };
            }

            return Read(chip.def);
        }

        /// <summary>
        /// 读取指定芯片 ThingDef 的正式快照。
        /// </summary>
        public static ChipDefinitionSnapshot Read(ThingDef chipDef)
        {
            ChipDefinitionReadResult result = ChipSurfaceAccess.Read(chipDef);
            ChipLoadoutContract loadout = result != null && result.Contract != null
                ? result.Contract.Loadout
                : null;
            ChipTrionContract trion = result != null && result.Contract != null
                ? result.Contract.Trion
                : null;

            return new ChipDefinitionSnapshot
            {
                ThingDef = chipDef,
                IsValid = result != null
                    && result.Validation != null
                    && result.Validation.IsValid,
                SlotRegion = loadout != null ? loadout.SlotRegion : ChipSlotRegion.Unspecified,
                SlotOccupancy = loadout != null ? loadout.SlotOccupancy : ChipSlotOccupancy.Unspecified,
                CapacityCost = trion != null ? trion.CapacityCost : 0f,
                ActivationCost = trion != null ? trion.ActivationCost : 0f,
                ActivationDelayTicks = loadout != null ? loadout.ActivationDelayTicks : -1,
                DeactivationDelayTicks = loadout != null ? loadout.DeactivationDelayTicks : -1
            };
        }
    }
}
