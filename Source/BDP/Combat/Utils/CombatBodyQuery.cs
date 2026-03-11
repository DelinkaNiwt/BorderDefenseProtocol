using RimWorld;
using Verse;
using Verse.AI;
using BDP.Core;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体状态查询工具类。
    /// 集中化常见的战斗体状态检查和查询操作,消除跨文件重复代码。
    /// </summary>
    public static class CombatBodyQuery
    {
        /// <summary>
        /// 检查Pawn是否处于战斗体激活状态。
        /// </summary>
        /// <param name="pawn">要检查的Pawn</param>
        /// <returns>如果有BDP_CombatBodyActive hediff则返回true</returns>
        public static bool IsCombatBodyActive(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return false;
            return pawn.health.hediffSet.HasHediff(BDP_DefOf.BDP_CombatBodyActive);
        }

        /// <summary>
        /// 检查Pawn是否处于战斗体破裂状态。
        /// </summary>
        /// <param name="pawn">要检查的Pawn</param>
        /// <returns>如果有BDP_CombatBodyCollapsing hediff则返回true</returns>
        public static bool IsCollapsing(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return false;
            return pawn.health.hediffSet.HasHediff(BDP_DefOf.BDP_CombatBodyCollapsing);
        }

        /// <summary>
        /// 查找Pawn装备的触发体组件(实现ICombatBodySupport接口的Comp)。
        /// </summary>
        /// <param name="pawn">要查找的Pawn</param>
        /// <returns>找到的ICombatBodySupport实例,未找到则返回null</returns>
        public static ICombatBodySupport FindCombatBodySupport(Pawn pawn)
        {
            if (pawn?.equipment?.Primary == null) return null;
            return FindCombatBodySupport(pawn.equipment.Primary);
        }

        /// <summary>
        /// 查找装备上的触发体组件(实现ICombatBodySupport接口的Comp)。
        /// </summary>
        /// <param name="equipment">要查找的装备</param>
        /// <returns>找到的ICombatBodySupport实例,未找到则返回null</returns>
        public static ICombatBodySupport FindCombatBodySupport(ThingWithComps equipment)
        {
            if (equipment?.AllComps == null) return null;

            foreach (var comp in equipment.AllComps)
            {
                if (comp is ICombatBodySupport support)
                    return support;
            }

            return null;
        }

        /// <summary>
        /// 检查Pawn的HediffSet是否有效(非null)。
        /// </summary>
        /// <param name="pawn">要检查的Pawn</param>
        /// <returns>如果pawn?.health?.hediffSet非null则返回true</returns>
        public static bool HasValidHediffSet(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null;
        }

        /// <summary>
        /// 打断Pawn当前的所有动作(Job/Stance/Pather)。
        /// 用于战斗体破裂、紧急脱离等需要强制中断行为的场景。
        /// </summary>
        /// <param name="pawn">要打断的Pawn</param>
        /// <param name="reason">打断原因(用于日志)</param>
        public static void InterruptCurrentAction(Pawn pawn, string reason)
        {
            if (pawn == null) return;

            Log.Message($"[BDP] 打断当前动作: {pawn.LabelShort} (原因: {reason})");

            // 结束当前Job
            if (pawn.jobs?.curJob != null)
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);

            // 取消战斗姿态
            if (pawn.stances != null)
                pawn.stances.CancelBusyStanceSoft();

            // 停止寻路
            if (pawn.pather != null)
                pawn.pather.StopDead();
        }
    }
}
