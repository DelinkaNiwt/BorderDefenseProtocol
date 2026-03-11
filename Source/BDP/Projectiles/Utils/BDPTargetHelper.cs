using Verse;

namespace BDP.Projectiles
{
    /// <summary>
    /// 投射物目标验证工具类。
    /// 提供统一的目标有效性和对齐检查。
    /// </summary>
    public static class BDPTargetHelper
    {
        /// <summary>
        /// 检查目标是否有效(存在、未销毁、已生成、未死亡/倒地)。
        /// 消除TrackingModule和VanillaAdapter中完全相同的实现。
        /// </summary>
        /// <param name="target">要检查的目标</param>
        /// <returns>如果目标有效返回true</returns>
        public static bool IsTargetValid(LocalTargetInfo target)
        {
            if (!target.IsValid) return false;
            if (target.Thing == null) return false;
            if (target.Thing.Destroyed) return false;
            if (!target.Thing.Spawned) return false;
            if (target.Thing is Pawn p && (p.Dead || p.Downed)) return false;
            return true;
        }

        /// <summary>
        /// 检查当前目标是否与锁定目标对齐(是同一个Thing或同一个Cell)。
        /// 用于追踪模块判断是否需要切换目标。
        /// </summary>
        /// <param name="current">当前目标</param>
        /// <param name="locked">锁定目标</param>
        /// <returns>如果对齐返回true</returns>
        public static bool IsTargetAligned(LocalTargetInfo current, LocalTargetInfo locked)
        {
            // Thing对齐检查
            if (current.HasThing && locked.HasThing)
                return current.Thing == locked.Thing;

            // Cell对齐检查
            if (!current.HasThing && !locked.HasThing)
                return current.Cell == locked.Cell;

            // 一个有Thing一个没有,不对齐
            return false;
        }
    }
}
