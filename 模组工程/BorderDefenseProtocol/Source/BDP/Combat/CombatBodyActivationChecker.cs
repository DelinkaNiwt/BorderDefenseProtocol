using BDP.Core;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体激活前置条件检查器（静态可否决事件订阅者）。
    /// 在模块启动时自动注册到BDPEvents.QueryCanActivateCombatBody事件。
    /// 检查：是否装备触发体。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CombatBodyActivationChecker
    {
        static CombatBodyActivationChecker()
        {
            // 注册到静态事件
            BDPEvents.QueryCanActivateCombatBody += CheckTriggerBodyEquipped;
            Log.Message("[BDP] CombatBodyActivationChecker已注册到QueryCanActivateCombatBody事件");
        }

        /// <summary>
        /// 检查是否装备触发体。
        /// </summary>
        private static void CheckTriggerBodyEquipped(CanActivateCombatBodyEventArgs args)
        {
            if (args.Pawn == null) return;

            // 检查是否装备了主武器
            var primaryWeapon = args.Pawn.equipment?.Primary;
            if (primaryWeapon == null)
            {
                args.Vetoed = true;
                args.BlockReason = "需要装备触发体";
                return;
            }

            // 检查主武器是否是触发体（通过ICombatBodySupport接口判断）
            bool hasTriggerBodyComp = CombatBodyQuery.FindCombatBodySupport(args.Pawn) != null;

            if (!hasTriggerBodyComp)
            {
                args.Vetoed = true;
                args.BlockReason = "需要装备触发体";
            }
        }
    }
}
