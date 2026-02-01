using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace GD3
{
    public class DryseaDummy : ThingWithComps
    {
        public bool IfPawnStanding
        {
            get
            {
                Pawn pawn = this.Position.GetFirstPawn(this.Map);
                if (pawn != null && pawn.Faction != null && pawn.RaceProps.Humanlike && pawn.Faction == Faction.OfPlayer)
                {
                    this.pawn = pawn;
                    return true;
                }
                this.pawn = null;
                return false;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!map.thingGrid.ThingsListAt(Position).Any(t => t.def == GDDefOf.Plant_PeaceLily))
            {
                Thing lily = ThingMaker.MakeThing(GDDefOf.Plant_PeaceLily);
                GenPlace.TryPlaceThing(lily, Position, map, ThingPlaceMode.Direct);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (IfPawnStanding && !triggered)
            {
                ticks++;
                if (ticks >= 60)
                {
                    triggered = true;
                    GDUtility.SendSignal(GDUtility.GetQuestOfThing(this), "Teleported");
                }
            }
            else
            {
                ticks = 0;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref triggered, "triggered");
        }

        private Pawn pawn = null;

        private int ticks;

        public bool triggered;
    }
}
