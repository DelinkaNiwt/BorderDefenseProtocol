using BDP.Core.Trigger;
using Verse;
using VerseThing = Verse.Thing;

namespace BDP.Content.Assembly.ChipManufacturing.Production
{
    /// <summary>
    /// 把触发体固定装载初始化键适配为动态芯片成品的Content工厂。
    /// </summary>
    public sealed class ManufacturedChipTriggerFixedLoadoutFactory : ITriggerFixedLoadoutFactory
    {
        /// <summary>
        /// 仅处理动态芯片实体Def，并按初始化键解析单个芯片功能预设。
        /// </summary>
        public bool TryCreate(
            ThingDef chipDef,
            string initializationKey,
            out VerseThing chip)
        {
            chip = null;
            if (chipDef == null
                || chipDef.defName != ChipProductCreationService.ManufacturedChipDefName
                || string.IsNullOrWhiteSpace(initializationKey))
            {
                return false;
            }

            string failureReason;
            return ChipProductCreationService.TryCreateSingleFunctionProduct(
                initializationKey,
                out chip,
                out failureReason);
        }
    }
}
