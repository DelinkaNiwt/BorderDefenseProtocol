using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 报告一个只绑定单个 Pawn 的自身许可提供器。
    /// Core 只关心提供者归属、许可键和当前有效性，不解释许可业务含义。
    /// </summary>
    public interface IPawnPermissionProvider
    {
        /// <summary>发布许可的 Pawn。</summary>
        Pawn Pawn { get; }

        /// <summary>提供的稳定许可键。</summary>
        string PermissionKey { get; }

        /// <summary>当前是否实际提供许可。</summary>
        bool IsActive { get; }
    }
}
