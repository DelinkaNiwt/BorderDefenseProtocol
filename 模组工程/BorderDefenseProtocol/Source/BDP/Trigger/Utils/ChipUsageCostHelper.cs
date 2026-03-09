using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片使用消耗统一工具类。
    /// 提供从CompProperties_TriggerChip.usageCost读取和扣除Trion的统一接口。
    /// </summary>
    public static class ChipUsageCostHelper
    {
        /// <summary>
        /// 获取芯片的使用消耗（每次射击/使用能力的Trion消耗）。
        /// </summary>
        /// <param name="chipThing">芯片物品</param>
        /// <returns>使用消耗量，0表示无消耗</returns>
        public static float GetUsageCost(Thing chipThing)
        {
            if (chipThing == null)
                return 0f;

            var chipComp = chipThing.TryGetComp<TriggerChipComp>();
            if (chipComp == null)
                return 0f;

            return chipComp.Props.usageCost;
        }

        /// <summary>
        /// 检查Pawn是否有足够的Trion支付使用消耗。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="chipThing">芯片物品</param>
        /// <returns>true=Trion足够，false=Trion不足</returns>
        public static bool CanAffordUsage(Pawn pawn, Thing chipThing)
        {
            if (pawn == null || chipThing == null)
                return false;

            float cost = GetUsageCost(chipThing);
            if (cost <= 0f)
                return true; // 无消耗，总是可以使用

            var trionComp = pawn.TryGetComp<BDP.Core.CompTrion>();
            if (trionComp == null)
                return false;

            return trionComp.Available >= cost;
        }

        /// <summary>
        /// 消耗Pawn的Trion（扣除使用消耗）。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="chipThing">芯片物品</param>
        /// <returns>true=成功扣除，false=扣除失败（Trion不足或无效参数）</returns>
        public static bool ConsumeUsageCost(Pawn pawn, Thing chipThing)
        {
            if (pawn == null || chipThing == null)
                return false;

            float cost = GetUsageCost(chipThing);
            if (cost <= 0f)
                return true; // 无消耗，总是成功

            var trionComp = pawn.TryGetComp<BDP.Core.CompTrion>();
            if (trionComp == null)
                return false;

            // 利用Consume的返回值,它已经包含了Available检查
            return trionComp.Consume(cost);
        }
    }
}
