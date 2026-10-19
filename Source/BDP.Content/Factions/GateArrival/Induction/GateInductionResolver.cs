using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>本建筑唯一的门解析订阅；不为每台装置建立全局注册或每刻扫描。</summary>
    [StaticConstructorOnStartup]
    public static class GateInductionResolver
    {
        /// <summary>在原版装载模组时接入现有业务钩子，不修改原版抵达流程。</summary>
        static GateInductionResolver() { GateArrivalHooks.ResolvingDestination += Resolve; }

        /// <summary>初始规划时选择最近的运作装置，再随机提交合法候选；不要求波已生成。</summary>
        private static void Resolve(GateRedirectRequest request)
        {
            Thing_GateArrival wave = request.Wave;
            if (wave == null || request.Map == null || request.GateIndex < 0 || request.GateIndex >= wave.Gates.Count) return;
            Map map = request.Map;
            GateArrivalPoint gate = wave.Gates[request.GateIndex];
            // 必须比较启动时原定位置，不能让此前的外部重定向改变“最近装置”的含义。
            CompGateInducer inducer = map.listerThings.ThingsOfDef(GateArrivalDefOf.BDP_GateInducer)
                .Where(thing => thing.Faction == Faction.OfPlayer)
                .Select(thing => thing.TryGetComp<CompGateInducer>())
                .Where(comp => comp != null && comp.IsOperational && comp.Accepts(wave))
                .OrderBy(comp => comp.parent.Position.DistanceToSquared(gate.OriginalPosition))
                .ThenBy(comp => comp.parent.thingIDNumber).FirstOrDefault();
            if (inducer == null) return;
            // 当前波尚未入地图索引；显式合并已规划门位，避免同波及重叠袭击出现同格。
            var occupiedGateCells = GateArrivalHooks.ActiveWaves(map).SelectMany(otherWave => otherWave.Gates)
                .Concat(wave.Gates.Take(request.GateIndex)).Select(other => other.Position).ToList();
            var candidates = GenRadial.RadialCellsAround(inducer.parent.Position, inducer.Radius, true)
                .Where(cell => Thing_GateArrival.IsValidDestination(map, cell)
                    && cell.GetEdifice(map) == null
                    && !occupiedGateCells.Contains(cell)).ToList();
            // 最近装置周围无合法格时不抢占更远装置，也不阻塞开门，原定位置作为既有回退。
            if (candidates.Count == 0) return;
            // 初始规划只解析一次；失败不提交提案，保留原候选，不转交第二台装置，也不在开门时重试。
            if (!Rand.Chance(inducer.SuccessChance)) return;
            request.Propose(candidates.RandomElement(), inducer.RedirectPriority, inducer.parent.ThingID);
        }
    }
}
