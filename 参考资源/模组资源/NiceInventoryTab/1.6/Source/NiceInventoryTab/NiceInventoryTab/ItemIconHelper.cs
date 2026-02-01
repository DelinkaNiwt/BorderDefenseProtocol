using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class ItemIconHelper
{
	private static readonly MethodInfo ThingIconWorker = typeof(Widgets).GetMethod("ThingIconWorker", BindingFlags.Static | BindingFlags.NonPublic);

	public static void ThingIcon(Rect rect, Thing thing, float alpha = 1f, Rot4? rot = null, bool stackOfOne = false, float scale = 1f, bool grayscale = false)
	{
		thing = thing.GetInnerIfMinified();
		if (thing is Blueprint blueprint && blueprint.EntityToBuild() != null)
		{
			Widgets.DefIcon(rect, blueprint.EntityToBuild(), blueprint.EntityToBuildStuff(), 1f, blueprint.EntityToBuildStyle(), drawPlaceholder: false, null, null, null, alpha);
			return;
		}
		float scale2;
		float angle;
		Vector2 iconProportions;
		Color color;
		Material material;
		Texture iconFor = Widgets.GetIconFor(thing, new Vector2(rect.width, rect.height), rot, stackOfOne, out scale2, out angle, out iconProportions, out color, out material);
		if (thing is Frame { BuildDef: not null } frame)
		{
			iconFor = Widgets.GetIconFor(frame.BuildDef, frame.Stuff, frame.StyleDef);
		}
		if (iconFor == null || iconFor == BaseContent.BadTex)
		{
			return;
		}
		GUI.color = color;
		ThingStyleDef styleDef = thing.StyleDef;
		if ((styleDef != null && styleDef.UIIcon != null) || !thing.def.uiIconPath.NullOrEmpty())
		{
			rect.position += new Vector2(thing.def.uiIconOffset.x * rect.size.x, thing.def.uiIconOffset.y * rect.size.y);
		}
		Material material2 = material;
		if (grayscale)
		{
			MaterialRequest req = new MaterialRequest
			{
				shader = ShaderDatabase.GrayscaleGUI,
				color = color
			};
			if (material != null)
			{
				req.maskTex = (Texture2D)material.GetTexture(ShaderPropertyIDs.MaskTex);
				req.color = material.GetColor(ShaderPropertyIDs.Color);
				req.colorTwo = material.GetColor(ShaderPropertyIDs.ColorTwo);
			}
			else
			{
				req.maskTex = Texture2D.redTexture;
			}
			material2 = MaterialPool.MatFrom(req);
		}
		ThingIconWorker.Invoke(null, new object[8] { rect, thing.def, iconFor, angle, scale, rot, material2, alpha });
		GUI.color = Color.white;
	}
}
