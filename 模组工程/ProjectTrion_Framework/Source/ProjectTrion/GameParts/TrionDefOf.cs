using Verse;

namespace ProjectTrion.GameParts
{
    /// <summary>
    /// Trion系统的Def引用。
    /// 这些将在应用层通过XML Def定义并注册。
    ///
    /// Trion system def references.
    /// These should be defined and registered by application layer via XML Defs.
    /// </summary>
    public static class TrionDefOf
    {
        // 应用层应通过XML定义这些Def
        // Application layer should define these Defs via XML

        /// <summary>
        /// Trion虚拟伤害类型。
        /// Virtual damage type for Trion energy system.
        /// </summary>
        public static DamageDef ProjectTrion_VirtualDamage;

        /// <summary>
        /// Trion枯竭debuff（当战斗体被动破裂时施加）。
        /// Trion depletion debuff (applied when combat body is passively destroyed).
        /// </summary>
        public static HediffDef ProjectTrion_Depletion;

        /// <summary>
        /// 在应用层初始化时调用此方法来注册Def。
        /// Call this method from application layer to register Defs.
        /// </summary>
        public static void InitializeDefs()
        {
            // 应用层应在此处使用DefDatabase<T>.GetNamed()填充这些引用
            // Application layer should populate these references here using DefDatabase<T>.GetNamed()
        }
    }
}
