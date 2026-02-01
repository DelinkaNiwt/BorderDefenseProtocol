using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;

namespace GD3
{
    public class QuestPart_AdjustProgress : QuestPart
    {
        public string inSignal;

        public int progress;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                GDUtility.MissionComponent.progress += progress;
                if (GDUtility.MissionComponent.progress > GDUtility.MissionComponent.EndingProgress)
                {
                    if (true)
                    {

                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref progress, "progress");
        }
    }
}