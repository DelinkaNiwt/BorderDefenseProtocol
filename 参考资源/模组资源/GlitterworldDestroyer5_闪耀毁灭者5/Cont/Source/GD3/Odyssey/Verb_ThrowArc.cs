using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.Sound;
using Verse;
using Verse.AI;

namespace GD3
{
    [StaticConstructorOnStartup]
    public class Verb_ThrowArc : Verb
    {
        private List<Thing> tmpHitThings = new List<Thing>();

        protected override int ShotsPerBurst => base.BurstShotCount;

        public Ext_Arc Ext => EquipmentSource?.def?.GetModExtension<Ext_Arc>();

        public Vector3 drawLoc;

        public override void WarmupComplete()
        {
            base.WarmupComplete();
            Find.BattleLog.Add(new BattleLogEntry_RangedFire(caster, currentTarget.HasThing ? currentTarget.Thing : null, base.EquipmentSource?.def, null, ShotsPerBurst > 1));
        }

        protected override bool TryCastShot()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
            {
                return false;
            }

            ShootLine resultingLine;
            bool flag = TryFindShootLineFromTo(caster.Position, currentTarget, out resultingLine);
            if (verbProps.stopBurstWithoutLos && !flag)
            {
                return false;
            }

            if (base.EquipmentSource != null)
            {
                base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
                base.EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
            }

            lastShotTick = Find.TickManager.TicksGame;

            if (Ext != null)
            {
                tmpHitThings.Clear();
                int count = Ext.chainCount.RandomInRange;
                for (int i = 0; i < count; i++)
                {
                    Thing target;
                    Thing source;
                    if (i == 0)
                    {
                        target = currentTarget.Thing;
                        source = caster;
                    }
                    else
                    {
                        IEnumerable<Thing> list = GenRadial.RadialDistinctThingsAround(tmpHitThings[i - 1].PositionHeld, caster.Map, Ext.range, false).Where(t => t is IAttackTarget tar && !tar.ThreatDisabled(caster as IAttackTargetSearcher) && (t.Faction == null || t.Faction.HostileTo(caster.Faction)) && !tmpHitThings.Contains(t));
                        if (!list.Any())
                        {
                            break;
                        }
                        target = list.RandomElementByWeight(t => 1 / t.PositionHeld.DistanceTo(tmpHitThings[i - 1].PositionHeld));
                        source = tmpHitThings[i - 1];
                    }
                    if (target != null)
                    {
                        tmpHitThings.Add(target);
                        LightningStrike(target, source, i == 0);
                    }
                }
            }

            return true;
        }

        protected IEnumerable<IntVec3> GetBeamHitNeighbourCells(IntVec3 source, IntVec3 pos)
        {
            if (!verbProps.beamHitsNeighborCells)
            {
                yield break;
            }

            for (int i = 0; i < 4; i++)
            {
                IntVec3 intVec = pos + GenAdj.CardinalDirections[i];
                if (intVec.InBounds(Caster.Map) && (!verbProps.beamHitsNeighborCellsRequiresLOS || GenSight.LineOfSight(source, intVec, caster.Map)))
                {
                    yield return intVec;
                }
            }
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            return base.TryStartCastOn(verbProps.beamTargetsGround ? ((LocalTargetInfo)castTarg.Cell) : castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }

        public virtual void LightningStrike(Thing thing, Thing source, bool applyOffset = false)
        {
            FleckMaker.ConnectingLine(source.TrueCenter() + (applyOffset ? drawLoc : Vector3.zero), thing.TrueCenter(), FleckDefOf.LightningGlow, caster.Map, 1f);
            FleckMaker.ConnectingLine(source.TrueCenter() + (applyOffset ? drawLoc : Vector3.zero), thing.TrueCenter(), GDDefOf.GD_LightningChain_Blue, caster.Map, 2f);
            Ext.sound?.PlayOneShot(new TargetInfo(currentTarget.Cell, caster.Map));
            float damage = EquipmentSource.QualityFactor() * Ext.amount * ((thing is Pawn p && p.RaceProps.IsMechanoid) ? 2f : 1f);
            float penetration = EquipmentSource.QualityFactor() * Ext.penetration;
            thing.TakeDamage(new DamageInfo(Ext.damage, damage, penetration, -1, caster, null, EquipmentSource?.def));
            thing.TakeDamage(new DamageInfo(DamageDefOf.EMP, Ext.EMPdamage, 999, -1, caster, null, EquipmentSource?.def));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref drawLoc, "drawLoc");
        }
    }
}