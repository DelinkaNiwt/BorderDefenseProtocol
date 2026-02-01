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
    public class QuestPart_PlaceThing : QuestPart
    {
        public string inSignal;

        public Site site;

        public ThingDef thingDef;

        public IntVec3 offset = IntVec3.Zero;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                Map map = site.Map;
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(thingDef), map.Center + offset, map, ThingPlaceMode.Direct);
                Log.Message(thingDef.defName + " placed.");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref offset, "offset", IntVec3.Zero);
            Scribe_References.Look(ref site, "site");
            Scribe_Defs.Look(ref thingDef, "thingDef");
        }
    }
}