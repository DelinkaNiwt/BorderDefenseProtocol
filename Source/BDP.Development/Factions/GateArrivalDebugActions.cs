using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Factions.GateArrival;
using LudeonTK;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BDP.Development.Factions
{
    /// <summary>黑洞门实测入口；删除开发程序集即可整体移除，不改变正式波次的逻辑。</summary>
    public static class GateArrivalDebugActions
    {
        /// <summary>四种节奏、纯视觉、重定向及取消均调用正式接口。</summary>
        [DebugAction("BDP", requiresBiotech = true, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode GateMenu()
        {
            var root = new DebugActionNode("BDP_Dev_Gate_Menu".Translate());
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_ParallelBatch".Translate(), DebugActionType.ToolMap, () => Spawn(false, false, false)));
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_ParallelAll".Translate(), DebugActionType.ToolMap, () => Spawn(false, true, false)));
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_SequentialBatch".Translate(), DebugActionType.ToolMap, () => Spawn(true, false, false)));
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_SequentialAll".Translate(), DebugActionType.ToolMap, () => Spawn(true, true, false)));
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_RandomWindow".Translate(), DebugActionType.ToolMap, () => Spawn(false, false, false, true)));
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_Visual".Translate(), DebugActionType.ToolMap, () => Spawn(false, true, true)));
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_Redirect".Translate(), DebugActionType.ToolMap, Redirect));
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_Output".Translate(), action: Output));
            root.AddChild(new DebugActionNode("BDP_Dev_Gate_Cancel".Translate(), action: () =>
            { foreach (var wave in GateArrivalHooks.ActiveWaves(Find.CurrentMap)) wave.Cancel(); }));
            return root;
        }

        /// <summary>鼠标选的是三个示范门的中心，不作为正式落点算法。</summary>
        private static void Spawn(bool sequential, bool all, bool visual, bool randomWindow = false)
        {
            Map map = Find.CurrentMap;
            IntVec3 center = Verse.UI.MouseCell();
            var positions = new List<IntVec3> { center + new IntVec3(-4, 0, 0), center, center + new IntVec3(4, 0, 0) };
            if (positions.Any(cell => !Thing_GateArrival.IsValidDestination(map, cell))) { Reject(); return; }
            var settings = new GateArrivalSettings
            {
                openDelayTicks = 600,
                gateOpenIntervalTicks = sequential ? 120 : 0,
                gateOpenRandomOffsetTicks = new IntRange(0, randomWindow ? 90 : 0),
                releaseDelayTicks = randomWindow ? 300 : 0,
                maintainEffecter = GateArrivalDefOf.BDP_GateMaintainedElectricity,
                releaseAllAtOnce = all,
                releaseIntervalTicks = 60,
                minimumOpenTicks = visual ? 600 : 120
            };
            Faction faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f =>
                f.def.defName == "BDP_Faction_NearRealm" && !f.defeated && f.HostileTo(Faction.OfPlayer));
            if (!visual && faction == null)
            {
                Messages.Message("BDP_Dev_Gate_NoFaction".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            var pawns = new List<Pawn>();
            Thing_GateArrival wave = null;
            try
            {
                if (!visual)
                    for (int i = 0; i < 9; i++) pawns.Add(PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                        DefDatabase<PawnKindDef>.GetNamed("BDP_NearRealm_Scout"), faction, forceGenerateNewPawn: true,
                        canGeneratePawnRelations: false, mustBeCapableOfViolence: true)));
                wave = Thing_GateArrival.Start(map, pawns, positions, settings, Rot4.South, isHostileRaid: !visual);
                if (pawns.Count > 0)
                    LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false,
                        canTimeoutOrFlee: false, canSteal: false), map, pawns);
                Output();
            }
            catch (Exception error)
            {
                if (wave != null) wave.Cancel();
                else foreach (Pawn pawn in pawns) if (!pawn.Destroyed && !pawn.Spawned) Find.WorldPawns.PassToWorld(pawn);
                Log.Error("BDP_Dev_Gate_Error".Translate(error.ToString()));
            }
        }

        /// <summary>鼠标位置作为新目的地，移动距离最近的一扇尚未开启的门。</summary>
        private static void Redirect()
        {
            IntVec3 target = Verse.UI.MouseCell();
            var candidates = GateArrivalHooks.ActiveWaves(Find.CurrentMap).SelectMany(w => w.Gates.Select((g, i) => new { wave = w, gate = g, index = i }))
                .Where(x => !x.gate.IsOpen).OrderBy(x => x.gate.Position.DistanceToSquared(target)).ToList();
            if (candidates.Count == 0 || !candidates[0].wave.TryRedirect(candidates[0].index, target)) { Reject(); return; }
            Output();
        }

        /// <summary>在日志中显示每波、每门当前进度，便于比较读档前后及重叠袭击。</summary>
        private static void Output()
        {
            var waves = GateArrivalHooks.ActiveWaves(Find.CurrentMap);
            Log.Message("BDP_Dev_Gate_Header".Translate(Find.TickManager.TicksGame, waves.Count));
            foreach (var wave in waves)
            {
                Log.Message("BDP_Dev_Gate_Wave".Translate(wave.ThingID, wave.PendingCount, wave.ArrivalComplete.ToString()));
                for (int i = 0; i < wave.Gates.Count; i++)
                {
                    var gate = wave.Gates[i];
                    Log.Message("BDP_Dev_Gate_Point".Translate(i, gate.Position.ToString(), gate.IsOpen.ToString(), gate.IsClosing.ToString(), gate.PendingCount)
                        + "BDP_Dev_Gate_Timing".Translate(gate.PlannedOpenTick, gate.OpenedTick, gate.OriginalPosition.ToString()));
                }
            }
        }

        /// <summary>操作无效时统一使用语言包反馈。</summary>
        private static void Reject() { Messages.Message("BDP_Dev_Gate_Rejected".Translate(), MessageTypeDefOf.RejectInput, false); }
    }
}
