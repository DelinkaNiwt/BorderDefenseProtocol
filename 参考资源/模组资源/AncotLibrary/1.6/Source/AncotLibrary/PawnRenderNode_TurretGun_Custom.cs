using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class PawnRenderNode_TurretGun_Custom : PawnRenderNode_Apparel
{
	public CompTurretGun_Custom turretComp;

	public PawnRenderNode_TurretGun_Custom(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree, null)
	{
		apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	public PawnRenderNode_TurretGun_Custom(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
		: base(pawn, props, tree, apparel)
	{
		base.apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
	{
		PawnRenderNodeProperties pawnRenderNodeProperties = base.Props;
		if (pawnRenderNodeProperties is PawnRenderNodeProperties_TurretGun_Custom turretGunProps && turretComp == null)
		{
			turretComp = (turretGunProps.isApparel ? apparel.TryGetComp<CompTurretGun_Custom>() : pawn.TryGetComp<CompTurretGun_Custom>());
		}
		if (base.Props.texPath != null)
		{
			yield return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, ShaderDatabase.Cutout);
		}
		else
		{
			yield return GraphicDatabase.Get<Graphic_Single>(turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.Cutout);
		}
	}
}
