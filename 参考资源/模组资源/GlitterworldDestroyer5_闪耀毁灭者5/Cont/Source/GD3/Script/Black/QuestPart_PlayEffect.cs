using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;

namespace GD3
{
    public class QuestPart_PlayEffect : QuestPart
    {
        public string inSignal;

        public Thing thing;

        public Map map;

        public EffecterDef effecter;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                Effecter effect = effecter.Spawn(thing, thing.Map, 1f);
                effect.Trigger(thing, thing, -1);
                effect.Cleanup();
                SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(thing.Position, thing.Map, false));

                quest.End(QuestEndOutcome.Success, true, true);

                CompAnalyzableSubcore comp = thing.TryGetComp<CompAnalyzableSubcore>();
                if (comp != null)
                {
                    Find.AnalysisManager.TryGetAnalysisProgress(comp.AnalysisID, out AnalysisDetails details);
                    if (details != null && details.Satisfied)
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref thing, "thing");
            Scribe_References.Look(ref map, "map");
            Scribe_Defs.Look(ref effecter, "effecter");
            Scribe_Values.Look(ref inSignal, "inSignal");
        }
    }
}