using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound;
using System.Linq;

namespace GD3
{
    public class HediffComp_BlackShield : HediffComp
    {
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Pawn p = this.parent.pawn;
            EffecterDefOf.Shield_Break.SpawnAttached(p, p.MapHeld, 1.8f);
            FleckMaker.Static(p.TrueCenter(), p.Map, FleckDefOf.ExplosionFlash, 12f);
            for (int i = 0; i < 6; i++)
            {
                FleckMaker.ThrowDustPuff(p.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), p.Map, Rand.Range(0.8f, 1.2f));
            }
            List<Pawn> pawns = p.Map.mapPawns.AllPawns.FindAll((Pawn p0) => (p0.Faction == null || p0.Faction.HostileTo(p.Faction)) && p0.Position.DistanceTo(p.Position) < 2.9f);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                for (int j = 0; j < 5; j++)
                {
                    DamageVictim(pawn);
                }
            }
        }

        public void DamageVictim(Pawn victim)
        {
            if (victim.Dead)
            {
                return;
            }
            HediffSet hediffSet = victim.health.hediffSet;
            IEnumerable<BodyPartRecord> source = from x in HittablePartsViolence(hediffSet)
                                                 where !victim.health.hediffSet.hediffs.Any((Hediff y) => y.Part == x && y.CurStage != null && y.CurStage.partEfficiencyOffset < 0f)
                                                 select x;
            BodyPartRecord bodyPartRecord = source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
            if (bodyPartRecord == null)
            {
                return;
            }
            int num = 30;
            Pawn p = this.parent.pawn;
            victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, (float)num, 10f, 0f, p, bodyPartRecord, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
        }
        private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
        {
            return from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
                   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
                   select x;
        }
    }
}
