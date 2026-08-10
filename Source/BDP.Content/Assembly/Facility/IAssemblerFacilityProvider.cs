using System.Collections.Generic;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 装配台设施连接读取口。
    /// 它隔离窗口和事务层对原版 Facility 连接的读取方式。
    /// </summary>
    internal interface IAssemblerFacilityProvider
    {
        /// <summary>
        /// 读取所有连接容器中当前可用的芯片。
        /// </summary>
        IReadOnlyList<Thing> GetAvailableChips();

        /// <summary>
        /// 从连接容器中取出指定芯片。
        /// </summary>
        bool TryTakeChip(Thing chip);

        /// <summary>
        /// 把指定芯片放回任意可用连接容器。
        /// </summary>
        bool TryStoreChip(Thing chip);

        /// <summary>
        /// 把无法回存的芯片落到装配台附近。
        /// </summary>
        void DropChipNearAssembler(Thing chip);

        /// <summary>
        /// 判断连接容器是否还有可回存空间。
        /// </summary>
        bool HasStorageSpace();
    }
}
