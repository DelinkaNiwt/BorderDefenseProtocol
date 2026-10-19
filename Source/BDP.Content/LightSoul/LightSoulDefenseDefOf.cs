using RimWorld;
using Verse;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂防护业务使用的原版 DefOf（定义快捷引用）。
    /// </summary>
    [DefOf]
    public static class LightSoulDefenseDefOf
    {
        /// <summary>
        /// 只站立并执行注视警戒的作业定义。
        /// </summary>
        public static JobDef BDP_LightSoulDefenseWatch;

        /// <summary>
        /// 确保原版在静态初始化阶段填充定义引用。
        /// </summary>
        static LightSoulDefenseDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LightSoulDefenseDefOf));
        }
    }
}
