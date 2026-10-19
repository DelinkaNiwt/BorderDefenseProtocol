using BDP.Core.Chips;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Content.Trigger.Classification
{
    /// <summary>
    /// BDP 配件独立激活控制提供器。
    /// 是否独立控制只读取正式芯片画像的 Category，不新增激活标识。
    /// </summary>
    internal sealed class AccessoryActivationControlProvider : ITriggerChipActivationControlProvider
    {
        /// <summary>
        /// 配件芯片类别 DefName。
        /// </summary>
        private const string AccessoryCategoryDefName = "BDP_ChipCategory_Accessory";

        /// <summary>
        /// 按芯片画像类别判断是否拥有独立激活控制范围。
        /// </summary>
        public bool HasIndependentActivationControl(Thing chip)
        {
            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(chip);
            return snapshot != null
                && snapshot.IsValid
                && string.Equals(
                    snapshot.CategoryDefName,
                    AccessoryCategoryDefName,
                    System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
