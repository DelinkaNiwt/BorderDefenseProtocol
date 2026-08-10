using Verse;

namespace BDP.Core.Bootstrap
{
    /// <summary>
    /// Pawn 宿主接线引导器。
    /// 它只负责在 Def 全部加载完成后，把 BDP 需要的 Pawn ThingComp 宿主接回人形 Pawn Def。
    /// 这里不承载任何业务规则，只解决“正式宿主应在何时接线”这个启动时机问题。
    /// </summary>
    [StaticConstructorOnStartup]
    /// <summary>
    /// Pawn 宿主自动接线启动器。
    /// </summary>
    public static class PawnCompBootstrap
    {
        /// <summary>
        /// 在 RimWorld 完成 Def 解析后执行一次。
        /// 此时再注入 Pawn Def.comps，后续新局与读档创建的 Pawn 实例才能自然拿到正式宿主 Comp。
        /// </summary>
        static PawnCompBootstrap()
        {
            PawnTrionCompInjector.Apply();
            PawnCombatBodyCompInjector.Apply();
        }
    }
}
