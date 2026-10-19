using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 一条通用规则对信息模型的贡献。
    /// </summary>
    public sealed class ChipInformationContribution
    {
        /// <summary>目标信息块键。</summary>
        public string BlockKey { get; private set; }

        /// <summary>父信息块键；根块为空。</summary>
        public string ParentBlockKey { get; private set; }

        /// <summary>目标信息块语义类型。</summary>
        public string Kind { get; private set; }

        /// <summary>信息块标题语言包键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>信息块稳定排序值。</summary>
        public int Order { get; private set; }

        /// <summary>字段结构声明，可为空。</summary>
        public ChipInformationFieldSchema FieldSchema { get; private set; }

        /// <summary>通用节点属性，可为空。</summary>
        public ChipInformationProperty Property { get; private set; }

        /// <summary>创建一条信息贡献。</summary>
        public ChipInformationContribution(
            string blockKey,
            string kind,
            string labelKey,
            int order,
            ChipInformationFieldSchema fieldSchema,
            ChipInformationProperty property,
            string parentBlockKey = null)
        {
            BlockKey = blockKey;
            ParentBlockKey = parentBlockKey;
            Kind = kind;
            LabelKey = labelKey;
            Order = order;
            FieldSchema = fieldSchema;
            Property = property;
        }
    }
}
