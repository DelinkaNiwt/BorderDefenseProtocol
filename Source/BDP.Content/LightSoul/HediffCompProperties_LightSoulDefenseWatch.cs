using Verse;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂防护“注视警戒”组件的 XML（可扩展标记语言）参数。
    /// 射程、视线和目标类型全部使用原版 VerbProperties（行为参数）定义。
    /// </summary>
    public sealed class HediffCompProperties_LightSoulDefenseWatch : HediffCompProperties_VerbGiver
    {
        /// <summary>
        /// 建立参数并绑定光魂防护业务组件。
        /// </summary>
        public HediffCompProperties_LightSoulDefenseWatch()
        {
            compClass = typeof(HediffComp_LightSoulDefenseWatch);
        }
    }
}
