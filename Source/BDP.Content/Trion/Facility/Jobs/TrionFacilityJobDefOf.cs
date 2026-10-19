using RimWorld;
using Verse;

namespace BDP.Content.Trion.Facility.Jobs
{
    /// <summary>核心绑定与解绑的独立原版任务定义。</summary>
    [DefOf]
    public static class TrionFacilityJobDefOf
    {
        /// <summary>皇室亲自建立核心归属。</summary>
        public static JobDef BDP_BindTrionFacility;

        /// <summary>绑定皇室亲自解除核心归属。</summary>
        public static JobDef BDP_UnbindTrionFacility;

        /// <summary>供物由原版搬运工作备齐后，指定人物前往执行最终献祭。</summary>
        public static JobDef BDP_SacrificeToTrionFacility;

        /// <summary>初始化正式定义引用。</summary>
        static TrionFacilityJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TrionFacilityJobDefOf));
        }
    }
}
