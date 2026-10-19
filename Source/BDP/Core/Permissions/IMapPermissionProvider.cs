using RimWorld;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 报告一个地图范围许可的中性提供器。
    /// Core 只关心提供者的归属、许可键和当前有效性，不解释许可业务含义。
    /// </summary>
    public interface IMapPermissionProvider
    {
        /// <summary>提供者所属地图。</summary>
        Map Map { get; }

        /// <summary>许可归属派系。</summary>
        Faction Faction { get; }

        /// <summary>提供的稳定许可键。</summary>
        string PermissionKey { get; }

        /// <summary>当前是否实际提供许可。</summary>
        bool IsActive { get; }
    }
}
