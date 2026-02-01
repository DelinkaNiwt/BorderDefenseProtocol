using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System;
using Verse;
using Verse.Sound;
using UnityEngine;
using RimWorld;
using Verse.AI.Group;
using RimWorld.QuestGen;

namespace GD3
{
    public class QuestPart_StartBattle : QuestPart
    {
        public string inSignal;

        public Pawn enemy1;

        public Pawn enemy2;

        public Site site;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                Map map = site.Map;
                IntVec3 pos = map.Center;
                List<IntVec3> vecs = map.AllCells.ToList().FindAll(c => c.DistanceTo(pos) <= 23.9f && c.DistanceTo(pos) >= 10.9f);
                
                IntVec3 vec = vecs.RandomElement();
                FleckMaker.ThrowAirPuffUp(vec.ToVector3(), map);
                FleckMaker.ThrowDustPuffThick(vec.ToVector3(), map, 1.6f, Color.white);
                GenPlace.TryPlaceThing(enemy1, vec, map, ThingPlaceMode.Near);
                if (!Find.World.GetComponent<MissionComponent>().militorSpawned)
                {
                    MoteMaker.ThrowText(vec.ToVector3(), map, "GD.TesseronSay".Translate(), 12f);
                }

                vec = vecs.RandomElement();
                FleckMaker.ThrowAirPuffUp(vec.ToVector3(), map);
                FleckMaker.ThrowDustPuffThick(vec.ToVector3(), map, 1.6f, Color.white);
                GenPlace.TryPlaceThing(enemy2, vec, map, ThingPlaceMode.Near);

                Lord lord = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_AssaultColony(Faction.OfMechanoids, false, false, false, false, false), map);
                lord.AddPawn(enemy1);
                lord.AddPawn(enemy2);

                Find.LetterStack.ReceiveLetter("GD.BattleStart".Translate(), "GD.BattleStartDesc".Translate(), LetterDefOf.ThreatBig, null, 0, true);
                Find.MusicManagerPlay.ForcePlaySong(GDDefOf.Xenanis, false);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref enemy1, "enemy1");
            Scribe_Deep.Look(ref enemy2, "enemy2");
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref site, "site");
        }
    }
}