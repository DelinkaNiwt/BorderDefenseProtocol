using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 对外接口获取面。
    /// 外部系统统一从这里拿 Trigger 的正式读写口，不再自己摸 Comp 类型。
    /// </summary>
    public static class TriggerSurfaceAccess
    {
        /// <summary>
        /// 读取 Pawn 当前主装备上的 Trigger 读取口。
        /// </summary>
        public static ITriggerLoadoutReader ResolveLoadoutReader(Pawn pawn)
        {
            CompTriggerBody comp = ResolveComp(pawn);
            return comp != null ? comp.LoadoutReaderSurface : null;
        }

        /// <summary>
        /// 读取 Pawn 当前主装备上的 Trigger 交互语义读取口。
        /// </summary>
        public static ITriggerInteractionReader ResolveInteractionReader(Pawn pawn)
        {
            CompTriggerBody comp = ResolveComp(pawn);
            return comp != null ? comp.InteractionSurface : null;
        }

        /// <summary>
        /// 读取 Pawn 当前主装备上的 Trigger 请求口。
        /// </summary>
        public static ITriggerLoadoutCommands ResolveLoadoutCommands(Pawn pawn)
        {
            CompTriggerBody comp = ResolveComp(pawn);
            return comp != null ? comp.LoadoutCommandSurface : null;
        }

        /// <summary>
        /// 读取 Pawn 当前主装备上的 Trigger 事件口。
        /// </summary>
        public static ITriggerEvents ResolveEvents(Pawn pawn)
        {
            CompTriggerBody comp = ResolveComp(pawn);
            return comp != null ? comp.EventSurface : null;
        }

        /// <summary>
        /// 统一定位 Pawn 当前主装备上的 Trigger 宿主 Comp。
        /// </summary>
        internal static CompTriggerBody ResolveComp(Pawn pawn)
        {
            ThingWithComps primaryEquipment = pawn?.equipment?.Primary;
            return primaryEquipment?.TryGetComp<CompTriggerBody>();
        }
    }
}
