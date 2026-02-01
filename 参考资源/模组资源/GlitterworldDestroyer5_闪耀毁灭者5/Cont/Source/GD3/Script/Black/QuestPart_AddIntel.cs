using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;

namespace GD3
{
    public class QuestPart_AddIntel : QuestPart
    {
        public string inSignal;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                MissionComponent component = Find.World.GetComponent<MissionComponent>();
                component.intelligenceAdvanced += 2500;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
        }
    }
}