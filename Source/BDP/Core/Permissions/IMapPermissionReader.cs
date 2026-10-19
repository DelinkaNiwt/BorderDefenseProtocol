using RimWorld;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 读取地图许可的中性入口。
    /// </summary>
    public interface IMapPermissionReader
    {
        /// <summary>
        /// 查询指定地图和派系是否至少拥有一个有效许可提供者。
        /// </summary>
        /// <param name="map">目标地图。</param>
        /// <param name="faction">目标派系。</param>
        /// <param name="permissionKey">许可键。</param>
        /// <returns>存在有效提供者时为 true。</returns>
        bool HasPermission(Map map, Faction faction, string permissionKey);
    }
}
