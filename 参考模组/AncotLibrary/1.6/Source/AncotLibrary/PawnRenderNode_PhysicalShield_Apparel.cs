using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnRenderNode_PhysicalShield_Apparel : PawnRenderNode_Apparel
{
	public CompPhysicalShield shieldComp;

	public PawnRenderNode_PhysicalShield_Apparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree, null)
	{
		apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	public PawnRenderNode_PhysicalShield_Apparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
		: base(pawn, props, tree, apparel)
	{
		base.apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	public override GraphicMeshSet MeshSetFor(Pawn pawn)
	{
		if (props.overrideMeshSize.HasValue)
		{
			return MeshPool.GetMeshSetForSize(props.overrideMeshSize.Value.x, props.overrideMeshSize.Value.y);
		}
		return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn);
	}

	protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
	{
		if (shieldComp == null)
		{
			shieldComp = apparel.TryGetComp<CompPhysicalShield>();
		}
		yield return GraphicDatabase.Get<Graphic_Multi>(shieldComp.ShieldState switch
		{
			An_ShieldState.Active => shieldComp.graphicPath_Holding, 
			An_ShieldState.Ready => shieldComp.graphicPath_Ready, 
			An_ShieldState.Resetting => shieldComp.graphicPath_Ready, 
			_ => shieldComp.graphicPath_Disabled, 
		}, ShaderDatabase.Cutout, Vector2.one, ColorFor(pawn));
	}

	public override Color ColorFor(Pawn pawn)
	{
		Color result = Color.white;
		if (apparel.def.MadeFromStuff)
		{
			result = apparel.Stuff.stuffProps.color;
		}
		return result;
	}
}
