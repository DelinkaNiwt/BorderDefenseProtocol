using System;
using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 将表达系统已发布结果适配为信息模型运行事实。
    /// </summary>
    public static class ChipInformationExpressionInputAdapter
    {
        /// <summary>把公开表达发布快照转换为运行表达事实集合。</summary>
        public static IReadOnlyList<ChipInformationExpressionFact> Create(
            ExpressionPublishedProjectionSnapshot projection)
        {
            List<ChipInformationExpressionFact> result =
                new List<ChipInformationExpressionFact>();
            if (projection == null || projection.Results == null)
            {
                return result;
            }

            for (int index = 0; index < projection.Results.Count; index++)
            {
                ExpressionPublishedResultSnapshot published = projection.Results[index];
                ChipInformationExpressionFact fact = CreateFact(published);
                if (fact != null)
                {
                    result.Add(fact);
                }
            }

            return result;
        }

        /// <summary>把单条公开结果转换为运行事实。</summary>
        private static ChipInformationExpressionFact CreateFact(
            ExpressionPublishedResultSnapshot published)
        {
            if (published == null || string.IsNullOrWhiteSpace(published.ResultId))
            {
                return null;
            }

            string channelKey = GetChannelKey(published);
            List<ChipInformationFactValue> fields = new List<ChipInformationFactValue>();
            AddField(
                fields,
                "publishedKey",
                "BDP_Info_Field_PublishedKey",
                ChipInformationValueKind.Text,
                ChipInformationValue.FromText(published.PublishedKey),
                published.ResultId);
            AddField(
                fields,
                "displayLabel",
                "BDP_Info_Field_Expression",
                ChipInformationValueKind.Text,
                ChipInformationValue.FromText(published.DisplayLabel),
                published.ResultId);
            AddField(
                fields,
                "available",
                "BDP_Info_Field_Available",
                ChipInformationValueKind.Boolean,
                ChipInformationValue.FromBoolean(published.IsAvailable),
                published.ResultId);
            AddField(
                fields,
                "canProject",
                "BDP_Info_Field_CanProject",
                ChipInformationValueKind.Boolean,
                ChipInformationValue.FromBoolean(published.CanProject),
                published.ResultId);
            AddField(
                fields,
                "mode",
                "BDP_Info_Field_Mode",
                ChipInformationValueKind.Enum,
                StringValue(published.ModeKey),
                published.ResultId);
            AddField(
                fields,
                "weaponMode",
                "BDP_Info_Field_WeaponMode",
                ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromWeaponMode(published.WeaponModeKey),
                published.ResultId);
            AddField(
                fields,
                "hediffDef",
                "BDP_Info_Field_Hediff",
                ChipInformationValueKind.Enum,
                ChipInformationValue.FromDefReference(typeof(HediffDef), published.HediffDefName),
                published.ResultId);
            AddField(
                fields,
                "hediffApplyMode",
                "BDP_Info_Field_HediffApplyMode",
                ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromHediffApplyMode(published.HediffApplyModeKey),
                published.ResultId);
            AddField(
                fields,
                "hediffControlMode",
                "BDP_Info_Field_HediffControlMode",
                ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromHediffControlMode(published.HediffControlMode),
                published.ResultId);
            AddField(
                fields,
                "trionUseCost",
                "BDP_Info_Field_UseCost",
                ChipInformationValueKind.Number,
                ChipInformationValue.FromNumber(published.TrionUseCost),
                published.ResultId);
            AddField(
                fields,
                "trionMinimumRequired",
                "BDP_Info_Field_MinimumRequired",
                ChipInformationValueKind.Number,
                ChipInformationValue.FromNumber(published.TrionMinimumRequired),
                published.ResultId);
            AddExposedData(fields, published);
            return new ChipInformationExpressionFact(
                published.ResultId,
                "BDP_Info_Field_Expression",
                channelKey,
                published.ResultId,
                ChipInformationSourceKind.RuntimeResult,
                published.WeaponModeKey,
                published.ModeKey,
                null,
                published.IsAvailable,
                fields,
                null,
                null);
        }

        /// <summary>把已发布通道转换成通用通道键。</summary>
        private static string GetChannelKey(ExpressionPublishedResultSnapshot published)
        {
            switch (published.ChannelKind)
            {
                case ExpressionPublishedChannelKind.Ability:
                    return "ability";
                case ExpressionPublishedChannelKind.Hediff:
                    return "hediff";
                case ExpressionPublishedChannelKind.Passive:
                    return "passive";
                default:
                    return string.Equals(
                        published.WeaponModeKey,
                        "Melee",
                        StringComparison.OrdinalIgnoreCase)
                        ? "melee"
                        : "ranged";
            }
        }

        /// <summary>提取公开附加数据。</summary>
        private static void AddExposedData(
            ICollection<ChipInformationFactValue> target,
            ExpressionPublishedResultSnapshot published)
        {
            if (published.ExposedData == null)
            {
                return;
            }

            for (int index = 0; index < published.ExposedData.Count; index++)
            {
                ExpressionPublishedDatum datum = published.ExposedData[index];
                if (datum == null || string.IsNullOrWhiteSpace(datum.Key))
                {
                    continue;
                }

                AddField(
                    target,
                    "exposed." + datum.Key,
                    "BDP_Info_Field_ExposedData",
                    ChipInformationValueKind.Text,
                    ChipInformationValue.FromText(datum.Value),
                    published.ResultId);
            }
        }

        /// <summary>添加运行时字段及来源链。</summary>
        private static void AddField(
            ICollection<ChipInformationFactValue> target,
            string fieldKey,
            string labelKey,
            ChipInformationValueKind valueKind,
            ChipInformationValue value,
            string sourceKey)
        {
            if (value == null)
            {
                return;
            }

            target.Add(new ChipInformationFactValue(
                fieldKey,
                labelKey,
                valueKind,
                value,
                null,
                new[]
                {
                    new ChipInformationProvenance(
                        sourceKey,
                        ChipInformationSourceKind.RuntimeResult,
                        ChipInformationContributionKind.Runtime,
                        value,
                        null,
                        true)
                }));
        }

        /// <summary>把非空文本转换为枚举值。</summary>
        private static ChipInformationValue StringValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : ChipInformationValue.FromEnum(value);
        }
    }
}
