using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class TextureTool
{
	public static void SetDirty(this Pawn p)
	{
		p?.Drawer?.renderer.SetAllGraphicsDirty();
	}

	internal static Texture2D GetTextureFromStackCount(this ThingDef t)
	{
		Texture2D result = null;
		if (t != null && t.graphic != null && t.graphic.GetType() == typeof(Graphic_StackCount))
		{
			Graphic_StackCount graphic_StackCount = t.graphic as Graphic_StackCount;
			Graphic graphic = graphic_StackCount.SubGraphicForStackCount(100, t);
			result = ContentFinder<Texture2D>.Get(graphic.path, reportFailure: false);
		}
		return result;
	}

	internal static Texture2D GetTextureFromMulti(this Graphic g, string rotate = "_south")
	{
		Graphic_Multi graphic_Multi = g as Graphic_Multi;
		return ContentFinder<Texture2D>.Get(graphic_Multi.path + rotate, reportFailure: false);
	}

	internal static string Rot4ToString(Rot4 rot4)
	{
		if (rot4 == Rot4.North)
		{
			return "_north";
		}
		if (rot4 == Rot4.South)
		{
			return "_south";
		}
		if (rot4 == Rot4.East)
		{
			return "_east";
		}
		if (rot4 == Rot4.West)
		{
			return "_west";
		}
		return "_north";
	}

	internal static Texture2D GetTexture(this ThingDef t, int stackCount = 1, ThingStyleDef tsd = null, Rot4 rotation = default(Rot4))
	{
		Texture2D texture2D = null;
		if (texture2D == null && t != null)
		{
			GraphicData graphicData = ((tsd != null) ? tsd.graphicData : t.graphicData);
			graphicData = graphicData ?? t.graphicData;
			if (graphicData != null)
			{
				texture2D = ((graphicData.graphicClass == typeof(Graphic_Multi)) ? GetGraphicForMulti(graphicData, rotation) : ((graphicData.graphicClass == typeof(Graphic_Random)) ? GetGraphicForRandom(graphicData) : ((graphicData.graphicClass == typeof(Graphic_StackCount)) ? GetGraphicForStackCount(graphicData, t, stackCount) : ((!(graphicData.graphicClass == typeof(Graphic_Single))) ? GetGraphicForUndefined(graphicData) : GetGraphicForSingle(graphicData)))));
			}
		}
		if (texture2D == null && t != null && t.uiIcon != null)
		{
			return t.uiIcon;
		}
		return texture2D;
	}

	internal static Texture2D GetGraphicForUndefined(GraphicData g)
	{
		return (g != null) ? ContentFinder<Texture2D>.Get(g.texPath, reportFailure: false) : null;
	}

	internal static Texture2D GetGraphicForMulti(GraphicData g, Rot4 rotation)
	{
		string text = Rot4ToString(rotation);
		Texture2D texture2D = ContentFinder<Texture2D>.Get((g.Graphic as Graphic_Multi).path + text, reportFailure: false);
		if (texture2D == null)
		{
			if (rotation == Rot4.West)
			{
				texture2D = ContentFinder<Texture2D>.Get((g.Graphic as Graphic_Multi).path + "_east", reportFailure: false);
			}
			if (texture2D == null)
			{
				texture2D = ContentFinder<Texture2D>.Get((g.Graphic as Graphic_Multi).path + "_north", reportFailure: false);
			}
		}
		return texture2D;
	}

	internal static Texture2D GetGraphicForRandom(GraphicData g)
	{
		return ContentFinder<Texture2D>.Get((g.Graphic as Graphic_Random).FirstSubgraphic().path, reportFailure: false);
	}

	internal static Texture2D GetGraphicForSingle(GraphicData g)
	{
		if (g.Graphic == null)
		{
			return ContentFinder<Texture2D>.Get(g.texPath, reportFailure: false);
		}
		try
		{
			return ContentFinder<Texture2D>.Get((g.Graphic as Graphic_Single).path, reportFailure: false);
		}
		catch
		{
			return ContentFinder<Texture2D>.Get(g.texPath, reportFailure: false);
		}
	}

	internal static Texture2D GetGraphicForStackCount(GraphicData g, ThingDef t, int stackVal)
	{
		try
		{
			return ContentFinder<Texture2D>.Get((g.Graphic as Graphic_StackCount).SubGraphicForStackCount(stackVal, t).path, reportFailure: false);
		}
		catch
		{
			return ContentFinder<Texture2D>.Get(g.Graphic.path);
		}
	}

	internal static void SetGraphicDataSingle(this ThingDef td, string texPath, string uiTexPath)
	{
		if (td != null)
		{
			td.graphicData = new GraphicData();
			td.graphicData.texPath = texPath;
			td.graphicData.shaderType = ShaderTypeDefOf.MetaOverlay;
			td.graphicData.graphicClass = typeof(Graphic_Single);
			td.uiIconPath = uiTexPath;
		}
	}

	internal static bool TestTexturePath(string path, bool showError = true)
	{
		Texture2D texture2D = ContentFinder<Texture2D>.Get(path, reportFailure: false);
		if (texture2D == null && showError)
		{
			MessageTool.Show("Missing Texture=" + path, MessageTypeDefOf.RejectInput);
		}
		return texture2D != null;
	}
}
