using BDP.Core.CombatBody;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Bootstrap
{
    /// <summary>
    /// CombatBody Trigger 按钮引导器。
    /// 它只负责把 CombatBody 的正式按钮 provider 注册到 Trigger 外部按钮口。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CombatBodyTriggerGizmoBootstrap
    {
        /// <summary>
        /// 模组装载时自动执行一次。
        /// </summary>
        static CombatBodyTriggerGizmoBootstrap()
        {
            TriggerExternalGizmoRegistry.Register(new CombatBodyTriggerGizmoProvider());
        }
    }
}
