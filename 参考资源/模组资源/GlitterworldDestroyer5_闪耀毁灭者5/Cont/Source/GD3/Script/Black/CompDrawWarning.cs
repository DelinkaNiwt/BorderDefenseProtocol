using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace GD3
{
    public class CompDrawWarning : ThingComp
    {
        public override void PostDraw()
        {
            base.PostDraw();
            Thing thing = this.parent;
            IntVec3 vec = this.parent.Position;
            List<Pawn> pawns = thing.Map.mapPawns.AllPawns.FindAll(p => p.Faction != null && p.Faction == Faction.OfPlayer && p.Position.DistanceTo(vec) <= 4.9f);
            if (pawns.Count == 0)
            {
                return;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                Vector3 drawPos = pawn.DrawPos;
                drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                drawPos += new Vector3(0, 0, 0.85f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(1.2f, 1.0f, 1.2f));
                Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("UI/Symbols/WarningIcon", ShaderDatabase.MoteGlow, new Color(1,1,1,0.5f)), 0);
            }
        }
    }
}
