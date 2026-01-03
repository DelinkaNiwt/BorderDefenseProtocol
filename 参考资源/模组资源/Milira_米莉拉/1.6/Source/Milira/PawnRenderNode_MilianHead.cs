using System.Linq;
using UnityEngine;
using Verse;

namespace Milira;

public class PawnRenderNode_MilianHead : PawnRenderNode
{
	public PawnRenderNode_MilianHead(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override GraphicMeshSet MeshSetFor(Pawn pawn)
	{
		if (props.overrideMeshSize.HasValue)
		{
			return MeshPool.GetMeshSetForSize(props.overrideMeshSize.Value.x, props.overrideMeshSize.Value.y);
		}
		return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn);
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		BodyPartRecord bodyPartRecord = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def.defName == "Milian_Head");
		if (bodyPartRecord == null)
		{
			return null;
		}
		if (MilianUtility.IsMilian_RookClass(pawn))
		{
			return GraphicDatabase.Get<Graphic_Multi>("Milian/Pawn/Head/MilianHeadN2", ShaderDatabase.Cutout, Vector2.one, Color.white);
		}
		return GraphicDatabase.Get<Graphic_Multi>("Milian/Pawn/Head/MilianHeadN1", ShaderDatabase.Cutout, Vector2.one, Color.white);
	}
}
