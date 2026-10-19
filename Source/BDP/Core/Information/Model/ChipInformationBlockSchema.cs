using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 一个公开信息块的结构声明。
    /// </summary>
    public sealed class ChipInformationBlockSchema
    {
        /// <summary>稳定信息块键。</summary>
        public string BlockKey { get; private set; }

        /// <summary>信息块语义类型。</summary>
        public string Kind { get; private set; }

        /// <summary>信息块标题语言包键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>稳定排序值。</summary>
        public int Order { get; private set; }

        /// <summary>字段声明集合。</summary>
        public IReadOnlyList<ChipInformationFieldSchema> Fields { get; private set; }

        /// <summary>嵌套子块结构声明集合。</summary>
        public IReadOnlyList<ChipInformationBlockSchema> ChildBlocks { get; private set; }

        /// <summary>按稳定字段键查询字段声明。</summary>
        public bool TryGetField(
            string fieldKey,
            out ChipInformationFieldSchema field)
        {
            field = null;
            if (string.IsNullOrWhiteSpace(fieldKey))
            {
                return false;
            }

            for (int index = 0; index < Fields.Count; index++)
            {
                ChipInformationFieldSchema candidate = Fields[index];
                if (candidate != null
                    && string.Equals(candidate.FieldKey, fieldKey, StringComparison.Ordinal))
                {
                    field = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>创建信息块声明。</summary>
        public ChipInformationBlockSchema(
            string blockKey,
            string kind,
            string labelKey,
            int order,
            IEnumerable<ChipInformationFieldSchema> fields,
            IEnumerable<ChipInformationBlockSchema> childBlocks = null)
        {
            BlockKey = blockKey;
            Kind = kind;
            LabelKey = labelKey;
            Order = order;
            Fields = Copy(fields);
            ChildBlocks = CopyBlocks(childBlocks);
        }

        /// <summary>复制字段声明集合。</summary>
        private static IReadOnlyList<ChipInformationFieldSchema> Copy(
            IEnumerable<ChipInformationFieldSchema> source)
        {
            List<ChipInformationFieldSchema> result = new List<ChipInformationFieldSchema>();
            if (source != null)
            {
                foreach (ChipInformationFieldSchema item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>复制子块声明集合。</summary>
        private static IReadOnlyList<ChipInformationBlockSchema> CopyBlocks(
            IEnumerable<ChipInformationBlockSchema> source)
        {
            List<ChipInformationBlockSchema> result = new List<ChipInformationBlockSchema>();
            if (source != null)
            {
                foreach (ChipInformationBlockSchema item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>按稳定键查询当前块下的子块声明。</summary>
        public bool TryGetBlock(string blockKey, out ChipInformationBlockSchema block)
        {
            block = null;
            if (string.IsNullOrWhiteSpace(blockKey))
            {
                return false;
            }

            for (int index = 0; index < ChildBlocks.Count; index++)
            {
                ChipInformationBlockSchema candidate = ChildBlocks[index];
                if (candidate != null
                    && string.Equals(candidate.BlockKey, blockKey, StringComparison.Ordinal))
                {
                    block = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
