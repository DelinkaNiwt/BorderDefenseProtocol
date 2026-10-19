using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Trion.Facility;
using BDP.Content.Trion.Facility.Sacrifice;
using BDP.Content.Trion.Royalty.Contract;
using RimWorld;
using Verse;

namespace BDP.Tests
{
    /// <summary>编译真实收益与最终提交，验证科技准入、收益断档及不吞供物。</summary>
    public static class TrionFacilitySacrificeTests
    {
        /// <summary>判断公共行为。</summary>
        private static void Check(bool result, string reason)
        {
            if (!result) throw new Exception(reason);
        }
        /// <summary>数值比较。</summary>
        private static void Equal(float expected, float actual, string reason)
        {
            Check(Math.Abs(expected - actual) < 0.005f, reason + ": " + actual);
        }
        /// <summary>创建仅标科技与市值、未接入类别白名单的物品。</summary>
        private static Thing Item(TechLevel level, float value = 100, int count = 1) =>
            new Thing { def = new ThingDef { techLevel = level }, MarketValue = value, stackCount = count };
        /// <summary>创建真实绑定与资源组件，不复制其业务判断。</summary>
        private static CompTrionFacilityControl Core(out Pawn royal, out CompTrionFacilityResource resource)
        {
            var building = new Building_TrionFacility();
            resource = new CompTrionFacilityResource
            {
                parent = building,
                props = new CompProperties_TrionFacilityResource { maxResource = 1000, baseDrainPerDay = 10 }
            };
            var control = new CompTrionFacilityControl
            {
                parent = building,
                props = new CompProperties_TrionFacilityControl { royalGene = new GeneDef(), contractHediff = new HediffDef() }
            };
            building.comps.Add(resource);
            building.comps.Add(control);
            resource.PostSpawnSetup(false);
            royal = new Pawn { Map = building.Map };
            Check(control.TryBind(royal), "初始正式皇室可绑定");
            Check(!resource.IsOperational && control.CompGetGizmosExtra().OfType<Command_Action>()
                .Any(gizmo => gizmo.defaultLabel == "BDP_TrionFacility_Sacrifice"),
                "零储量的已绑定核心必须提供正式补充入口");
            return control;
        }
        /// <summary>验证真实定义、收益规则和一次性物品消费。</summary>
        public static void Run()
        {
            Find.TickManager.TicksGame = 0;
            var settings = new TrionFacilitySacrificeSettingsDef { referenceMarketValue = 100 };
            float[] lower = { 1, 3, 10, 100, 1000, 10000 };
            float[] upper = { 2, 6, 20, 200, 2000, 20000 };
            for (int i = 0; i < lower.Length; i++)
                settings.yieldTiers.Add(new TrionFacilitySacrificeYieldTier { techLevel = (TechLevel)(i + (int)TechLevel.Neolithic), minYield = lower[i], maxYield = upper[i] });
            TrionFacilitySacrificeSettingsDefOf.BDP_TrionFacilitySacrificeSettings = settings;
            Check(!settings.ConfigErrors().Any(), "合法配置不应报错");
            settings.referenceMarketValue = 0;
            Check(settings.ConfigErrors().Any(), "无效参考市值应明确报错");
            settings.referenceMarketValue = 100;
            var industrial = Item(TechLevel.Industrial);
            float yield;
            Check(TrionFacilitySacrificeUtility.TryGetUnitYield(industrial, out yield), "工业制成品可献祭");
            Equal(15, yield, "参考市值对应档内中点");
            industrial.MarketValue = 300;
            TrionFacilitySacrificeUtility.TryGetUnitYield(industrial, out yield);
            Equal(17.5f, yield, "品质和耐久的价值变化直接在档内体现");
            Check(TrionFacilitySacrificeUtility.TryGetUnitYield(Item(TechLevel.Spacer, 0), out yield) && yield > 20, "科技断档优先于价格");
            Check(TrionFacilitySacrificeUtility.TryGetUnitYield(Item(TechLevel.Spacer, 10000), out yield), "其他模组有明确科技的物品不依赖成品类别注册");
            Check(TrionFacilitySacrificeUtility.TryGetUnitYield(Item(TechLevel.Neolithic), out yield), "原木等明确标注新石器科技的物品也应接收");
            Check(!TrionFacilitySacrificeUtility.TryGetUnitYield(Item(TechLevel.Undefined, 10000), out yield), "无档位物品不能因高市值而猜最低档");
            Check(!TrionFacilitySacrificeUtility.TryGetUnitYield(Item(TechLevel.Animal), out yield), "动物档不是已配置的献祭科技档");
            for (TechLevel level = TechLevel.Neolithic; level <= TechLevel.Archotech; level++)
                Check(TrionFacilitySacrificeUtility.TryGetUnitYield(Item(level), out yield), "全部已配置科技档应可接收：" + level);
            var component = Item(TechLevel.Undefined);
            settings.techLevelOverrides.Add(new TrionFacilitySacrificeTechOverride { thing = component.def, techLevel = TechLevel.Industrial });
            Check(TrionFacilitySacrificeUtility.TryGetUnitYield(component, out yield), "指定例外提供合法性和明确档位");
            Equal(15, yield, "组件不额外套类别倍率");
            var minified = new MinifiedThing { InnerThing = Item(TechLevel.Industrial), MarketValue = 100 };
            Check(TrionFacilitySacrificeUtility.TryGetUnitYield(minified, out yield), "缩小建筑按内层定义识别");
            Equal(15, yield, "缩小建筑仍使用物品本身市值");

            var bulk = Item(TechLevel.Industrial, count: 20);
            var batch = new List<ThingCountClass> { new ThingCountClass(bulk, 10) };
            double batchYield;
            float lastUnitYield;
            Check(TrionFacilitySacrificeUtility.TryGetBatchYield(batch, out batchYield, out lastUnitYield), "部分堆叠可按数量计算");
            Equal(150, (float)batchYield, "数量线性累加");
            Equal(15, lastUnitYield, "一堆十件仍以单件收益判断溢出");
            batch.Add(new ThingCountClass(bulk, 10));
            Check(!TrionFacilitySacrificeUtility.TryGetBatchYield(batch, out batchYield, out lastUnitYield), "重复引用不得多算收益或消费两次");
            Equal(0, (float)batchYield, "无效整批不能在预览中留下部分收益");
            var orderedLow = Item(TechLevel.Industrial, count: 3);
            var orderedHigh = Item(TechLevel.Archotech);
            var orderedBatch = new List<ThingCountClass>
            {
                new ThingCountClass(orderedHigh, 1), new ThingCountClass(orderedLow, 3)
            };
            Check(TrionFacilitySacrificeUtility.TryGetBatchYield(orderedBatch, out batchYield, out lastUnitYield),
                "按玩家选择顺序读取整批");
            Equal(15, lastUnitYield, "最后加入低收益物品时不能改用此前超凡物品作为最后一件");
            Check(!TrionFacilitySacrificeUtility.CanAcceptBatch(batchYield, lastUnitYield, 0, 1000),
                "高收益物品已越过门槛，不能再用最后的小额物品补入");
            orderedBatch.Reverse();
            Check(TrionFacilitySacrificeUtility.TryGetBatchYield(orderedBatch, out batchYield, out lastUnitYield)
                && TrionFacilitySacrificeUtility.CanAcceptBatch(batchYield, lastUnitYield, 0, 1000),
                "先小额后超凡时，玩家最后一件可完整溢出");

            Check(TrionFacilitySacrificeUtility.GetMaximumCount(15, 200, 900, 1000) == 7, "全部最多选择七件，最后一件溢出");
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(15, 200, 910, 1000) == 6, "恰好填满不能再加一件");
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(15000, 200, 999, 1000) == 1, "单件高收益只选一件，完整保留收益");
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(15, 3, 0, 1000) == 3, "全部数量不能超过实际供物");
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(15, 200, 1000, 1000) == 0, "已满时不能选择更多供物");
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(15, 200, 900, 1000, 15000) == 0,
                "先选超凡供物达到门槛后，不能把小额供物插到它之前");
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(15000, 200, 900, 1000, 105) == 0,
                "其他供物已经填满时不能再选择高收益的一件");
            // 小数单件收益也必须使数量按钮与最终容量校验一致。
            float fractionalYield = 12.424242f;
            double screenshotYield = (double)fractionalYield * 81;
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(fractionalYield, 9987, 0, 1000, 0, 81) == 81,
                "选满后的本行保持81件上限，仍可减少但不能增加");
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(1.1304348f, 5, 0, 1000, screenshotYield) == 0,
                "截图81件零部件已溢出后，果子不能再增加");
            double belowThreshold = (double)fractionalYield * 80;
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(1.1304348f, 200, 0, 1000, belowThreshold) == 6,
                "减为80件后，最后添加果子的全部只能增加六件，不能把零部件视为末项");
            Check(TrionFacilitySacrificeUtility.GetMaximumCount(15, 200, 0, 1000, 500, 30) == 34,
                "本行已有30件，剩余50收益只能再加四件，而不是重新按最高收益算上限");
            int limit = TrionFacilitySacrificeUtility.GetMaximumCount(fractionalYield, 200, 287.78f, 1000);
            Check(TrionFacilitySacrificeUtility.CanAcceptBatch((double)fractionalYield * limit, fractionalYield, 287.78f, 1000)
                && !TrionFacilitySacrificeUtility.CanAcceptBatch((double)fractionalYield * (limit + 1), fractionalYield, 287.78f, 1000),
                "小数收益的上限与最终提交必须一致");

            Pawn royal;
            CompTrionFacilityResource resource;
            var control = Core(out royal, out resource);
            resource.TryAddResource(900);
            var second = Item(TechLevel.Industrial, count: 20);
            bulk.Map = second.Map = royal.Map;
            batch = new List<ThingCountClass> { new ThingCountClass(bulk, 10), new ThingCountClass(second, 10) };
            string problem;
            Check(!control.TrySacrifice(royal, batch, out problem), "不能用整批溢出绕过单件上限");
            Equal(900, resource.CurrentResource, "超量批次不能入账");
            Check(bulk.stackCount == 20 && second.stackCount == 20, "超量拒绝不得消费供物");
            batch = new List<ThingCountClass> { new ThingCountClass(bulk, 4), new ThingCountClass(second, 3) };
            Check(control.TrySacrifice(royal, batch, out problem), "最后一件可完整溢出");
            Equal(1005, resource.CurrentResource, "同种跨堆共用最后一件溢出额度");
            Check(bulk.stackCount == 16 && second.stackCount == 17, "只消费选中的七件");
            Check(!control.TrySacrifice(royal, batch, out problem), "达到门槛拒绝新补充");
            Check(bulk.stackCount == 16 && second.stackCount == 17, "门槛拒绝不得吞供物");
            Check(!control.TrySacrifice(new Pawn { Map = royal.Map }, batch, out problem), "无关皇室没有操作权限");

            control = Core(out royal, out resource);
            resource.TryAddResource(910);
            bulk = Item(TechLevel.Industrial, count: 7);
            bulk.Map = royal.Map;
            Check(!control.TrySacrifice(royal, new List<ThingCountClass> { new ThingCountClass(bulk, 7) }, out problem),
                "前六件恰好填满时不能再收第七件");
            Check(control.TrySacrifice(royal, new List<ThingCountClass> { new ThingCountClass(bulk, 6) }, out problem),
                "恰好填满的数量可提交");
            Equal(1000, resource.CurrentResource, "恰好满额不多收一件");

            control = Core(out royal, out resource);
            resource.TryAddResource(900);
            bulk = Item(TechLevel.Industrial, count: 7);
            second = Item(TechLevel.Archotech, count: 2);
            bulk.Map = second.Map = royal.Map;
            batch = new List<ThingCountClass> { new ThingCountClass(second, 1), new ThingCountClass(bulk, 7) };
            Check(!control.TrySacrifice(royal, batch, out problem), "混合供物不能每类各溢出一件");
            Check(bulk.stackCount == 7 && second.stackCount == 2, "混合超量拒绝保留全部实物");
            batch[1] = new ThingCountClass(bulk, 6);
            Check(!control.TrySacrifice(royal, batch, out problem), "最后选低收益时不能把前面的超凡一件挪到最后");
            batch.Reverse();
            Check(control.TrySacrifice(royal, batch, out problem), "小额供物先填充，最后一件高收益供物允许完整溢出");
            Equal(15990, resource.CurrentResource, "单件超凡收益完整保留，不限制为一个资源点");
            Check(bulk.stackCount == 1 && second.stackCount == 1, "混合供物只消费合法数量");

            control = Core(out royal, out resource);
            var partner = new Pawn { Map = royal.Map };
            royal.health.hediffSet.hediffs.Add(new Hediff_RoyalContract
            {
                def = ((CompProperties_TrionFacilityControl)control.props).contractHediff,
                partner = partner, royalSigner = true
            });
            royal.health.capacities.awake = false;
            resource.RefreshOperation();
            bulk = Item(TechLevel.Industrial, count: 3);
            bulk.Map = royal.Map;
            batch = new List<ThingCountClass> { new ThingCountClass(bulk, 3) };
            bulk.MarketValue = 50;
            Check(control.TrySacrifice(partner, batch, out problem), "皇室昏迷时契约者仍可补充");
            Equal(40, resource.CurrentResource, "最终提交须读取实际状态后的价值");
            Check(bulk.Destroyed && !resource.IsOperational && control.BoundRoyal == royal, "消费成功但不能偷偷唤醒或改绑定");
            bulk = Item(TechLevel.Industrial);
            bulk.Map = royal.Map;
            royal.genes.gene.Active = false;
            Check(!control.TrySacrifice(partner, new List<ThingCountClass> { new ThingCountClass(bulk, 1) }, out problem), "执行前皇室失效则拒绝");
            Check(!bulk.Destroyed && bulk.stackCount == 1, "资格失效不得消费供物");
            // 装入核心后物品不再在地图生成；只能接受本核心实际持有，不能接受别处货物。
            control = Core(out royal, out resource);
            var cargo = new CompTransporter { parent = control.parent };
            control.parent.comps.Add(cargo);
            bulk = Item(TechLevel.Industrial, count: 3);
            bulk.Spawned = false;
            bulk.Map = null;
            batch = new List<ThingCountClass> { new ThingCountClass(bulk, 3) };
            Check(!control.TrySacrifice(royal, batch, out problem), "其他容器或人物持有的实物不能用于本核心提交");
            Check(!bulk.Destroyed && bulk.stackCount == 3, "错误归属拒绝不消费");
            cargo.innerContainer.Add(bulk);
            double preflightYield;
            Check(control.CanSacrifice(royal, batch, out preflightYield, out problem), "本核心已搬入供物可用于最终核验");
            Equal(45, (float)preflightYield, "已装供物按实际数量计算收益");
            Equal(0, resource.CurrentResource, "准备和派工核验不能提前入账");
            Check(!bulk.Destroyed && bulk.stackCount == 3, "核验本身不能消费供物");
            Check(control.TrySacrifice(royal, batch, out problem), "已装入本核心的完整实物可以提交");
            Equal(45, resource.CurrentResource, "已装供物最终入账一次");
            Check(bulk.Destroyed, "只有最终成功后才消费已搬入供物");
            Console.WriteLine("通过：原生科技准入、科技断档、固有市值、组件例外、缩小建筑、单件溢出、混合供物、权限和拒绝不吞物品。");
        }
    }
}
