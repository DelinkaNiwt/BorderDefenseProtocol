using System;
using System.Collections.Generic;
using System.Globalization;
using BDP.Core.Information;
using Verse;

namespace BDP.Content.Information
{
    /// <summary>
    /// 统一格式化信息模型值，避免制造预览、详情和语义展示各自解释值来源。
    /// </summary>
    internal static class ChipInformationTextFormatter
    {
        /// <summary>尝试格式化文本、布尔、整数和普通数值。</summary>
        internal static bool TryFormatScalar(
            ChipInformationValue value,
            ChipInformationValueKind kind,
            out string text,
            IFormatProvider formatProvider = null)
        {
            text = string.Empty;
            if (value == null)
            {
                return false;
            }

            IFormatProvider provider = formatProvider ?? CultureInfo.CurrentCulture;
            switch (kind)
            {
                case ChipInformationValueKind.Text:
                    text = value.Text ?? string.Empty;
                    return true;
                case ChipInformationValueKind.Boolean:
                    text = (value.Boolean ? "BDP_Yes" : "BDP_No").Translate().ToString();
                    return true;
                case ChipInformationValueKind.Integer:
                    text = value.Integer.ToString(provider);
                    return true;
                case ChipInformationValueKind.Number:
                    text = value.Number.ToString("0.##", provider);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>按字段单位语言键套用已经格式化的值。</summary>
        internal static string ApplyUnit(string unitKey, string text)
        {
            return unitKey.NullOrEmpty()
                ? text
                : unitKey.Translate(text).ToString();
        }

        /// <summary>
        /// 格式化以秒为单位的持续时间。
        /// 有单位键时只把原始数值交给单位键；没有单位键时才使用默认“秒”文本。
        /// </summary>
        internal static string FormatDuration(
            float seconds,
            string unitKey,
            IFormatProvider formatProvider = null)
        {
            IFormatProvider provider = formatProvider ?? CultureInfo.CurrentCulture;
            string raw = seconds.ToString("0.##", provider);
            return unitKey.NullOrEmpty()
                ? "BDP_Window_TriggerAssembly_DurationValue".Translate(raw).ToString()
                : ApplyUnit(unitKey, raw);
        }

        /// <summary>格式化语义功能属性，供通用和专用展示器共用。</summary>
        internal static string FormatFeatureProperty(ChipInformationProperty property)
        {
            if (property == null || property.Value == null)
            {
                return string.Empty;
            }

            return FormatValueWithUnit(
                property.Value,
                property.ValueKind,
                property.UnitKey);
        }

        /// <summary>按类型和显式译名来源格式化任意信息值。</summary>
        internal static string FormatValue(
            ChipInformationValue value,
            ChipInformationValueKind? declaredKind,
            IFormatProvider formatProvider = null)
        {
            if (value == null)
            {
                return string.Empty;
            }

            ChipInformationValueKind kind = declaredKind ?? value.Kind;
            string scalar;
            IFormatProvider provider = formatProvider ?? CultureInfo.InvariantCulture;
            if (TryFormatScalar(value, kind, out scalar, provider))
            {
                return scalar;
            }

            if (kind == ChipInformationValueKind.Percentage)
            {
                return (Math.Abs(value.Number) <= 1.5f
                    ? value.Number * 100f
                    : value.Number).ToString("0.#", provider) + "%";
            }

            if (kind == ChipInformationValueKind.Duration)
            {
                return FormatDuration(value.Number, null, provider);
            }

            if (kind == ChipInformationValueKind.Enum)
            {
                switch (value.DisplaySourceKind)
                {
                    case ChipInformationValueDisplaySourceKind.TranslationKey:
                        return string.IsNullOrWhiteSpace(value.DisplayKey)
                            ? value.EnumKey ?? string.Empty
                            : value.DisplayKey.Translate().ToString();
                    case ChipInformationValueDisplaySourceKind.DefReference:
                        return ChipInformationDefLabelResolverRegistry.Resolve(value);
                    default:
                        return value.EnumKey ?? string.Empty;
                }
            }

            if (kind == ChipInformationValueKind.List)
            {
                List<string> values = new List<string>();
                if (value.Items != null)
                {
                    for (int index = 0; index < value.Items.Count; index++)
                    {
                        values.Add(FormatValue(value.Items[index], null, provider));
                    }
                }

                return string.Join("、", values.ToArray());
            }

            if (kind == ChipInformationValueKind.Record)
            {
                List<string> fields = new List<string>();
                if (value.Fields != null)
                {
                    for (int index = 0; index < value.Fields.Count; index++)
                    {
                        ChipInformationValueField field = value.Fields[index];
                        if (field == null || field.Value == null)
                        {
                            continue;
                        }

                        string label = field.LabelKey.NullOrEmpty()
                            ? field.Key ?? string.Empty
                            : field.LabelKey.Translate().ToString();
                        string fieldValue = FormatValueWithUnit(
                            field.Value,
                            field.Value.Kind,
                            field.UnitKey,
                            provider);
                        fields.Add(label + "：" + fieldValue);
                    }
                }

                return string.Join("，", fields.ToArray());
            }

            return value.Number.ToString("0.##", provider);
        }

        /// <summary>格式化字段值并只应用一次字段单位。</summary>
        internal static string FormatValueWithUnit(
            ChipInformationValue value,
            ChipInformationValueKind? declaredKind,
            string unitKey,
            IFormatProvider formatProvider = null)
        {
            if (value == null)
            {
                return string.Empty;
            }

            ChipInformationValueKind kind = declaredKind ?? value.Kind;
            IFormatProvider provider = formatProvider ?? CultureInfo.InvariantCulture;
            if (kind == ChipInformationValueKind.Duration)
            {
                return FormatDuration(value.Number, unitKey, provider);
            }

            return ApplyUnit(
                unitKey,
                FormatValue(value, kind, provider));
        }

    }
}
