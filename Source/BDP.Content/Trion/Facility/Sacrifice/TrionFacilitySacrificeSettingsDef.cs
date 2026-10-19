using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Facility.Sacrifice
{
    /// <summary>单一科技档内的单件收益边界，由内容配置提供。</summary>
    public sealed class TrionFacilitySacrificeYieldTier
    {
        /// <summary>原版科技档位。</summary>
        public TechLevel techLevel;
        /// <summary>本档单件收益下限。</summary>
        public float minYield;
        /// <summary>本档单件收益上限。</summary>
        public float maxYield;
    }

    /// <summary>不猜测未标注档位；指定例外同时授予供物资格和明确科技档。</summary>
    public sealed class TrionFacilitySacrificeTechOverride
    {
        /// <summary>实际物品或缩小建筑内层定义。</summary>
        public ThingDef thing;
        /// <summary>指定档位。</summary>
        public TechLevel techLevel;
    }

    /// <summary>正式献祭的科技例外、断档区间及市值映射参数。</summary>
    public sealed class TrionFacilitySacrificeSettingsDef : Def
    {
        /// <summary>只写明确例外，不按配方或研究前置猜科技档。</summary>
        public List<TrionFacilitySacrificeTechOverride> techLevelOverrides = new List<TrionFacilitySacrificeTechOverride>();
        /// <summary>各档独立的可调单件收益范围。</summary>
        public List<TrionFacilitySacrificeYieldTier> yieldTiers = new List<TrionFacilitySacrificeYieldTier>();
        /// <summary>达到档内中点时的物品固有市值；必须为正。</summary>
        public float referenceMarketValue;

        /// <summary>原版加载期报告错误配置，避免静默猜档或出现无限收益。</summary>
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (referenceMarketValue <= 0 || float.IsNaN(referenceMarketValue) || float.IsInfinity(referenceMarketValue))
                yield return "BDP_TrionFacility_Config_Reference".Translate(defName).ToString();
            var tiers = new Dictionary<TechLevel, TrionFacilitySacrificeYieldTier>();
            foreach (TrionFacilitySacrificeYieldTier tier in yieldTiers)
            {
                if (tier == null || tier.techLevel < TechLevel.Neolithic || tier.techLevel > TechLevel.Archotech
                    || tier.minYield < 0 || tier.maxYield < tier.minYield || tier.maxYield <= 0
                    || float.IsNaN(tier.minYield) || float.IsInfinity(tier.minYield)
                    || float.IsNaN(tier.maxYield) || float.IsInfinity(tier.maxYield)
                    || tiers.ContainsKey(tier.techLevel))
                    yield return "BDP_TrionFacility_Config_Tier".Translate(defName).ToString();
                else tiers.Add(tier.techLevel, tier);
            }
            TrionFacilitySacrificeYieldTier previous = null;
            for (TechLevel level = TechLevel.Neolithic; level <= TechLevel.Archotech; level++)
            {
                TrionFacilitySacrificeYieldTier tier;
                if (!tiers.TryGetValue(level, out tier) || (previous != null && tier.minYield <= previous.maxYield))
                    yield return "BDP_TrionFacility_Config_Gap".Translate(defName, level.ToString()).ToString();
                previous = tier;
            }
            var overrides = new HashSet<ThingDef>();
            foreach (TrionFacilitySacrificeTechOverride entry in techLevelOverrides)
                if (entry == null || entry.thing == null || !overrides.Add(entry.thing) || !tiers.ContainsKey(entry.techLevel))
                    yield return "BDP_TrionFacility_Config_Override".Translate(defName).ToString();
        }
    }

    /// <summary>正式参数的原版定义绑定，不另建设置缓存。</summary>
    [DefOf]
    public static class TrionFacilitySacrificeSettingsDefOf
    {
        /// <summary>科技供物和收益试用配置。</summary>
        public static TrionFacilitySacrificeSettingsDef BDP_TrionFacilitySacrificeSettings;
        /// <summary>初始化正式定义引用。</summary>
        static TrionFacilitySacrificeSettingsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TrionFacilitySacrificeSettingsDefOf));
        }
    }
}
