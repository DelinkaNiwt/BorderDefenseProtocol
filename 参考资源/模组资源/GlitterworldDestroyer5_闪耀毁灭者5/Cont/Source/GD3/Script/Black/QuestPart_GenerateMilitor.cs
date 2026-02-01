using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System;
using Verse;
using Verse.Sound;
using RimWorld;
using Verse.AI.Group;
using RimWorld.QuestGen;

namespace GD3
{
    public class QuestPart_GenerateMilitor : QuestPart
    {
        public string inSignal;

        public Site site;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                if (Find.World.GetComponent<MissionComponent>().militorSpawned)
                {
                    return;
                }
                Map map = site.Map;
                Thing dummy = ThingMaker.MakeThing(GDDefOf.GD_EliminateDummy);
                GenPlace.TryPlaceThing(dummy, map.Center, map, ThingPlaceMode.Direct);
                Pawn pawn = PawnGenerator.GeneratePawn(GDDefOf.Mech_BlackMilitor, Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid));
                if (Find.World.GetComponent<MissionComponent>().BranchDict.TryGetValue("WillMilitorDie", true))
                {
                    GenPlace.TryPlaceThing(pawn, dummy.Position, dummy.Map, ThingPlaceMode.Direct);
                    HealthUtility.DamageUntilDead(pawn, GDDefOf.Beam);
                }
                else
                {
                    GenPlace.TryPlaceThing(pawn, dummy.Position, dummy.Map, ThingPlaceMode.Direct);
                    Lord lord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_WaitAtPoint(dummy.Position), dummy.Map);
                    lord.AddPawn(pawn);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "site");
            Scribe_Values.Look(ref inSignal, "inSignal");
        }
    }
}