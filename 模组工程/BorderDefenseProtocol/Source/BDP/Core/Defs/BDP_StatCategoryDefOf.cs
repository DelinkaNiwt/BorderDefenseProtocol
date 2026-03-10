using RimWorld;

namespace BDP.Core
{
    /// <summary>
    /// BDP自定义StatCategory的DefOf引用。
    /// 用于Info Card的stat条目分类。
    /// </summary>
    [DefOf]
    public static class BDP_StatCategoryDefOf
    {
        /// <summary>芯片信息StatCategory。</summary>
        public static StatCategoryDef BDP_ChipInfo;

        /// <summary>触发器配置StatCategory。</summary>
        public static StatCategoryDef BDP_TriggerConfig;

        static BDP_StatCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BDP_StatCategoryDefOf));
        }
    }
}
