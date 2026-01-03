using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnRenderNode_ApparelAddition : PawnRenderNode_Apparel
{
	public PawnRenderNode_ApparelAddition(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree, null)
	{
		apparel = apparel;
		useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
		meshSet = MeshSetFor(pawn);
	}

	public PawnRenderNode_ApparelAddition(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh)
		: base(pawn, props, tree, apparel)
	{
		base.apparel = apparel;
		base.useHeadMesh = useHeadMesh;
		meshSet = MeshSetFor(pawn);
	}

	protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
	{
		if (TryGetGraphicApparel(props.texPath, apparel, tree.pawn.story.bodyType, pawn.Drawer.renderer.StatueColor.HasValue, out var rec))
		{
			yield return rec.graphic;
		}
	}

	public static bool TryGetGraphicApparel(string texPath, Apparel apparel, BodyTypeDef bodyType, bool forStatue, out ApparelGraphicRecord rec)
	{
		if (bodyType == null)
		{
			Log.Error("Getting apparel graphic with undefined body type.");
			bodyType = BodyTypeDefOf.Male;
		}
		string path = ((apparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead && apparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover && !apparel.RenderAsPack() && !(texPath == BaseContent.PlaceholderImagePath) && !(texPath == BaseContent.PlaceholderGearImagePath)) ? (texPath + "_" + bodyType.defName) : texPath);
		Shader shader = ShaderDatabase.Cutout;
		if (!forStatue)
		{
			if (apparel.StyleDef?.graphicData.shaderType != null)
			{
				shader = apparel.StyleDef.graphicData.shaderType.Shader;
			}
			else if ((apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask))
			{
				shader = ShaderDatabase.CutoutComplex;
			}
		}
		Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
		rec = new ApparelGraphicRecord(graphic, apparel);
		return true;
	}
}
