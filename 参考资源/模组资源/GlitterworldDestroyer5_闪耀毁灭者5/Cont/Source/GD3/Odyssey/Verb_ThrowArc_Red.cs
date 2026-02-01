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
    public class Verb_ThrowArc_Red : Verb_ThrowArc
    {
        public override void LightningStrike(Thing thing, Thing source, bool applyOffset = false)
        {
            List<IntVec3> cells = GenRadial.RadialCellsAround(thing.PositionHeld, 2.9f, false).ToList();
            IEnumerable<IntVec3> selected = cells.TakeRandom(Rand.Range(1, 4));
            foreach (IntVec3 cell in selected)
            {
                if (!cell.InBounds(thing.MapHeld))
                {
                    continue;
                }
                ThunderArrow arrow = (ThunderArrow)ThingMaker.MakeThing(GDDefOf.GD_ThunderArrow);
                arrow.launcher = thing;
                arrow.instigator = caster;
                arrow.timeToLaunch = Find.TickManager.TicksGame + 1;
                arrow.instant = true;
                arrow.dontPlaySound = true;
                SoundInfo info = thing;
                info.volumeFactor = 0.4f;
                GDDefOf.ThunderArrowShoot.PlayOneShot(info);
                GenSpawn.Spawn(arrow, cell, thing.MapHeld);
            }

            FleckMaker.ConnectingLine(source.TrueCenter() + (applyOffset ? drawLoc : Vector3.zero), thing.TrueCenter(), FleckDefOf.LightningGlow, caster.Map, 1f);
            FleckMaker.ConnectingLine(source.TrueCenter() + (applyOffset ? drawLoc : Vector3.zero), thing.TrueCenter(), GDDefOf.GD_LightningChain_Red, caster.Map, 2f);
            Ext.sound?.PlayOneShot(new TargetInfo(currentTarget.Cell, caster.Map));
            float damage = EquipmentSource.QualityFactor() * Ext.amount * ((thing is Pawn p && p.RaceProps.IsMechanoid) ? 2f : 1f);
            float penetration = EquipmentSource.QualityFactor() * Ext.penetration;
            thing.TakeDamage(new DamageInfo(Ext.damage, damage, penetration, -1, caster, null, EquipmentSource?.def));
            thing.TakeDamage(new DamageInfo(DamageDefOf.EMP, Ext.EMPdamage, 999, -1, caster, null, EquipmentSource?.def));
        }
    }
}