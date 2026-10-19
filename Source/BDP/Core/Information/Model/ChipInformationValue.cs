using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息字段的类型化值容器，不暴露原始游戏对象。
    /// </summary>
    public sealed class ChipInformationValue
    {
        /// <summary>当前值类型。</summary>
        public ChipInformationValueKind Kind { get; private set; }

        /// <summary>文本值。</summary>
        public string Text { get; private set; }

        /// <summary>布尔值。</summary>
        public bool Boolean { get; private set; }

        /// <summary>整数值。</summary>
        public int Integer { get; private set; }

        /// <summary>普通数值或百分比数值。</summary>
        public float Number { get; private set; }

        /// <summary>枚举键。</summary>
        public string EnumKey { get; private set; }

        /// <summary>值显示所走的译名来源。</summary>
        public ChipInformationValueDisplaySourceKind DisplaySourceKind { get; private set; }

        /// <summary>Keyed（文本键）显示键，或留空。</summary>
        public string DisplayKey { get; private set; }

        /// <summary>Def（定义对象）运行时类型的程序集限定名。</summary>
        public string DefinitionTypeKey { get; private set; }

        /// <summary>Def（定义对象）名称键。</summary>
        public string DefinitionName { get; private set; }

        /// <summary>列表值。</summary>
        public IReadOnlyList<ChipInformationValue> Items { get; private set; }

        /// <summary>结构化记录字段。</summary>
        public IReadOnlyList<ChipInformationValueField> Fields { get; private set; }

        /// <summary>创建类型化值。</summary>
        private ChipInformationValue(ChipInformationValueKind kind)
        {
            Kind = kind;
            Items = new List<ChipInformationValue>().AsReadOnly();
            Fields = new List<ChipInformationValueField>().AsReadOnly();
        }

        /// <summary>创建文本值。</summary>
        public static ChipInformationValue FromText(string value)
        {
            return new ChipInformationValue(ChipInformationValueKind.Text)
            {
                Text = value
            };
        }

        /// <summary>创建布尔值。</summary>
        public static ChipInformationValue FromBoolean(bool value)
        {
            return new ChipInformationValue(ChipInformationValueKind.Boolean)
            {
                Boolean = value
            };
        }

        /// <summary>创建整数值。</summary>
        public static ChipInformationValue FromInteger(int value)
        {
            return new ChipInformationValue(ChipInformationValueKind.Integer)
            {
                Integer = value
            };
        }

        /// <summary>创建普通数值。</summary>
        public static ChipInformationValue FromNumber(float value)
        {
            return new ChipInformationValue(ChipInformationValueKind.Number)
            {
                Number = value
            };
        }

        /// <summary>创建百分比数值。</summary>
        public static ChipInformationValue FromPercentage(float value)
        {
            return new ChipInformationValue(ChipInformationValueKind.Percentage)
            {
                Number = value
            };
        }

        /// <summary>创建持续时间数值，单位为秒。</summary>
        public static ChipInformationValue FromDuration(float seconds)
        {
            return new ChipInformationValue(ChipInformationValueKind.Duration)
            {
                Number = seconds
            };
        }

        /// <summary>创建原始枚举值；不会把枚举名自动当作 Keyed（文本键）。</summary>
        public static ChipInformationValue FromEnum(string enumKey)
        {
            return new ChipInformationValue(ChipInformationValueKind.Enum)
            {
                EnumKey = enumKey,
                DisplaySourceKind = ChipInformationValueDisplaySourceKind.Raw
            };
        }

        /// <summary>创建值与显示名相同的 Keyed（文本键）枚举值。</summary>
        public static ChipInformationValue FromTranslationKey(string translationKey)
        {
            return FromLocalizedEnum(translationKey, translationKey);
        }

        /// <summary>创建原始枚举值及其显式 Keyed（文本键）显示名。</summary>
        public static ChipInformationValue FromLocalizedEnum(
            string enumKey,
            string translationKey)
        {
            if (string.IsNullOrWhiteSpace(translationKey))
            {
                return FromEnum(enumKey);
            }

            return new ChipInformationValue(ChipInformationValueKind.Enum)
            {
                EnumKey = enumKey,
                DisplaySourceKind = ChipInformationValueDisplaySourceKind.TranslationKey,
                DisplayKey = translationKey
            };
        }

        /// <summary>创建 Def（定义对象）引用；仅保存类型身份和 DefName（定义键）。</summary>
        public static ChipInformationValue FromDefReference(Type definitionType, string defName)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return null;
            }

            if (definitionType == null)
            {
                return FromEnum(defName);
            }

            return new ChipInformationValue(ChipInformationValueKind.Enum)
            {
                DisplaySourceKind = ChipInformationValueDisplaySourceKind.DefReference,
                DefinitionTypeKey = definitionType.AssemblyQualifiedName,
                DefinitionName = defName
            };
        }

        /// <summary>创建只读列表值。</summary>
        public static ChipInformationValue FromList(IEnumerable<ChipInformationValue> values)
        {
            List<ChipInformationValue> items = new List<ChipInformationValue>();
            if (values != null)
            {
                foreach (ChipInformationValue value in values)
                {
                    if (value != null)
                    {
                        items.Add(value);
                    }
                }
            }

            return new ChipInformationValue(ChipInformationValueKind.List)
            {
                Items = items.AsReadOnly()
            };
        }

        /// <summary>创建带稳定字段键的只读结构化记录。</summary>
        public static ChipInformationValue FromRecord(
            IEnumerable<ChipInformationValueField> fields)
        {
            List<ChipInformationValueField> result =
                new List<ChipInformationValueField>();
            if (fields != null)
            {
                foreach (ChipInformationValueField field in fields)
                {
                    if (field != null
                        && !string.IsNullOrWhiteSpace(field.Key)
                        && field.Value != null)
                    {
                        result.Add(field);
                    }
                }
            }

            return new ChipInformationValue(ChipInformationValueKind.Record)
            {
                Fields = result.AsReadOnly()
            };
        }
    }
}
