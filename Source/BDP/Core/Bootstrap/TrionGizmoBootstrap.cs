using BDP.Core.CombatBody;
using BDP.Core.Trion.External;
using Verse;

namespace BDP.Core.Bootstrap
{
    /// <summary>
    /// Trion 状态卡扩展引导器。
    /// 只负责把主模组自己的正式扩展 provider 注册到 Trion GUI 扩展口。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class TrionGizmoBootstrap
    {
        /// <summary>
        /// 模组装载时自动执行一次。
        /// </summary>
        static TrionGizmoBootstrap()
        {
            TrionGizmoExtensionRegistry.Register(new CombatBodyTrionGizmoExtensionProvider());
        }
    }
}
