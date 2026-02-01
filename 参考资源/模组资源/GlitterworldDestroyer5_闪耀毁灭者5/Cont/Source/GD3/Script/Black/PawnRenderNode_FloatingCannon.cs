using RimWorld;
using Verse;

namespace GD3
{
    public class PawnRenderNode_FloatingCannon : PawnRenderNode
    {
        public CompFloatingCannon turretComp;

        public PawnRenderNode_FloatingCannon(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            return GraphicDatabase.Get<Graphic_Single>(turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.Cutout);
        }
    }
}