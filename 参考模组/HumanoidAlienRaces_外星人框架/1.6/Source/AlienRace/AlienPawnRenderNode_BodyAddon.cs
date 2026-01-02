using UnityEngine;
using Verse;

namespace AlienRace;

public class AlienPawnRenderNode_BodyAddon : PawnRenderNode
{
	public new AlienPawnRenderNodeProperties_BodyAddon props;

	private readonly Pawn owningPawn;

	public AlienPawnRenderNode_BodyAddon(Pawn pawn, AlienPawnRenderNodeProperties_BodyAddon props, PawnRenderTree tree)
	{
		this.props = props;
		owningPawn = pawn;
		base._002Ector(pawn, props, tree);
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		return props.graphic;
	}

	public void UpdateGraphic()
	{
		primaryGraphic = GraphicFor(owningPawn);
	}

	public override GraphicMeshSet MeshSetFor(Pawn pawn)
	{
		return MeshPool.GetMeshSetForSize(Vector2.one);
	}

	public override Mesh GetMesh(PawnDrawParms parms)
	{
		if (parms.flipHead && props.addon.alignWithHead)
		{
			parms.facing = parms.facing.Opposite;
		}
		return props.graphic.MeshAt(parms.facing);
	}
}
