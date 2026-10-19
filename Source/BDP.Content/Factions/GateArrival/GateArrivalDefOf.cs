using RimWorld;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>门业务的正式定义引用。</summary>
    [DefOf]
    public static class GateArrivalDefOf
    {
        /// <summary>不占格、不受伤的出场控制物。</summary>
        public static ThingDef BDP_GateArrivalController;
        /// <summary>位于门格并深存本门待出场人物的持有物。</summary>
        public static ThingDef BDP_GateArrivalPawnHolder;
        /// <summary>可建造的门诱导装置。</summary>
        public static ThingDef BDP_GateInducer;
        /// <summary>可供正式抵达和开发视觉测试共用的维持期电弧定义。</summary>
        public static EffecterDef BDP_GateMaintainedElectricity;
        /// <summary>近界宙域门抵达方式。</summary>
        public static PawnsArrivalModeDef BDP_NearRealmGate;
        /// <summary>延续原版定义初始化顺序。</summary>
        static GateArrivalDefOf() { DefOfHelper.EnsureInitializedInCtor(typeof(GateArrivalDefOf)); }
    }
}
