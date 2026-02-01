using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse.AI;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class Projectile_PocketThunder : Projectile_Explosive
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shockTick, "shockTick", 0);
        }

        public bool ShouldShowEffecter()
        {
            if (Spawned)
            {
                return MapHeld == Find.CurrentMap;
            }
            return false;
        }

        protected override void Tick()
        {
            base.Tick();
            this.shockTick++;
            if (this.shockTick > 40)
            {
                this.shockTick = 0;
                IEnumerable<Pawn> enumerable = from x in this.Map.mapPawns.AllPawnsSpawned
                                               where x.PositionHeld.DistanceTo(this.PositionHeld) < 9.9f && x.HostileTo(this.Launcher)
                                               select x;
                if (enumerable.Count() == 0)
                {
                    return;
                }
                GDDefOf.PocketThunderWave.PlayOneShot(this);
                List<Pawn> list = enumerable.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    Pawn pawn = list[i];
                    FleckMaker.ConnectingLine(this.DrawPos, pawn.DrawPos, GDDefOf.GD_LightningChain_Red, this.Map, 3f);
                    DamageVictim(pawn);
                }
            }
            if (ShouldShowEffecter())
            {
                if (effecter == null)
                {
                    effecter = GDDefOf.ApocrionAttached.SpawnAttached(this, MapHeld);
                }
                if (effecterOri == null)
                {
                    effecterOri = GDDefOf.PocketThunderEffect.SpawnAttached(this, MapHeld);
                }
                effecter?.EffectTick(this, this);
                if (shockTick % 6 == 0)
                {
                    effecterOri?.EffectTick(this, this);
                }
            }
            else
            {
                effecter?.Cleanup();
                effecter = null;
                effecterOri?.Cleanup();
                effecterOri = null;
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
            int maxHitPoints = bodyPartRecord.def.hitPoints;
            int num = (int)(maxHitPoints / victim.GetStatValue(StatDefOf.IncomingDamageFactor)) * 4;
            Pawn apocriton = this.Launcher as Pawn;
            GDDefOf.PocketThunderWave.PlayOneShot(victim);
            victim.TakeDamage(new DamageInfo(DamageDefOf.Burn, (float)num, 10f, 0f, apocriton, bodyPartRecord, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
        }
        private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
        {
            return from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
                   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
                   select x;
        }

        private int shockTick = 0;

        private Effecter effecterOri;

        private Effecter effecter;
    }
}
