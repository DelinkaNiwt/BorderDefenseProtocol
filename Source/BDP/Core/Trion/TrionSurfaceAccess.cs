using Verse;

namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 对外接口获取面。
    /// 外部系统统一从这里拿 Trion 的正式读写口，不再自己摸 Comp 类型。
    /// </summary>
    public static class TrionSurfaceAccess
    {
        /// <summary>
        /// 读取 Pawn 身上的 Trion 只读口。
        /// </summary>
        public static ITrionReader ResolveReader(Pawn pawn)
        {
            CompTrion comp = ResolveComp(pawn);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 读取任意 ThingWithComps 宿主上的 Trion 只读口。
        /// 该入口服务建筑等非 Pawn 宿主，不承载任何战斗体业务。
        /// </summary>
        public static ITrionReader ResolveReader(ThingWithComps thing)
        {
            CompTrion comp = ResolveComp(thing);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 读取 Pawn 身上的 Trion 请求口。
        /// </summary>
        public static ITrionCommands ResolveCommands(Pawn pawn)
        {
            CompTrion comp = ResolveComp(pawn);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 读取任意 ThingWithComps 宿主上的 Trion 请求口。
        /// 当前用于建筑等非 Pawn 宿主的未来扩展预留。
        /// </summary>
        public static ITrionCommands ResolveCommands(ThingWithComps thing)
        {
            CompTrion comp = ResolveComp(thing);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 读取 Pawn 身上的 Trion 事件口。
        /// </summary>
        public static ITrionEvents ResolveEvents(Pawn pawn)
        {
            CompTrion comp = ResolveComp(pawn);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 读取任意 ThingWithComps 宿主上的 Trion 事件口。
        /// 当前用于建筑等非 Pawn 宿主的未来扩展预留。
        /// </summary>
        public static ITrionEvents ResolveEvents(ThingWithComps thing)
        {
            CompTrion comp = ResolveComp(thing);
            return comp != null ? comp.Service : null;
        }

        /// <summary>
        /// 统一定位 Pawn 身上的 Trion 宿主 Comp。
        /// </summary>
        private static CompTrion ResolveComp(Pawn pawn)
        {
            return pawn?.GetComp<CompTrion>();
        }

        /// <summary>
        /// 统一定位任意 ThingWithComps 宿主上的 Trion 宿主 Comp。
        /// </summary>
        private static CompTrion ResolveComp(ThingWithComps thing)
        {
            return thing?.GetComp<CompTrion>();
        }
    }
}
