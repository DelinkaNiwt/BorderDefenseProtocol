using UnityEngine;
using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// Trion 地标设施消耗速率的最小注册表。
    /// 当前没有外部修正时直接使用 Def 基础速率。
    /// </summary>
    public static class TrionFacilityDrainRateRegistry
    {
        /// <summary>
        /// 当前唯一外部速率提供器。
        /// </summary>
        private static ITrionFacilityDrainRateProvider current;

        /// <summary>
        /// 注册或替换当前速率提供器。
        /// </summary>
        /// <param name="provider">新的速率提供器；传入 null 表示清空。</param>
        public static void Register(ITrionFacilityDrainRateProvider provider)
        {
            current = provider;
        }

        /// <summary>
        /// 只清除指定实例，避免旧提供器误删后续注册的实例。
        /// </summary>
        /// <param name="provider">准备清除的速率提供器。</param>
        public static void Clear(ITrionFacilityDrainRateProvider provider)
        {
            if (ReferenceEquals(current, provider))
            {
                current = null;
            }
        }

        /// <summary>
        /// 解析最终每日消耗速率，并钳制为非负值。
        /// </summary>
        /// <param name="facility">当前 Trion 地标设施。</param>
        /// <param name="baseDrainPerDay">Def 提供的基础每日消耗量。</param>
        /// <returns>最终每日消耗量。</returns>
        public static float ResolveDrainPerDay(Building facility, float baseDrainPerDay)
        {
            float resolved = current == null
                ? baseDrainPerDay
                : current.ResolveDrainPerDay(facility, baseDrainPerDay);
            return Mathf.Max(0f, resolved);
        }
    }
}
