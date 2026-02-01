using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Verse.Sound;

namespace GD3
{
    public class EliminateDummy : ThingWithComps
    {
        public List<Pawn> Pawns
        {
            get
            {
                List<Pawn> pawns = Map.mapPawns.AllPawns.FindAll(p => p.Faction != null && p.Faction == Faction.OfPlayer);
                pawns.SortBy(p => p.Position.DistanceTo(Position));
                return pawns;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            IntVec3 vec = this.Position;
            List<IntVec3> vecs = this.Map.AllCells.ToList().FindAll(c => c.DistanceTo(vec) <= 49);
            for (int i = 0; i < vecs.Count; i++)
            {
                IntVec3 v = vecs[i];
                Map.roofGrid.SetRoof(v, null);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (!told)
            {
                Find.MusicManagerPlay.ForceFadeoutAndSilenceFor(1f);

                Pawn militor = Position.GetFirstPawn(Map);
                Corpse corpse = Position.GetFirstThing<Corpse>(Map);
                if (militor != null)
                {
                    NPC = militor;
                    MakeTurn(militor);
                }
                else if (corpse != null)
                {
                    NPC = corpse;
                }
                else
                {
                    NPC = null;
                }

                if (Pawns.Count > 0 && Pawns[0].Position.DistanceTo(Position) <= 10.9f)
                {
                    told = true;
                    Find.World.GetComponent<MissionComponent>().militorSpawned = true;
                    if (NPC != null && NPC is Pawn)
                    {
                        IntVec3 direction = Pawns[0].Position - NPC.Position;
                        if (direction != IntVec3.Zero)
                        {
                            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                            NPC.Rotation = Rot4.FromAngleFlat(angle);
                        }
                        Messages.Message("GD.FoundMilitor".Translate(), MessageTypeDefOf.PositiveEvent);
                    }
                    else if (NPC != null && NPC is Corpse)
                    {
                        Messages.Message("GD.FoundMilitorCorpse".Translate(), MessageTypeDefOf.PawnDeath);
                    }
                    else
                    {
                        Messages.Message("GD.ReadyToBattle".Translate(), MessageTypeDefOf.NegativeEvent);
                    }
                }
            }
            else
            {
                toldTick++;

                if (toldTick <= 639)
                {
                    Find.MusicManagerPlay.Stop();
                }

                if (NPC != null && NPC is Pawn && toldTick == 180 || NPC != null && NPC is Corpse && toldTick == 700)
                {
                    SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(NPC.Position, NPC.Map, false));

                    Effecter effecter = EffecterDefOf.Skip_Entry.Spawn(NPC, NPC.Map, 1f);
                    effecter.Trigger(NPC, NPC, -1);
                    effecter.Cleanup();
                    effecter = EffecterDefOf.Skip_Entry.Spawn(NPC, NPC.Map, 1f);
                    effecter.Trigger(NPC, NPC, -1);
                    effecter.Cleanup();

                    NPC.Destroy(DestroyMode.Vanish);
                }
                if (toldTick == 360)
                {
                    GDDefOf.GD_Morse_CLEAR.PlayOneShotOnCamera();
                }

                if (toldTick == 640)
                {
                    GDUtility.SendSignal(GDUtility.GetQuestOfThing(this), "BattleStarted");
                }
            }
        }

        private void MakeTurn(Pawn pawn)
        {
            if (GDSettings.DeveloperMode)
            {
                Log.Warning("TryingTurn");
            }
            turnTicker++;
            if (turnTicker >= 200)
            {
                turnTicker = 0;
                if (pawn.Rotation == Rot4.North)
                {
                    pawn.Rotation = Rot4.West;
                }
                else if (pawn.Rotation == Rot4.West)
                {
                    pawn.Rotation = Rot4.South;
                }
                else if (pawn.Rotation == Rot4.South)
                {
                    pawn.Rotation = Rot4.East;
                }
                else
                {
                    pawn.Rotation = Rot4.North;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref NPC, "NPC");
            Scribe_Values.Look(ref told, "told");
        }

        public bool told = false;

        private int turnTicker;

        private int toldTick;

        private Thing NPC;
    }
}
