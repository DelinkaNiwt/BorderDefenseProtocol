using RimWorld;
using RimWorld.SketchGen;
using Verse;

namespace AncotLibrary;

public class SketchResolver_Prefab : SketchResolver
{
	public PrefabDef prefab;

	protected override bool CanResolveInt(SketchResolveParams parms)
	{
		return parms.sketch != null;
	}

	protected override void ResolveInt(SketchResolveParams parms)
	{
		Sketch sketch = new Sketch();
		sketch.GravShip_AddPrefab(prefab, new IntVec3(0, 0, 0), Rot4.North);
		parms.sketch.Merge(sketch);
	}
}
