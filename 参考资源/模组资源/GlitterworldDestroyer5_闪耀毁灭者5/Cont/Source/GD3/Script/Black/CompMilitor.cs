using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse.Sound;
using Verse.AI.Group;
using Verse.AI;

namespace GD3
{
    public class CompMilitor : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();
            if (!active)
            {
                return;
            }
            ticker++;
            if (GDSettings.DeveloperMode)
            {
                int num = ticker / 60;
                if (ticker != 0 && ticker % 600 == 0)
                {
                    Log.Warning("Seconds:" + num);
                }
            }
            Pawn pawn = (Pawn)this.parent;
            if (pawn.Downed && !pawn.Dead)
            {
                HealthUtility.DamageUntilDead(pawn);
            }

            if (ticker == 6000 || ticker == 10000)
            {
                QuestUtility.SendQuestTargetSignals(pawn.questTags, "raidLight", pawn.Named("SUBJECT"));
            }

            if (ticker == 16000 || ticker == 22000)
            {
                QuestUtility.SendQuestTargetSignals(pawn.questTags, "raidHeavy", pawn.Named("SUBJECT"));
            }

            if (ticker == 28000)
            {
                QuestUtility.SendQuestTargetSignals(pawn.questTags, "raidUltraHeavy", pawn.Named("SUBJECT"));
            }

            if (ticker == 29200)
            {
                QuestUtility.SendQuestTargetSignals(pawn.questTags, "helpArrive", pawn.Named("SUBJECT"));
                
            }
            if (ticker == 32000)
            {
                pawn.TryGetLord(out Lord lord);
                lord.RemovePawn(pawn);
                Lord lord2 = LordMaker.MakeNewLord(pawn.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog, false, true), pawn.Map);
                lord2.AddPawn(pawn);
                Messages.Message("GD.MilitorExiting".Translate(), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn pawn = (Pawn)this.parent;
            if (!active || !pawn.Spawned || pawn.Dead)
            {
                return;
            }
            Thing thing = this.parent;
            Vector3 drawPos = thing.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            drawPos += new Vector3(0, 0, 0.4f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(1f, 1f, 1f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("UI/Symbols/AllyArrow", ShaderDatabase.Transparent), 0);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticker, "ticker", 0, false);
            Scribe_Values.Look<bool>(ref this.active, "active", false, false);
        }

        public int ticker = 0;

        public bool active = false;
    }
}
