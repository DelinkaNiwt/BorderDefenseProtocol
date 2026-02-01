using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GD3
{
    public class CompDrawBattery : ThingComp
    {
        public bool CanDraw
        {
            get
            {
                Pawn pawn = (Pawn)this.parent;
                if (pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.Faction != null)
                {
                    if (pawn.Faction.IsPlayer && pawn.Drafted)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (!CanDraw)
            {
                return;
            }
            Pawn thing = (Pawn)this.parent;
            Vector3 drawPos = thing.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            drawPos += new Vector3(0, 0, 2.8f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(1.5f, 1.0f, 1.5f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom(GetTexturePath(), ShaderDatabase.Transparent), 0);
        }

        private string GetTexturePath()
        {
            Pawn pawn = (Pawn)this.parent;
            if (pawn.needs.energy.CurLevelPercentage >= 0.66f)
            {
                return "UI/Symbols/CentipedeBattery_A";
            }
            else if (pawn.needs.energy.CurLevelPercentage >= 0.33f)
            {
                return "UI/Symbols/CentipedeBattery_B";
            }
            return "UI/Symbols/CentipedeBattery_C";
        }
    }
}
