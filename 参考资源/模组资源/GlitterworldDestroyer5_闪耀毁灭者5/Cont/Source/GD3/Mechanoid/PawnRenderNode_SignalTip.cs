using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
    public class PawnRenderNode_SignalTip : PawnRenderNode
    {
        public PawnRenderNode_SignalTip(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
        {
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            Graphic graphic = pawn.Drawer.renderer.SilhouetteGraphic;
            if (graphic != null)
            {
                return MeshPool.GetMeshSetForSize(graphic.drawSize.x * props.overrideMeshSize.Value.x, graphic.drawSize.y * props.overrideMeshSize.Value.y);
            }
            return null;
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            Graphic graphic = pawn.Drawer.renderer.SilhouetteGraphic;
            if (graphic != null)
            {
                return graphic.GetColoredVersion(ShaderDatabase.Silhouette, ColorFor(pawn), ColorFor(pawn));
            }
            return null;
        }

        public override Color ColorFor(Pawn pawn)
        {
            return Color.red;
        }
    }
}
