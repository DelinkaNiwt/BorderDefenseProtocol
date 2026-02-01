using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Graphics;

public abstract class Graphic_FleckCollection : Graphic_Fleck
{
	protected Graphic_Fleck[] subGraphics;

	public override void Init(GraphicRequest req)
	{
		data = req.graphicData;
		if (req.path.NullOrEmpty())
		{
			throw new ArgumentNullException("folderPath");
		}
		if (req.shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		path = req.path;
		maskPath = req.maskPath;
		color = req.color;
		colorTwo = req.colorTwo;
		drawSize = req.drawSize;
		List<Texture2D> list = (from x in ContentFinder<Texture2D>.GetAllInFolder(req.path)
			where !x.name.EndsWith(Graphic_Single.MaskSuffix)
			orderby x.name
			select x).ToList();
		if (list.NullOrEmpty())
		{
			Log.Error("Collection cannot init: No textures found at path " + req.path);
			subGraphics = new Graphic_Fleck[0];
		}
		else
		{
			subGraphics = list.Select((Texture2D texture2D) => (Graphic_Fleck)GraphicDatabase.Get(typeof(Graphic_Fleck), req.path + "/" + texture2D.name, req.shader, drawSize, color, colorTwo, data, req.shaderParameters)).ToArray();
		}
	}
}
