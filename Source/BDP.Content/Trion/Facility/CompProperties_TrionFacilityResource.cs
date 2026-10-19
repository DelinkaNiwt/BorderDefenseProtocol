using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// Trion 地标设施资源容器的 Def 配置。
    /// 这些字段只描述建筑自己的资源闭环，不包含献祭者或心情规则。
    /// </summary>
    public sealed class CompProperties_TrionFacilityResource : CompProperties
    {
        /// <summary>
        /// 下一次补充的容量门槛，已接收的收益可以完整超过该值。
        /// </summary>
        public float maxResource = 1000f;

        /// <summary>
        /// 没有外部修正时的每日基础消耗量。
        /// </summary>
        public float baseDrainPerDay = 10f;

        /// <summary>
        /// 资源结算与绑定者检测共用的周期，单位为游戏刻；允许少量启停延迟。
        /// </summary>
        public int settleInterval = 250;

        /// <summary>
        /// 构造资源组件配置并绑定正式组件类型。
        /// </summary>
        public CompProperties_TrionFacilityResource()
        {
            compClass = typeof(CompTrionFacilityResource);
        }
    }
}
