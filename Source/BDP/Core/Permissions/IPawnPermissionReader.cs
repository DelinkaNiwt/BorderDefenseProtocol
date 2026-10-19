using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 读取 Pawn 自身许可的中性入口。
    /// 与地图许可读取契约分离，避免改变既有地图许可接口的兼容性。
    /// </summary>
    public interface IPawnPermissionReader
    {
        /// <summary>
        /// 查询指定地图中指定 Pawn 是否拥有自身许可。
        /// </summary>
        /// <param name="map">目标地图。</param>
        /// <param name="pawn">目标 Pawn。</param>
        /// <param name="permissionKey">许可键。</param>
        /// <returns>该 Pawn 存在有效自身许可提供者时为 true。</returns>
        bool HasSelfPermission(Map map, Pawn pawn, string permissionKey);
    }
}
