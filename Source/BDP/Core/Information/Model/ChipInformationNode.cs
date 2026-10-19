using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 对外芯片信息树中的统一节点；一般信息、功能、构型上下文和关系共用此结构。
    /// </summary>
    public sealed class ChipInformationNode
    {
        /// <summary>节点稳定键。</summary>
        public string Key { get; private set; }

        /// <summary>节点语义类型键。</summary>
        public string KindKey { get; private set; }

        /// <summary>来源定义键；没有定义来源时为空。</summary>
        public string DefinitionKey { get; private set; }

        /// <summary>节点名称语言键；没有名称时为空。</summary>
        public string LabelKey { get; private set; }

        /// <summary>同级节点的稳定排序值。</summary>
        public int Order { get; private set; }

        /// <summary>节点上的客观类型化属性。</summary>
        public IReadOnlyList<ChipInformationProperty> Properties { get; private set; }

        /// <summary>节点的子节点。</summary>
        public IReadOnlyList<ChipInformationNode> Children { get; private set; }

        /// <summary>创建不可变信息节点。</summary>
        public ChipInformationNode(
            string key,
            string kindKey,
            string definitionKey,
            string labelKey,
            int order,
            IEnumerable<ChipInformationProperty> properties,
            IEnumerable<ChipInformationNode> children)
        {
            Key = key;
            KindKey = kindKey;
            DefinitionKey = definitionKey;
            LabelKey = labelKey;
            Order = order;
            Properties = Copy(properties);
            Children = Copy(children);
        }

        /// <summary>复制属性集合并过滤空项。</summary>
        private static IReadOnlyList<ChipInformationProperty> Copy(
            IEnumerable<ChipInformationProperty> source)
        {
            List<ChipInformationProperty> result = new List<ChipInformationProperty>();
            if (source != null)
            {
                foreach (ChipInformationProperty item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>复制子节点集合并过滤空项。</summary>
        private static IReadOnlyList<ChipInformationNode> Copy(
            IEnumerable<ChipInformationNode> source)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (source != null)
            {
                foreach (ChipInformationNode item in source)
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
