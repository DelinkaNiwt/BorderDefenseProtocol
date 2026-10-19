using System;

namespace BDP.Core.Information
{
    /// <summary>集中映射芯片信息中已有且有明确语言键的稳定值。</summary>
    internal static class ChipInformationKnownValueFactory
    {
        /// <summary>将已知武器模式映射为本地化键；未知模式保持原始值。</summary>
        internal static ChipInformationValue FromWeaponMode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string translationKey;
            if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
            {
                translationKey = "BDP_Info_WeaponMode_None";
            }
            else if (string.Equals(value, "Melee", StringComparison.OrdinalIgnoreCase))
            {
                translationKey = "BDP_Info_WeaponMode_Melee";
            }
            else if (string.Equals(value, "Ranged", StringComparison.OrdinalIgnoreCase))
            {
                translationKey = "BDP_Info_WeaponMode_Ranged";
            }
            else
            {
                return ChipInformationValue.FromEnum(value);
            }

            return ChipInformationValue.FromLocalizedEnum(value, translationKey);
        }

        /// <summary>将已知 Hediff 应用模式映射为本地化键；未知模式保持原始值。</summary>
        internal static ChipInformationValue FromHediffApplyMode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return string.Equals(value, "countToSeverity", StringComparison.OrdinalIgnoreCase)
                ? ChipInformationValue.FromLocalizedEnum(
                    value,
                    "BDP_Info_HediffApplyMode_CountToSeverity")
                : ChipInformationValue.FromEnum(value);
        }

        /// <summary>将已知 Hediff 控制模式映射为本地化键；未知模式保持原始值。</summary>
        internal static ChipInformationValue FromHediffControlMode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (string.Equals(value, "Automatic", StringComparison.OrdinalIgnoreCase))
            {
                return ChipInformationValue.FromLocalizedEnum(
                    value,
                    "BDP_Info_HediffControlMode_Automatic");
            }

            if (string.Equals(value, "AbilityButton", StringComparison.OrdinalIgnoreCase))
            {
                return ChipInformationValue.FromLocalizedEnum(
                    value,
                    "BDP_Info_HediffControlMode_AbilityButton");
            }

            return ChipInformationValue.FromEnum(value);
        }
    }
}
