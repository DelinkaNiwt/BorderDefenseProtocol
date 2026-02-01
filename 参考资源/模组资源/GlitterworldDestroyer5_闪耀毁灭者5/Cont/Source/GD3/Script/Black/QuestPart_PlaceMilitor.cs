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
    public class QuestPart_PlaceMilitor : QuestPart
    {
        public string inSignal;

        public Site site;

        public Pawn militor;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                Map map = site.Map;
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.GD_ServerDummy), map.Center, map, ThingPlaceMode.Direct);
                if (GDSettings.DeveloperMode)
                {
                    Log.Warning("Dummy has been placed.");
                }

                GenPlace.TryPlaceThing(militor, map.Center, map, ThingPlaceMode.Direct);
                if (GDSettings.DeveloperMode)
                {
                    Log.Warning("Pawn has been placed.");
                }

                Lord lord = LordMaker.MakeNewLord(militor.Faction, new LordJob_MechanoidsDefend(new List<Thing>(), militor.Faction, 12f, map.Center, false, false), map);
                lord.AddPawn(militor);
                CompMilitor comp = militor.TryGetComp<CompMilitor>();
                comp.active = true;

                Messages.Message("GD.MilitorMissionStart".Translate(), militor, MessageTypeDefOf.NeutralEvent);

                Hediff hediff = militor.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_Militor, false);
                if (hediff == null)
                {
                    hediff = militor.health.AddHediff(GDDefOf.GD_Militor, militor.health.hediffSet.GetBrain(), null, null);
                    hediff.Severity = 1f;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref site, "site");
            Scribe_References.Look(ref militor, "militor");
        }
    }
}