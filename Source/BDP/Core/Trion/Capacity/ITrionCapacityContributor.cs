using Verse;

namespace BDP.Core.Trion.Capacity
{
    /// <summary>
    /// Trion 最大容量贡献提供器的中性接口。
    /// Core 只读取额外容量，不认识具体装备、类别或业务来源。
    /// </summary>
    public interface ITrionCapacityContributor
    {
        /// <summary>
        /// 返回指定 Pawn 当前应获得的额外 Trion 容量。
        /// </summary>
        float GetAdditionalCapacity(Pawn pawn);
    }
}
