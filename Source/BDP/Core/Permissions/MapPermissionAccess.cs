using RimWorld;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 地图许可的安全静态访问入口。
    /// </summary>
    public static class MapPermissionAccess
    {
        /// <summary>读取指定地图的许可组件。</summary>
        /// <param name="map">目标地图。</param>
        /// <returns>许可读取器；不可用时为 null。</returns>
        public static IMapPermissionReader GetReader(Map map)
        {
            return map?.GetComponent<MapPermissionMapComponent>();
        }

        /// <summary>查询指定地图和派系是否拥有许可。</summary>
        public static bool HasPermission(Map map, Faction faction, string permissionKey)
        {
            IMapPermissionReader reader = GetReader(map);
            return reader != null && reader.HasPermission(map, faction, permissionKey);
        }

        /// <summary>读取指定地图的 Pawn 自身许可组件。</summary>
        public static IPawnPermissionReader GetPawnReader(Map map)
        {
            return map?.GetComponent<MapPermissionMapComponent>();
        }

        /// <summary>查询指定地图中的 Pawn 自身许可。</summary>
        public static bool HasSelfPermission(Map map, Pawn pawn, string permissionKey)
        {
            IPawnPermissionReader reader = GetPawnReader(map);
            return reader != null && reader.HasSelfPermission(map, pawn, permissionKey);
        }
    }
}
