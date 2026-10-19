using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Facility.Sacrifice
{
    /// <summary>预览与实际提交共用一套供物及价值规则，不使用交易报价。</summary>
    public static class TrionFacilitySacrificeUtility
    {
        /// <summary>按实际定义识别科技，按物品固有市值计算单件收益。</summary>
        public static bool TryGetUnitYield(Thing thing, out float yield)
        {
            yield = 0;
            var settings = TrionFacilitySacrificeSettingsDefOf.BDP_TrionFacilitySacrificeSettings;
            Thing actual = thing is MinifiedThing minified ? minified.InnerThing : thing;
            if (thing == null || thing.Destroyed || actual?.def == null || settings == null
                || settings.referenceMarketValue <= 0 || float.IsNaN(settings.referenceMarketValue)
                || float.IsInfinity(settings.referenceMarketValue)) return false;
            // 准入仅看原生科技档或显式例外，不依赖物品类别、材质、配方或研究前置。
            TechLevel level = actual.def.techLevel;
            foreach (TrionFacilitySacrificeTechOverride entry in settings.techLevelOverrides)
                if (entry.thing == actual.def)
                {
                    level = entry.techLevel;
                    break;
                }
            if (level < TechLevel.Neolithic || level > TechLevel.Archotech) return false;
            float value = thing.MarketValue;
            if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            if (value < 0) value = 0;
            foreach (TrionFacilitySacrificeYieldTier tier in settings.yieldTiers)
                if (tier.techLevel == level)
                {
                    // 科技决定独立档位；固有市值只在档内移动，不叠加销售或类别倍率。
                    yield = tier.minYield + (tier.maxYield - tier.minYield)
                        * (value / (value + settings.referenceMarketValue));
                    return yield > 0 && !float.IsNaN(yield) && !float.IsInfinity(yield);
                }
            return false;
        }

        /// <summary>完整计价并读取批次末项的单件收益；调用方按玩家选择指定末项，不允许按收益重排。</summary>
        public static bool TryGetBatchYield(IList<ThingCountClass> batch, out double yield, out float lastUnitYield)
        {
            yield = 0;
            lastUnitYield = 0;
            if (batch == null || batch.Count == 0) return false;
            var seen = new HashSet<Thing>();
            double total = 0;
            float last = 0;
            foreach (ThingCountClass entry in batch)
            {
                float unitYield;
                if (entry?.thing == null || entry.Count <= 0 || entry.Count > entry.thing.stackCount
                    || !seen.Add(entry.thing) || !TryGetUnitYield(entry.thing, out unitYield)) return false;
                total += (double)unitYield * entry.Count;
                last = unitYield;
            }
            if (total <= 0 || total > float.MaxValue || double.IsInfinity(total) || double.IsNaN(total)) return false;
            yield = total;
            lastUnitYield = last;
            return true;
        }

        /// <summary>只有玩家最后加入的一件可越过容量；扣除这一件后，之前的储量必须仍低于容量。</summary>
        public static bool CanAcceptBatch(double yield, float lastUnitYield, float currentResource, float maxResource) =>
            maxResource > 0 && currentResource < maxResource && yield > 0 && lastUnitYield > 0
                && (double)currentResource + yield - lastUnitYield < maxResource;

        /// <summary>保留本行已选数量，新增只按当前剩余容量截断；达到门槛后各行只能减量。</summary>
        public static int GetMaximumCount(float unitYield, int availableCount, float currentResource, float maxResource,
            double otherYield = 0, int selectedCount = 0)
        {
            if (unitYield <= 0 || float.IsNaN(unitYield) || float.IsInfinity(unitYield)
                || availableCount <= 0 || maxResource <= 0 || currentResource >= maxResource) return 0;
            // 达到或跨过门槛的那一件就是此次新增的最后一件，不把其他物品移到它之前。
            double remaining = (double)maxResource - currentResource - otherYield - (double)unitYield * selectedCount;
            double count = System.Math.Max(0, selectedCount);
            if (remaining > 0) count += System.Math.Ceiling(remaining / unitYield);
            if (double.IsNaN(count) || count <= 0) return 0;
            return count >= availableCount ? availableCount : (int)count;
        }
    }
}
