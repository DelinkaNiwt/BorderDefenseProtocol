using ProjectTrion.Components;
using Verse;

namespace ProjectTrion.Utilities
{
    /// <summary>
    /// Trion系统工具函数。
    ///
    /// Trion system utilities.
    /// </summary>
    public static class TrionUtil
    {
        /// <summary>
        /// 获取Pawn的CompTrion组件。
        /// Get CompTrion from a pawn.
        /// </summary>
        public static CompTrion GetCompTrion(this Pawn pawn)
        {
            if (pawn == null)
                return null;

            return pawn.GetComp<CompTrion>();
        }

        /// <summary>
        /// 检查Pawn是否有Trion能力（已装备触发器）。
        /// Check if pawn has Trion capability (equipped trigger).
        /// </summary>
        public static bool HasTrionAbility(this Pawn pawn)
        {
            var comp = pawn.GetCompTrion();
            return comp != null && comp.Capacity > 0;
        }

        /// <summary>
        /// 检查Pawn是否处于战斗体状态。
        /// Check if pawn is in combat body state.
        /// </summary>
        public static bool IsInCombat(this Pawn pawn)
        {
            var comp = pawn.GetCompTrion();
            return comp != null && comp.IsInCombat;
        }

        /// <summary>
        /// 获取Pawn当前可用的Trion量。
        /// Get pawn's current available Trion.
        /// </summary>
        public static float GetAvailableTrion(this Pawn pawn)
        {
            var comp = pawn.GetCompTrion();
            return comp != null ? comp.Available : 0f;
        }

        /// <summary>
        /// 生成战斗体。
        /// Generate combat body for a pawn.
        /// </summary>
        public static bool GenerateCombatBody(this Pawn pawn)
        {
            var comp = pawn.GetCompTrion();
            if (comp == null)
                return false;

            if (comp.IsInCombat)
            {
                Log.Warning($"ProjectTrion: {pawn.Name}已在战斗体状态");
                return false;
            }

            comp.GenerateCombatBody();
            return true;
        }

        /// <summary>
        /// 摧毁战斗体。
        /// Destroy combat body for a pawn.
        /// </summary>
        public static bool DestroyCombatBody(this Pawn pawn)
        {
            var comp = pawn.GetCompTrion();
            if (comp == null)
                return false;

            if (!comp.IsInCombat)
            {
                Log.Warning($"ProjectTrion: {pawn.Name}未在战斗体状态");
                return false;
            }

            comp.DestroyCombatBody(Core.DestroyReason.Manual);
            return true;
        }
    }
}
