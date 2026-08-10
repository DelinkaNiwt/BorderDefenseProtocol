using System;

namespace BDP.Core.Trion
{
    /// <summary>
    /// `Trion` 持续消耗登记键。
    /// 该值对象只表达资源账本的中性身份，不携带任何 `Trigger` 业务语义。
    /// </summary>
    public readonly struct TrionDrainKey : IEquatable<TrionDrainKey>
    {
        /// <summary>
        /// 消耗来源所属领域。
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// 消耗来源所属通道。
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// 消耗来源的序号。
        /// 无序号时使用 `-1`。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 消耗来源的附加标签。
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// 按显式字段构造一个稳定的持续消耗键。
        /// </summary>
        public TrionDrainKey(string domain, string channel, int index, string tag)
        {
            Domain = domain ?? string.Empty;
            Channel = channel ?? string.Empty;
            Index = index;
            Tag = tag ?? string.Empty;
        }

        /// <summary>
        /// 判断两个键的四元组身份是否完全一致。
        /// </summary>
        public bool Equals(TrionDrainKey other)
        {
            return string.Equals(Domain, other.Domain, StringComparison.Ordinal)
                && string.Equals(Channel, other.Channel, StringComparison.Ordinal)
                && Index == other.Index
                && string.Equals(Tag, other.Tag, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断当前键是否与另一个对象表示同一身份。
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is TrionDrainKey other && Equals(other);
        }

        /// <summary>
        /// 生成与四元组身份一致的哈希值。
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (Domain != null ? StringComparer.Ordinal.GetHashCode(Domain) : 0);
                hash = (hash * 31) + (Channel != null ? StringComparer.Ordinal.GetHashCode(Channel) : 0);
                hash = (hash * 31) + Index;
                hash = (hash * 31) + (Tag != null ? StringComparer.Ordinal.GetHashCode(Tag) : 0);
                return hash;
            }
        }

        /// <summary>
        /// 返回仅用于日志诊断的稳定字符串。
        /// 该字符串不作为存储身份来源。
        /// </summary>
        public override string ToString()
        {
            return Domain + ":" + Channel + ":" + Index + ":" + Tag;
        }

        /// <summary>
        /// 判断两个键是否表示同一持续消耗身份。
        /// </summary>
        public static bool operator ==(TrionDrainKey left, TrionDrainKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 判断两个键是否表示不同持续消耗身份。
        /// </summary>
        public static bool operator !=(TrionDrainKey left, TrionDrainKey right)
        {
            return !left.Equals(right);
        }
    }
}
