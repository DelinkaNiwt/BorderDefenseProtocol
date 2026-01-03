using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class PawnRenderNode_IntegrationWeaponSystem : PawnRenderNode_Apparel
{
	public CompIntegrationWeaponSystem SystemComp;

	public PawnRenderNode_IntegrationWeaponSystem(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree, null)
	{
		apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	public PawnRenderNode_IntegrationWeaponSystem(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
		: base(pawn, props, tree, apparel)
	{
		base.apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
	{
		if (SystemComp == null)
		{
			SystemComp = apparel.TryGetComp<CompIntegrationWeaponSystem>();
		}
		yield return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, ShaderDatabase.Cutout);
	}
}
