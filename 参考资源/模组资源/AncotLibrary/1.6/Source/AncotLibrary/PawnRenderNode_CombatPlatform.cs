using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class PawnRenderNode_CombatPlatform : PawnRenderNode_Apparel
{
	public CompCombatPlatform compCombatPlatform;

	public PawnRenderNode_CombatPlatform(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree, null)
	{
		apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	public PawnRenderNode_CombatPlatform(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
		: base(pawn, props, tree, apparel)
	{
		base.apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
	{
		PawnRenderNodeProperties pawnRenderNodeProperties = base.Props;
		if (pawnRenderNodeProperties is PawnRenderNodeProperties_CombatPlatform combatPlatformProps && compCombatPlatform == null)
		{
			compCombatPlatform = (combatPlatformProps.isApparel ? apparel.TryGetComp<CompCombatPlatform>() : pawn.TryGetComp<CompCombatPlatform>());
		}
		yield return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, ShaderDatabase.Cutout);
	}
}
