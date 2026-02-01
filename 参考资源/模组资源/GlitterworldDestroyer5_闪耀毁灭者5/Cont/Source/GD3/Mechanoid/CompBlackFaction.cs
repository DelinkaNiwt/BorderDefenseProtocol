using System;
using Verse;
using RimWorld;
using Verse.AI;
using Verse.AI.Group;

namespace GD3
{
    public class CompBlackFaction : ThingComp
    {
        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid).HostileTo(Faction.OfPlayer) || totalDamageDealt <= 0 && dinfo.Def.causeStun == true)
            {
                return;
            }
            if (dinfo.Instigator?.Faction != null && this.parent.Faction != null && this.parent.Faction.def == GDDefOf.BlackMechanoid && dinfo.Instigator.Faction == Faction.OfPlayer)
            {
                if (inMission == true || (((Pawn)parent).TryGetLord(out Lord lord) && lord.LordJob.GetType() == typeof(LordJob_AssistColony)))
                {
                    Find.World.GetComponent<MissionComponent>().BlackMechRelationOffset(-10);
                    Messages.Message("GD.DamageBlack".Translate(10), MessageTypeDefOf.NeutralEvent);
                }
                else
                {
                    Find.World.GetComponent<MissionComponent>().BlackMechRelationOffset(-20);
                    Messages.Message("GD.DamageBlack".Translate(20), MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        public override void Notify_Downed()
        {
            base.Notify_Downed();

            ((Pawn)parent).Kill(null);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref inMission, "inMission", false, false);
        }

        public bool inMission = false;
    }
}
