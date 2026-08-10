using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 身体禁用同步的瞬时缓存。
    /// 它只保存“最近同步到哪一版”这种运行时缓存，不保存正式真值。
    /// </summary>
    internal sealed class TriggerDisableSyncCache
    {
        /// <summary>
        /// 最近一次完成同步时对应的身体约束版本号。
        /// </summary>
        public int LastSyncedVersion = -1;

        /// <summary>
        /// 最近一次完成同步时对应的宿主 Pawn。
        /// </summary>
        public Pawn LastSyncedPawn;

        /// <summary>
        /// 当前缓存是否至少完成过一次同步。
        /// </summary>
        public bool Initialized;
    }
}
