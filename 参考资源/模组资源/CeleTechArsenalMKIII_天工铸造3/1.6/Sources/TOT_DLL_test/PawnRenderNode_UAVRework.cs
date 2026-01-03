using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PawnRenderNode_UAVRework : PawnRenderNode
{
	public Comp_FloatingGunRework TurretComp;

	public Comp_FloatingGunRework turretComp
	{
		get
		{
			if (TurretComp == null)
			{
				TurretComp = apparel.TryGetComp<Comp_FloatingGunRework>();
			}
			return TurretComp;
		}
	}

	public PawnRenderNode_UAVRework(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		if (base.Props.texPath != null)
		{
			Shader shader = props.shaderTypeDef.Shader;
			PawnRenderNodeProperties_UAVRework pawnRenderNodeProperties_UAVRework = props as PawnRenderNodeProperties_UAVRework;
			if (pawnRenderNodeProperties_UAVRework.useforcedColor)
			{
				return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, shader, base.Props.overrideMeshSize.Value, ColorFor(pawn));
			}
			return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, shader);
		}
		return GraphicDatabase.Get<Graphic_Single>(turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.CutoutComplex);
	}

	public override Color ColorFor(Pawn pawn)
	{
		return turretComp.parent.DrawColor;
	}
}
