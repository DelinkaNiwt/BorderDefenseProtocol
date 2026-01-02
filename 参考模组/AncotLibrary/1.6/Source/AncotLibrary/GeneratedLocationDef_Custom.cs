using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class GeneratedLocationDef_Custom : Def
{
	private PlanetLayerDef layerDef = null;

	public int layerMaximum = -1;

	public WorldObjectDef worldObjectDef;

	public List<ThingDef> preciousResources;

	public FloatRange TimeoutRangeDays = new FloatRange(20f, 40f);

	public IntRange objectAmount = IntRange.Zero;

	private static PlanetLayerDef Surface;

	public PlanetLayerDef LayerDef
	{
		get
		{
			PlanetLayerDef surface;
			if ((surface = layerDef) == null && (surface = Surface) == null)
			{
				surface = PlanetLayerDefOf.Surface;
				Surface = PlanetLayerDefOf.Surface;
			}
			return surface;
		}
	}
}
