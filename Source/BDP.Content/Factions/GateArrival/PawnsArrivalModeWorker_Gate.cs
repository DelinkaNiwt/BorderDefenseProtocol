using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>近界宙域的门抵达；位置暂借原版边缘入口，队伍策略不由门系统创建。</summary>
    public sealed class PawnsArrivalModeWorker_Gate : PawnsArrivalModeWorker
    {
        /// <summary>仅准入近界派系，保留原版地图和派系黑白名单约束。</summary>
        public override bool CanUseWith(IncidentParms parms)
        {
            if (parms.faction?.def?.defName != "BDP_Faction_NearRealm" || parms.raidArrivalModeForQuickMilitaryAid) return false;
            // 原版检查抵达方式白黑名单、袭击策略候选列表和地图条件。
            return base.CanUseWith(parms);
        }

        /// <summary>皇室任务仅调用地图检查；人物派系在实际抵达时另行验证。</summary>
        public override bool CanUseOnMap(Map map)
        { return base.CanUseOnMap(map) && PawnsArrivalModeDefOf.EdgeWalkIn.Worker.CanUseOnMap(map); }

        /// <summary>保留原版入口和朝向解析，门落点样式由当前抵达方式单独选择。</summary>
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        { return PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(parms); }

        /// <summary>原版已生成人物后接管尚未落地的部分，后续仍由原版建立整波队伍。</summary>
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            if (pawns.NullOrEmpty()) return;
            Map map = (Map)parms.target;
            if (pawns.Exists(p => p.Faction?.def?.defName != "BDP_Faction_NearRealm") || !CanUseOnMap(map))
            { PawnsArrivalModeDefOf.EdgeWalkIn.Worker.Arrive(pawns, parms); return; }
            GateArrivalModeExtension arrivalMode = def.GetModExtension<GateArrivalModeExtension>();
            GateArrivalSettings settings = arrivalMode?.settings ?? new GateArrivalSettings();
            List<IntVec3> positions = GateArrivalLanding.FindPositions(map, settings.GateCountFor(pawns.Count),
                parms.spawnCenter, settings, arrivalMode);
            // 新算法没有有效候选时回到原版边缘落点，确保不丢弃已生成的袭击人员。
            if (positions.Count == 0 && arrivalMode != null && arrivalMode.landingPattern != GateArrivalLandingPattern.Edge)
                positions = GateArrivalLanding.FindEdgePositions(map, settings.GateCountFor(pawns.Count),
                    parms.spawnCenter, settings.placementRadius);
            // 极端地图没有可用门位置时保留原版抵达能力，不丢弃已生成的袭击人员。
            if (positions.Count == 0) { PawnsArrivalModeDefOf.EdgeWalkIn.Worker.Arrive(pawns, parms); return; }
            // 原版袭击入口提供raidStrategy；普通任务人物抵达不因人物敌对就自动归为袭击。
            bool hostileRaid = parms.raidStrategy != null && parms.faction != null
                && parms.faction.HostileTo(Faction.OfPlayer);
            Thing_GateArrival.Start(map, pawns, positions, settings, parms.spawnRotation, hostileRaid);
        }
    }
}
