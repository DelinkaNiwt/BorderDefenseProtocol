using System.Collections.Generic;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体额外按钮提供器。
    /// 这是主模组正式开放给外部模组的扩展口。
    /// </summary>
    public interface ITriggerExternalGizmoProvider
    {
        /// <summary>
        /// 基于当前触发体上下文返回要追加的按钮。
        /// 提供器应只通过正式上下文取值，不直接摸主模组内部实现。
        /// </summary>
        IEnumerable<Gizmo> BuildGizmos(TriggerExternalGizmoContext context);
    }
}
