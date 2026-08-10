using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// CombatBody 对外接口获取面。
    /// 外部系统统一从这里拿 CombatBody 的正式读写口，不再自己摸宿主 Comp 类型。
    /// </summary>
    public static class CombatBodySurfaceAccess
    {
        /// <summary>
        /// 读取 Pawn 身上的 CombatBody 只读口。
        /// </summary>
        public static ICombatBodyReader ResolveReader(Pawn pawn)
        {
            CompCombatBodyHost comp = ResolveComp(pawn);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 读取 Pawn 身上的 CombatBody 请求口。
        /// </summary>
        public static ICombatBodyCommands ResolveCommands(Pawn pawn)
        {
            CompCombatBodyHost comp = ResolveComp(pawn);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 读取 Pawn 身上的 CombatBody 事件口。
        /// </summary>
        public static ICombatBodyEvents ResolveEvents(Pawn pawn)
        {
            CompCombatBodyHost comp = ResolveComp(pawn);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 统一定位 Pawn 身上的 CombatBody 宿主 Comp。
        /// </summary>
        private static CompCombatBodyHost ResolveComp(Pawn pawn)
        {
            return pawn?.GetComp<CompCombatBodyHost>();
        }
    }
}
