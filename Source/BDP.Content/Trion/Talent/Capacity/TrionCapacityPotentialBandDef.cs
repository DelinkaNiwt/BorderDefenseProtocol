using Verse;

namespace BDP.Content.Trion.Talent.Capacity
{
    /// <summary>
    /// 玩家可见的 Trion 容量潜质模糊档位。
    /// </summary>
    public sealed class TrionCapacityPotentialBandDef : Def
    {
        /// <summary>档位包含的最低容量。</summary>
        public int minimumCapacity;

        /// <summary>档位包含的最高容量。</summary>
        public int maximumCapacity;

        /// <summary>判断指定容量是否属于本档。</summary>
        public bool Contains(int capacity)
        {
            return capacity >= minimumCapacity && capacity <= maximumCapacity;
        }
    }
}
