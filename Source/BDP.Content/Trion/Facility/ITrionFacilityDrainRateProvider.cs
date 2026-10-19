using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// 设施有效消耗速率的外部修正接口。
    /// 未来心情规则只提供最终每日速率，不进入资源容器内部。
    /// </summary>
    public interface ITrionFacilityDrainRateProvider
    {
        /// <summary>
        /// 根据设施和基础速率解析当前有效每日消耗量。
        /// </summary>
        /// <param name="facility">当前 Trion 地标设施。</param>
        /// <param name="baseDrainPerDay">Def 提供的基础每日消耗量。</param>
        /// <returns>最终每日消耗量。</returns>
        float ResolveDrainPerDay(Building facility, float baseDrainPerDay);
    }
}
