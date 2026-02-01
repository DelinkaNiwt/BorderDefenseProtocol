using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using Verse;

namespace GD3
{
    public class QuestPart_InitiateBuildRequest : QuestPart
    {
        public string inSignal;

        public WorldObject settlement;

        public List<ThingDef> requestedThingDef;

        public List<int> requestedCount;

        public bool keepAfterQuestEnds;

        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo questLookTarget in base.QuestLookTargets)
                {
                    yield return questLookTarget;
                }

                if (settlement != null)
                {
                    yield return settlement;
                }
            }
        }

        public override IEnumerable<Faction> InvolvedFactions
        {
            get
            {
                foreach (Faction involvedFaction in base.InvolvedFactions)
                {
                    yield return involvedFaction;
                }

                if (settlement?.Faction != null)
                {
                    yield return settlement.Faction;
                }
            }
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                foreach (Dialog_InfoCard.Hyperlink hyperlink in base.Hyperlinks)
                {
                    yield return hyperlink;
                }

                foreach (ThingDef def in requestedThingDef)
                {
                    yield return new Dialog_InfoCard.Hyperlink(def);
                }
            }
        }

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (!(signal.tag == inSignal))
            {
                return;
            }

            BuildRequestComp component = settlement.GetComponent<BuildRequestComp>();
            if (component != null)
            {
                component.requestThingDefs = requestedThingDef;
                component.requestCountList = requestedCount;
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (!keepAfterQuestEnds)
            {
                BuildRequestComp component = settlement.GetComponent<BuildRequestComp>();
                if (component != null && component.activeRequest)
                {
                    component.Disable();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Collections.Look(ref requestedThingDef, "requestedThingDef", LookMode.Def);
            Scribe_Collections.Look(ref requestedCount, "requestedCount", LookMode.Value);
            Scribe_Values.Look(ref keepAfterQuestEnds, "keepAfterQuestEnds", defaultValue: false);
        }
    }
}
