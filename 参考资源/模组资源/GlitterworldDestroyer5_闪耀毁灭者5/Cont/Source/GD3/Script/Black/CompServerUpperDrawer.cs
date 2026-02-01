using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GD3
{
    public class CompServerUpperDrawer : ThingComp
    {
        public CompProperties_ServerUpperDrawer Props => (CompProperties_ServerUpperDrawer)props;

        public override void PostDraw()
        {
            base.PostDraw();
            Thing thing = this.parent;
            Vector3 drawPos = thing.DrawPos;
            drawPos.y = AltitudeLayer.Blueprint.AltitudeFor();
            drawPos += Props.drawOffset;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(2.5f, 1.0f, 2.0f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom(thing.def.graphicData.texPath + "_upper", ShaderDatabase.Transparent), 0);
        }
    }
}
