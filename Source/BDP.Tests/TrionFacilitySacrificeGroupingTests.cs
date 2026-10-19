using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Trion.Facility.Sacrifice;
using RimWorld;
using Verse;

namespace BDP.Tests
{
    /// <summary>验证合并后的选择仍准确落到具体实物，不改变数量、价值或最终消费语义。</summary>
    public static class TrionFacilitySacrificeGroupingTests
    {
        /// <summary>报告公共行为不符。</summary>
        private static void Check(bool result, string reason)
        {
            if (!result) throw new Exception(reason);
        }

        /// <summary>同一物品定义的普通堆栈。</summary>
        private static Thing Stack(ThingDef def, int count = 1, float value = 100) =>
            new Thing { def = def, stackCount = count, MarketValue = value };

        /// <summary>验证聚合、部分跨堆分配、实际差异和数量变化。</summary>
        public static void Run()
        {
            // 存档复现：零部件耐久62～70，但原版不按耐久修正其市值。
            var componentDef = new ThingDef { techLevel = TechLevel.Industrial, healthAffectsPrice = false };
            int[] health = { 70, 67, 65, 66, 68, 64, 69, 62, 63 };
            int[] counts = { 550, 2800, 900, 1537, 1750, 800, 1150, 100, 400 };
            var components = health.Select((hp, index) =>
            {
                Thing thing = Stack(componentDef, counts[index], 32);
                thing.HitPoints = hp;
                return thing;
            }).ToArray();
            var componentGroups = TrionFacilitySacrificeSupplyGroup.Create(components);
            Check(componentGroups.Count == 1 && componentGroups[0].AvailableCount == 9987,
                "零部件无价值影响的耐久差异不应拆分列表");

            var def = new ThingDef { techLevel = TechLevel.Industrial };
            var stacks = new[] { Stack(def, 1), Stack(def, 36), Stack(def, 4), Stack(def, 45), Stack(def, 50) };
            var groups = TrionFacilitySacrificeSupplyGroup.Create(stacks);
            Check(groups.Count == 1 && groups[0].AvailableCount == 136, "同种无差异物品必须按总数聚合");
            groups[0].ForceTo(40);
            var batch = new List<ThingCountClass>();
            Check(groups[0].TryAppendSelected(batch), "聚合总数可分配成真实搬运批次");
            Check(batch.Count == 3 && batch[0].Count == 1 && batch[1].Count == 36 && batch[2].Count == 3,
                "跨堆数量必须准确，不多拿一件");
            double yield;
            float lastUnitYield;
            Check(TrionFacilitySacrificeUtility.TryGetBatchYield(batch, out yield, out lastUnitYield) && Math.Abs(yield - 600) < 0.005,
                "聚合不改变实际总收益");
            Check(stacks[2].stackCount == 4 && stacks.All(thing => !thing.Destroyed), "界面分配不能提前拆堆或消费");
            stacks[0].Destroyed = true;
            batch.Clear();
            Check(groups[0].AvailableCount == 135 && groups[0].TryAppendSelected(batch)
                && batch.Sum(entry => entry.Count) == 40 && batch.All(entry => entry.thing != stacks[0]),
                "已消失的实物不能进入搬运批次");
            groups[0].ForceTo(groups[0].GetMaximumToTransfer());
            batch.Clear();
            Check(groups[0].CountToTransfer == 135 && groups[0].TryAppendSelected(batch)
                && batch.Sum(entry => entry.Count) == 135, "全部按钮的上限排除已消失堆栈");
            groups[0].CapacityLimit = TrionFacilitySacrificeUtility.GetMaximumCount(groups[0].UnitYield,
                groups[0].AvailableCount, 900, 1000);
            groups[0].ForceTo(groups[0].GetMaximumToTransfer());
            batch.Clear();
            Check(groups[0].CountToTransfer == 7 && groups[0].TryAppendSelected(batch)
                && batch.Sum(entry => entry.Count) == 7, "原版全部入口按容量自动截断到最后一件溢出");
            groups[0].ForceTo(136);
            batch.Clear();
            Check(!groups[0].TryAppendSelected(batch) && batch.Count == 0, "总量不足必须拒绝，不能静默少拿");

            var material = Stack(def); material.Stuff = new ThingDef();
            var quality = Stack(def); quality.quality = 2;
            var worn = Stack(def); worn.HitPoints = 99;
            var valuable = Stack(def, value: 101);
            var named = Stack(def); named.LabelNoCount = "不同命名";
            groups = TrionFacilitySacrificeSupplyGroup.Create(new[] { Stack(def), material, quality, worn, valuable, named });
            Check(groups.Count == 6, "材质、品质、耐久、价值和自定义名称差异必须保留");
            Console.WriteLine("通过：供物聚合、跨堆数量分配、收益不变、消失物品与实际差异。");
        }
    }
}
