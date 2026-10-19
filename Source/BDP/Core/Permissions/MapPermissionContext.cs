using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 芯片业务接收许可时看到的只读事实上下文。
    /// </summary>
    public sealed class MapPermissionContext
    {
        /// <summary>
        /// 使用当前 Pawn 和结果标识建立许可上下文。
        /// </summary>
        /// <param name="pawn">当前芯片使用者。</param>
        /// <param name="resultId">当前表达结果标识。</param>
        public MapPermissionContext(Pawn pawn, string resultId)
            : this(pawn, resultId, null)
        {
        }

        /// <summary>使用当前 Pawn、结果标识和显式许可声明建立上下文。</summary>
        public MapPermissionContext(
            Pawn pawn,
            string resultId,
            IReadOnlyList<string> declaredPermissionKeys)
        {
            Pawn = pawn;
            Map = pawn != null ? pawn.Map : null;
            Faction = pawn != null ? pawn.Faction : null;
            ResultId = resultId;
            DeclaredPermissionKeys = declaredPermissionKeys ?? new List<string>();
            Reader = MapPermissionAccess.GetReader(Map);
            PawnReader = MapPermissionAccess.GetPawnReader(Map);
        }

        /// <summary>当前芯片使用者。</summary>
        public Pawn Pawn { get; private set; }

        /// <summary>当前芯片使用者所在地图。</summary>
        public Map Map { get; private set; }

        /// <summary>当前芯片使用者派系。</summary>
        public Faction Faction { get; private set; }

        /// <summary>当前表达结果稳定标识。</summary>
        public string ResultId { get; private set; }

        /// <summary>当前结果显式声明接收的许可键。</summary>
        public IReadOnlyList<string> DeclaredPermissionKeys { get; private set; }

        /// <summary>当前地图许可读取器；缺失时为 null。</summary>
        public IMapPermissionReader Reader { get; private set; }

        /// <summary>当前 Pawn 自身许可读取器；缺失时为 null。</summary>
        public IPawnPermissionReader PawnReader { get; private set; }

        /// <summary>
        /// 读取当前 Pawn 所属地图上的许可。
        /// </summary>
        /// <param name="permissionKey">许可键。</param>
        /// <returns>许可有效时为 true。</returns>
        public bool HasPermission(string permissionKey)
        {
            return Reader != null && Reader.HasPermission(Map, Faction, permissionKey);
        }

        /// <summary>
        /// 读取当前 Pawn 自己发布的许可；不会读取同地图或同派系的地图许可。
        /// </summary>
        /// <param name="permissionKey">许可键。</param>
        /// <returns>当前 Pawn 存在有效自身许可时为 true。</returns>
        public bool HasSelfPermission(string permissionKey)
        {
            return PawnReader != null && PawnReader.HasSelfPermission(Map, Pawn, permissionKey);
        }
    }
}
