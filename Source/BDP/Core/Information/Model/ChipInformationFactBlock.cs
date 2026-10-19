using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 输入侧的中性结构化信息块。
    /// 块只保存原始层级、字段和顺序，不保存解释或计算结果。
    /// </summary>
    public sealed class ChipInformationFactBlock
    {
        /// <summary>稳定信息块键。</summary>
        public string BlockKey { get; private set; }

        /// <summary>信息块语义类型。</summary>
        public string Kind { get; private set; }

        /// <summary>信息块标题语言包键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>稳定排序值。</summary>
        public int Order { get; private set; }

        /// <summary>当前块的字段事实。</summary>
        public IReadOnlyList<ChipInformationFactValue> Fields { get; private set; }

        /// <summary>当前块的嵌套子块。</summary>
        public IReadOnlyList<ChipInformationFactBlock> ChildBlocks { get; private set; }

        /// <summary>创建中性结构化信息块。</summary>
        public ChipInformationFactBlock(
            string blockKey,
            string kind,
            string labelKey,
            int order,
            IEnumerable<ChipInformationFactValue> fields,
            IEnumerable<ChipInformationFactBlock> childBlocks)
        {
            BlockKey = blockKey;
            Kind = kind;
            LabelKey = labelKey;
            Order = order;
            Fields = CopyFields(fields);
            ChildBlocks = CopyBlocks(childBlocks);
        }

        /// <summary>复制字段集合。</summary>
        private static IReadOnlyList<ChipInformationFactValue> CopyFields(
            IEnumerable<ChipInformationFactValue> source)
        {
            List<ChipInformationFactValue> result = new List<ChipInformationFactValue>();
            if (source != null)
            {
                foreach (ChipInformationFactValue item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>复制子块集合。</summary>
        private static IReadOnlyList<ChipInformationFactBlock> CopyBlocks(
            IEnumerable<ChipInformationFactBlock> source)
        {
            List<ChipInformationFactBlock> result = new List<ChipInformationFactBlock>();
            if (source != null)
            {
                foreach (ChipInformationFactBlock item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }
    }
}
