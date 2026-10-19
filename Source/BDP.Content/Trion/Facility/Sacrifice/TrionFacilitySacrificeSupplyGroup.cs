using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Facility.Sacrifice
{
    /// <summary>窗口内同种无差异供物的聚合选择；不修改地图堆栈或建立永久库存。</summary>
    public sealed class TrionFacilitySacrificeSupplyGroup : TransferableOneWay
    {
        /// <summary>显示图标和不含数量名称的代表物品。</summary>
        public Thing Representative => AnyThing;
        /// <summary>同组单件预览收益，最终提交仍重新计价。</summary>
        public float UnitYield { get; }
        /// <summary>仍在地图上的实际总数量，不计已销毁或被搬走的堆栈。</summary>
        public int AvailableCount => things.Where(thing => !thing.Destroyed && thing.Spawned).Sum(thing => thing.stackCount);
        /// <summary>窗口按当前储量和其他已选供物更新的数量上限，原版输入与“全部”共同使用。</summary>
        public int CapacityLimit { get; set; } = int.MaxValue;

        /// <summary>数量不得超过仍可用实物与单件溢出上限；原版“全部”会自动停在此处。</summary>
        public override int GetMaximumToTransfer() => System.Math.Min(AvailableCount, CapacityLimit);

        /// <summary>用第一堆实物建立组。</summary>
        private TrionFacilitySacrificeSupplyGroup(Thing thing, float unitYield)
        {
            things.Add(thing);
            UnitYield = unitYield;
        }

        /// <summary>复用原版材质、品质及组件分组；精确保留价值和名称差异，仅在耐久影响市值时区分耐久。</summary>
        private bool CanGroupWith(Thing thing)
        {
            Thing firstActual = Representative is MinifiedThing first ? first.InnerThing : Representative;
            Thing actual = thing is MinifiedThing minified ? minified.InnerThing : thing;
            return (!firstActual.def.healthAffectsPrice || firstActual.HitPoints == actual.HitPoints)
                && Representative.MarketValue == thing.MarketValue
                && Representative.LabelNoCount == thing.LabelNoCount
                && TransferableUtility.TransferAsOne(Representative, thing, TransferAsOneMode.Normal);
        }

        /// <summary>只在打开窗口时分组；按定义缩小比较范围，不扫描地图或逐帧重新分组。</summary>
        public static List<TrionFacilitySacrificeSupplyGroup> Create(IEnumerable<Thing> supplies)
        {
            var result = new List<TrionFacilitySacrificeSupplyGroup>();
            var byDef = new Dictionary<ThingDef, List<TrionFacilitySacrificeSupplyGroup>>();
            foreach (Thing thing in supplies)
            {
                float yield;
                if (thing == null || !thing.Spawned || !TrionFacilitySacrificeUtility.TryGetUnitYield(thing, out yield)) continue;
                Thing actual = thing is MinifiedThing minified ? minified.InnerThing : thing;
                List<TrionFacilitySacrificeSupplyGroup> candidates;
                if (!byDef.TryGetValue(actual.def, out candidates))
                    byDef.Add(actual.def, candidates = new List<TrionFacilitySacrificeSupplyGroup>());
                var group = candidates.FirstOrDefault(candidate => candidate.CanGroupWith(thing));
                if (group != null) group.things.Add(thing);
                else
                {
                    group = new TrionFacilitySacrificeSupplyGroup(thing, yield);
                    candidates.Add(group);
                    result.Add(group);
                }
            }
            return result;
        }

        /// <summary>总数准确分配到具体实物；不足时拒绝，不拆堆、不消费、不静默减量。</summary>
        public bool TryAppendSelected(List<ThingCountClass> batch)
        {
            if (CountToTransfer == 0) return true;
            if (CountToTransfer < 0 || CountToTransfer > AvailableCount) return false;
            int remaining = CountToTransfer;
            foreach (Thing thing in things)
            {
                if (remaining == 0) break;
                if (thing.Destroyed || !thing.Spawned) continue;
                int take = System.Math.Min(remaining, thing.stackCount);
                if (take > 0) batch.Add(new ThingCountClass(thing, take));
                remaining -= take;
            }
            return remaining == 0;
        }
    }
}
