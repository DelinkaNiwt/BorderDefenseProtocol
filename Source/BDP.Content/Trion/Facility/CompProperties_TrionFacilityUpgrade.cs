using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// Trion 地标设施升级状态的 Def 配置占位。
    /// 当前没有升级条件、成本或具体能力。
    /// </summary>
    public sealed class CompProperties_TrionFacilityUpgrade : CompProperties
    {
        /// <summary>
        /// 构造并绑定升级状态组件。
        /// </summary>
        public CompProperties_TrionFacilityUpgrade()
        {
            compClass = typeof(CompTrionFacilityUpgrade);
        }
    }
}
