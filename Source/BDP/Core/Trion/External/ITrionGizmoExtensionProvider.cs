using System.Collections.Generic;

namespace BDP.Core.Trion.External
{
    /// <summary>
    /// Trion 状态条扩展徽标提供接口。
    /// 其它系统只通过这个接口向右上角扩展区投放附加信息。
    /// </summary>
    public interface ITrionGizmoExtensionProvider
    {
        /// <summary>
        /// 获取当前上下文可显示的徽标集合。
        /// </summary>
        IEnumerable<TrionGizmoExtensionBadge> GetBadges(TrionGizmoExtensionContext context);
    }
}
