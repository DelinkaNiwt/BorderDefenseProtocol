using BDP.Core.CombatBody.Wounds;
using Verse;

namespace BDP.Core.Bootstrap
{
    /// <summary>
    /// 战斗体伤口信息显示引导器。
    /// 只负责把 BDP 的伤口提示组件接入原版伤口 Def，不承载伤口业务规则。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CombatBodyWoundInfoBootstrap
    {
        /// <summary>
        /// 模组装载时执行一次伤口提示组件注入。
        /// </summary>
        static CombatBodyWoundInfoBootstrap()
        {
            CombatBodyWoundTrionInfoInjector.Apply();
        }
    }
}
