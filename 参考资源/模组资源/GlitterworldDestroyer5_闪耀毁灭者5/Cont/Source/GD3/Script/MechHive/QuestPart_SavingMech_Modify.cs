using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;

namespace GD3
{
    public class QuestPart_SavingMech_Modify : QuestPart
    {
        public string inSignal;

        public Pawn mech;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal && mech != null)
            {
                HediffWithComps hediff = (HediffWithComps)mech.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_SavingMech);
                HediffComp_SavingMech comp = hediff.TryGetComp<HediffComp_SavingMech>();
                comp.allowModify = true;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref mech, "mech");
        }
    }
}