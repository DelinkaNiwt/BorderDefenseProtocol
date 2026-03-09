using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 触发体查询工具类。
    /// 集中化常见的触发体/芯片查询操作,消除跨文件重复的查询链。
    /// </summary>
    public static class TriggerBodyQueries
    {
        // ═══════════════════════════════════════════
        //  ChipSlot 扩展方法 (消除13+处重复)
        // ═══════════════════════════════════════════

        /// <summary>
        /// 获取槽位中芯片的TriggerChipComp组件。
        /// 消除 slot.loadedChip?.TryGetComp&lt;TriggerChipComp&gt;() 重复链。
        /// </summary>
        public static TriggerChipComp GetChipComp(this ChipSlot slot)
        {
            return slot?.loadedChip?.TryGetComp<TriggerChipComp>();
        }

        /// <summary>
        /// 获取槽位中芯片的属性配置。
        /// 消除 slot.loadedChip?.TryGetComp&lt;TriggerChipComp&gt;()?.Props 重复链。
        /// </summary>
        public static CompProperties_TriggerChip GetChipProps(this ChipSlot slot)
        {
            return slot.GetChipComp()?.Props;
        }

        // ═══════════════════════════════════════════
        //  VerbChipConfig 查询 (消除12+处重复)
        // ═══════════════════════════════════════════

        /// <summary>
        /// 获取指定侧别的VerbChipConfig配置。
        /// 消除 triggerComp.GetActiveSlot(side)?.loadedChip?.def?.GetModExtension&lt;VerbChipConfig&gt;() 长链。
        /// </summary>
        public static VerbChipConfig GetVerbChipConfig(CompTriggerBody triggerComp, SlotSide side)
        {
            if (triggerComp == null) return null;
            var slot = triggerComp.GetActiveSlot(side);
            return slot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
        }

        /// <summary>
        /// 获取双侧的VerbChipConfig配置。
        /// 消除双侧Verb中9处重复的左右查询组合。
        /// </summary>
        public static (VerbChipConfig left, VerbChipConfig right) GetDualChipConfigs(CompTriggerBody triggerComp)
        {
            if (triggerComp == null) return (null, null);

            var leftCfg = GetVerbChipConfig(triggerComp, SlotSide.LeftHand);
            var rightCfg = GetVerbChipConfig(triggerComp, SlotSide.RightHand);

            return (leftCfg, rightCfg);
        }

        // ═══════════════════════════════════════════
        //  激活槽位查询 (消除8+处重复)
        // ═══════════════════════════════════════════

        /// <summary>
        /// 获取双手的激活槽位。
        /// 消除 GetActiveSlot(LeftHand) + GetActiveSlot(RightHand) 总是成对出现的模式。
        /// </summary>
        public static (ChipSlot left, ChipSlot right) GetActiveHandSlots(CompTriggerBody triggerComp)
        {
            if (triggerComp == null) return (null, null);

            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);

            return (leftSlot, rightSlot);
        }

        // ═══════════════════════════════════════════
        //  CompTriggerBody 查询 (消除2处继承链分叉重复)
        // ═══════════════════════════════════════════

        private static CompTriggerBody cachedTriggerComp;
        private static Pawn cachedTriggerPawn;

        /// <summary>
        /// 获取Pawn主武器上的CompTriggerBody组件(带缓存)。
        /// 消除 Verb_BDPRangedBase 和 Verb_BDPMelee 各自独立实现的相同缓存逻辑。
        /// </summary>
        public static CompTriggerBody GetTriggerComp(Pawn pawn)
        {
            if (pawn == null)
            {
                cachedTriggerComp = null;
                cachedTriggerPawn = null;
                return null;
            }

            // 缓存命中检查
            if (pawn == cachedTriggerPawn && cachedTriggerComp != null)
                return cachedTriggerComp;

            // 缓存未命中,重新查找
            cachedTriggerPawn = pawn;
            cachedTriggerComp = pawn.equipment?.Primary?.TryGetComp<CompTriggerBody>();

            return cachedTriggerComp;
        }
    }
}
