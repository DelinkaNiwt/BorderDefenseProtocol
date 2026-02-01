using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class HediffCompProperties_ShieldGenerator : HediffCompProperties
    {
        public HediffCompProperties_ShieldGenerator()
        {
            this.compClass = typeof(HediffComp_ShieldGenerator);
        }

        public float range;
    }

    public class HediffComp_ShieldGenerator : HediffComp
    {
        public HediffCompProperties_ShieldGenerator Props
        {
            get
            {
                return (HediffCompProperties_ShieldGenerator)this.props;
            }
        }

        public bool CanApply
        {
            get
            {
                Pawn pawn = base.Pawn;
                if (pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.Faction != null)
                {
                    if (!pawn.Faction.IsPlayer)
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
                List<Thing> projectiles = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
                if (projectiles.Count <= 0)
                {
                    return;
                }
                Projectile p = projectiles[0] as Projectile;
                if (p != null && (p.Launcher == null || p.Launcher.Faction == null || p.Launcher.Faction.HostileTo(pawn.Faction)) && p.Position.DistanceTo(pawn.Position) <= this.Props.range)
                {
                    this.GenerateShield(pawn.Position, pawn.Map, pawn.Faction, p.def.projectile.flyOverhead);
                    pawn.health.RemoveHediff(this.parent);
                }
            }
        }

        public void GenerateShield(IntVec3 pos, Map map, Faction f, bool flyOverhead)
        {
            if (pos.IsValid && pos.InBounds(map))
            {
                ThingDef thingDef;
                if (flyOverhead)
                {
                    thingDef = GDDefOf.GD_FullAngelShieldProjector;
                }
                else
                {
                    thingDef = GDDefOf.GD_LowAngelShieldProjector;
                }
                Thing shield = ThingMaker.MakeThing(thingDef, null);
                shield.SetFaction(f);
                GenPlace.TryPlaceThing(shield, pos, map, ThingPlaceMode.Near, null, null, default(Rot4));
                HediffComp_ShieldGenerator.SpawnEffect(shield);
            }
        }
        private static void SpawnEffect(Thing projector)
        {
            FleckMaker.Static(projector.TrueCenter(), projector.Map, FleckDefOf.BroadshieldActivation, 1f);
            SoundDefOf.Broadshield_Startup.PlayOneShot(new TargetInfo(projector.Position, projector.Map, false));
        }
    }
}