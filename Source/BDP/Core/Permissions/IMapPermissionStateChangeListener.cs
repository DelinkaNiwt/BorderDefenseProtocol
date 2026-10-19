using RimWorld;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 监听地图许可状态变化的中性回调。
    /// 监听者自行决定收到许可变化后要刷新什么缓存或表现。
    /// </summary>
    public interface IMapPermissionStateChangeListener
    {
        /// <summary>监听者所属地图。</summary>
        Map Map { get; }

        /// <summary>通知指定许可键及派系的有效状态可能发生变化。</summary>
        /// <param name="permissionKey">发生变化的许可键。</param>
        /// <param name="faction">发生变化的许可归属派系。</param>
        void NotifyPermissionStateChanged(string permissionKey, Faction faction);
    }
}
