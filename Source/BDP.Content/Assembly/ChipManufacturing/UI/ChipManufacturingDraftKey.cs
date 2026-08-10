using System;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>一条制造草稿路径由主分类与可空职业共同确定。</summary>
    public struct ChipManufacturingDraftKey : IEquatable<ChipManufacturingDraftKey>
    {
        /// <summary>主分类稳定 DefName。</summary>
        public string CategoryDefName { get; }

        /// <summary>可空职业稳定 DefName。</summary>
        public string ProfessionDefName { get; }

        /// <summary>建立一条草稿路径键。</summary>
        public ChipManufacturingDraftKey(string categoryDefName, string professionDefName)
        {
            CategoryDefName = categoryDefName;
            ProfessionDefName = professionDefName;
        }

        /// <summary>按两个稳定 DefName 比较。</summary>
        public bool Equals(ChipManufacturingDraftKey other)
        {
            return string.Equals(CategoryDefName, other.CategoryDefName, StringComparison.Ordinal)
                && string.Equals(ProfessionDefName, other.ProfessionDefName, StringComparison.Ordinal);
        }

        /// <summary>按草稿键类型比较。</summary>
        public override bool Equals(object obj)
        {
            return obj is ChipManufacturingDraftKey other && Equals(other);
        }

        /// <summary>为草稿字典生成稳定哈希。</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((CategoryDefName != null ? CategoryDefName.GetHashCode() : 0) * 397)
                    ^ (ProfessionDefName != null ? ProfessionDefName.GetHashCode() : 0);
            }
        }
    }
}
