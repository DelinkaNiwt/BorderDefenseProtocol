using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class HediffComp_Reboot : HediffComp
    {
        public HediffCompProperties_Reboot Props
        {
            get
            {
                return (HediffCompProperties_Reboot)this.props;
            }
        }

        public bool CanApply
        {
            get
            {
                Pawn pawn = base.Pawn;
                if (ModsConfig.BiotechActive && pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.Faction != null)
                {
                    if (pawn.Faction.IsPlayer)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = base.Pawn;
            if (this.CanApply)
            {
                if (pawn.CurJobDef == null || (pawn.CurJobDef != JobDefOf.MechCharge && pawn.CurJobDef != JobDefOf.SelfShutdown))
                {
                    pawn.needs.energy.CurLevel -= 0.001f;
                }
                HediffWithComps hediff = this.parent;
                if (hediff != null && pawn.CurJobDef != null && pawn.CurJobDef == JobDefOf.SelfShutdown)
                {
                    if (hediff.Severity >= 0.03f)
                    {
                        MoteMaker.ThrowText(pawn.TrueCenter(), pawn.Map, "CentipedeReboot".Translate(), 4.5f);
                        SoundDefOf.DisconnectedMech.PlayOneShot(new TargetInfo(pawn.PositionHeld, pawn.MapHeld, false));
                    }
                    hediff.Severity = 0.000000001f;
                }
            }
        }
    }
}