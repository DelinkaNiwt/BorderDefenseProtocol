using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using Verse.Sound;
using RimWorld;

namespace GD3
{
    public class QuestPart_InitiateComet : QuestPart
    {
        public string inSignal;

        public Pawn pawn;

        public PawnKindDef pawnKindDef;

        public FactionDef factionDef;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                ThingWithComps comet = (ThingWithComps)ThingMaker.MakeThing(GDDefOf.BlackStrike_Pod);
                CompSpawnThingOnDestroy comp = comet.TryGetComp<CompSpawnThingOnDestroy>();
                comp.pawnKindDef = pawnKindDef;
                comp.faction = factionDef;
                nextExplosionCell = (from x in GenRadial.RadialCellsAround(pawn.Position, impactAreaRadius, useCenter: true)
                                     where x.InBounds(pawn.Map) && x.Walkable(pawn.Map) && x.Standable(pawn.Map)
                                     select x).RandomElementByWeight((IntVec3 x) => DistanceChanceFactor.Evaluate(x.DistanceTo(pawn.Position) / impactAreaRadius));
                if (nextExplosionCell.IsValid)
                {
                    GenPlace.TryPlaceThing(comet, nextExplosionCell, pawn.Map, ThingPlaceMode.Direct);
                    Messages.Message("GD.HelpArrive".Translate(), comet, MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    GenPlace.TryPlaceThing(comet, pawn.Position, pawn.Map, ThingPlaceMode.Direct);
                    Messages.Message("GD.HelpArrive".Translate(), comet, MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Defs.Look(ref pawnKindDef, "pawnKindDef");
            Scribe_Defs.Look(ref factionDef, "factionDef");
            Scribe_Values.Look(ref inSignal, "inSignal");
        }

        private float impactAreaRadius = 4.9f;

        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(1f, 0.1f)
        };
    }
}