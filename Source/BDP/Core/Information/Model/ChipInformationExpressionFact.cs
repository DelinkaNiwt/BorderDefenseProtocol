using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 一个表达来源的中性事实。
    /// </summary>
    public sealed class ChipInformationExpressionFact
    {
        /// <summary>表达来源稳定键。</summary>
        public string ExpressionKey { get; private set; }

        /// <summary>表达来源名称语言包键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>表达通道键，例如 rangedAttack 或 ability。</summary>
        public string ChannelKey { get; private set; }

        /// <summary>表达来源键，用于来源链追踪。</summary>
        public string SourceKey { get; private set; }

        /// <summary>表达来源类别。</summary>
        public ChipInformationSourceKind SourceKind { get; private set; }

        /// <summary>当前来源的武器模式键。</summary>
        public string WeaponModeKey { get; private set; }

        /// <summary>当前来源所属形态键。</summary>
        public string ModeKey { get; private set; }

        /// <summary>当前来源所属姿态键。</summary>
        public string StanceKey { get; private set; }

        /// <summary>当前表达来源是否可用。</summary>
        public bool IsAvailable { get; private set; }

        /// <summary>表达来源的公开字段事实。</summary>
        public IReadOnlyList<ChipInformationFactValue> Fields { get; private set; }

        /// <summary>表达来源的递归子表达事实。</summary>
        public IReadOnlyList<ChipInformationExpressionFact> Children { get; private set; }

        /// <summary>表达来源下的结构化信息块事实。</summary>
        public IReadOnlyList<ChipInformationFactBlock> Blocks { get; private set; }

        /// <summary>创建表达事实。</summary>
        public ChipInformationExpressionFact(
            string expressionKey,
            string labelKey,
            string channelKey,
            string sourceKey,
            ChipInformationSourceKind sourceKind,
            string weaponModeKey,
            string modeKey,
            string stanceKey,
            bool isAvailable,
            IEnumerable<ChipInformationFactValue> fields,
            IEnumerable<ChipInformationExpressionFact> children,
            IEnumerable<ChipInformationFactBlock> blocks = null)
        {
            ExpressionKey = expressionKey;
            LabelKey = labelKey;
            ChannelKey = channelKey;
            SourceKey = sourceKey;
            SourceKind = sourceKind;
            WeaponModeKey = weaponModeKey;
            ModeKey = modeKey;
            StanceKey = stanceKey;
            IsAvailable = isAvailable;
            Fields = CopyFields(fields);
            Children = CopyChildren(children);
            Blocks = CopyBlocks(blocks);
        }

        /// <summary>复制字段事实。</summary>
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

        /// <summary>复制子表达事实。</summary>
        private static IReadOnlyList<ChipInformationExpressionFact> CopyChildren(
            IEnumerable<ChipInformationExpressionFact> source)
        {
            List<ChipInformationExpressionFact> result = new List<ChipInformationExpressionFact>();
            if (source != null)
            {
                foreach (ChipInformationExpressionFact item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>复制结构化信息块。</summary>
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
