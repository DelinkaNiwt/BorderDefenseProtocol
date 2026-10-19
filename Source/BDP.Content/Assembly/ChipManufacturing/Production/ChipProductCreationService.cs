using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Assembly.ChipManufacturing.Thing;
using Verse;
using VerseThing = Verse.Thing;

namespace BDP.Content.Assembly.ChipManufacturing.Production
{
    /// <summary>
    /// 动态芯片成品创建服务。
    /// 只负责把已经验证的组合记录变成实体成品，不创建账单、不消耗材料、不占用工作时间。
    /// </summary>
    public static class ChipProductCreationService
    {
        /// <summary>
        /// 当前动态芯片唯一实体ThingDef名称。
        /// </summary>
        public const string ManufacturedChipDefName = "BDP_Chip_Custom";

        /// <summary>
        /// 根据单个芯片功能预设创建一枚有效动态芯片。
        /// 预设本身必须能解析为单功能组合记录。
        /// </summary>
        public static bool TryCreateSingleFunctionProduct(
            string functionDefName,
            out VerseThing product,
            out string failureReason)
        {
            product = null;
            failureReason = null;

            ChipManufacturingPresetDef function =
                ChipManufacturingDefLookup.FindChipFunctionDef(functionDefName);
            if (function == null || function.config == null)
            {
                failureReason = "function_missing";
                return false;
            }

            if (function.config.Profile == null
                || function.config.Profile.Category == null
                || function.config.Loadout == null)
            {
                failureReason = "function_config_invalid";
                return false;
            }

            ChipCombinationRecord record = new ChipCombinationRecord
            {
                CategoryDefName = function.config.Profile.Category.defName,
                ProfessionDefName = function.profession != null
                    ? function.profession.defName
                    : null,
                ArmamentFormDefName = function.fixedLoadoutArmamentForm != null
                    ? function.fixedLoadoutArmamentForm.defName
                    : null,
                OrderedChipFunctionDefNames = new System.Collections.Generic.List<string>
                {
                    function.defName
                }
            };

            ChipCombinationResolution resolution = new ChipCombinationResolver().Resolve(record);
            if (resolution == null
                || resolution.Status != ChipCombinationResolutionStatus.Valid)
            {
                failureReason = "function_resolution_invalid";
                return false;
            }

            return TryCreateProduct(
                DefDatabase<ThingDef>.GetNamedSilentFail(ManufacturedChipDefName),
                record,
                out product,
                out failureReason);
        }

        /// <summary>
        /// 根据已经验证的组合记录创建一枚实体成品，并写入动态芯片组件。
        /// 调用方负责把返回的实体交给后续容器或地图放置流程。
        /// </summary>
        public static bool TryCreateProduct(
            ThingDef productDef,
            ChipCombinationRecord record,
            out VerseThing product,
            out string failureReason)
        {
            product = null;
            failureReason = null;
            if (productDef == null || record == null)
            {
                failureReason = "product_input_invalid";
                return false;
            }

            VerseThing candidate = ThingMaker.MakeThing(productDef);
            if (candidate == null)
            {
                failureReason = "product_creation_failed";
                return false;
            }

            CompManufacturedChip manufacturedChip = candidate.TryGetComp<CompManufacturedChip>();
            if (manufacturedChip == null)
            {
                candidate.Destroy(DestroyMode.Vanish);
                failureReason = "manufactured_chip_comp_missing";
                return false;
            }

            manufacturedChip.InitializeFromBill(record);
            product = candidate;
            return true;
        }
    }
}
