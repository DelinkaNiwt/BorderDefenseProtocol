using Verse;
using RimWorld;

namespace ProjectTrion.Utilities
{
    /// <summary>
    /// 关键部位判定工具。
    /// 用于识别Trion供给器官等关键部位。
    ///
    /// Utility for identifying vital parts.
    /// Used to identify Trion supply organs and other critical parts.
    /// </summary>
    public static class VitalPartUtil
    {
        /// <summary>
        /// 检查一个身体部位是否是关键部位（Trion供给器官）。
        /// Check if a body part is vital (Trion supply organ).
        ///
        /// 关键部位定义：
        /// - 心脏（供给Trion的器官）
        /// - 应用层可通过Strategy自定义额外的关键部位
        ///
        /// Vital parts are:
        /// - Heart (supplies Trion)
        /// - Applications can define additional vital parts via Strategy
        /// </summary>
        public static bool IsVitalPart(BodyPartRecord part)
        {
            if (part == null)
                return false;

            // 框架定义的关键部位：心脏
            // 通过检查 defName 来识别心脏（RimWorld 1.6 兼容方式）
            if (part.def?.defName == "Heart")
                return true;

            // 躯干核心（躯干部位没有父部位）也是关键部位
            if (part.IsInGroup(BodyPartGroupDefOf.Torso) && part.parent == null)
                return true;

            // 应用层可通过其他方式扩展关键部位定义
            // （例如通过DefName检查）

            return false;
        }

        /// <summary>
        /// 检查一个身体部位是否是重要部位（影响泄漏速率）。
        /// Check if a body part is important (affects leak rate).
        /// </summary>
        public static bool IsImportantPart(BodyPartRecord part)
        {
            if (part == null)
                return false;

            // 重要部位：四肢、躯干、头部
            return part.IsInGroup(BodyPartGroupDefOf.LeftHand) ||
                   part.IsInGroup(BodyPartGroupDefOf.RightHand) ||
                   part.IsInGroup(BodyPartGroupDefOf.Legs) ||
                   part.IsInGroup(BodyPartGroupDefOf.Torso) ||
                   part.IsInGroup(BodyPartGroupDefOf.FullHead);
        }

        /// <summary>
        /// 根据部位获取泄漏加成倍数。
        /// Get leak rate multiplier for a specific body part.
        /// </summary>
        public static float GetLeakMultiplier(BodyPartRecord part)
        {
            if (!IsImportantPart(part))
                return 1f;

            // 躯干和头部泄漏加速
            if (part.IsInGroup(BodyPartGroupDefOf.Torso) ||
                part.IsInGroup(BodyPartGroupDefOf.FullHead))
                return 2f;

            // 四肢泄漏加速
            if (part.IsInGroup(BodyPartGroupDefOf.LeftHand) ||
                part.IsInGroup(BodyPartGroupDefOf.RightHand) ||
                part.IsInGroup(BodyPartGroupDefOf.Legs))
                return 1.5f;

            return 1f;
        }
    }
}
