using RimWorld.SketchGen;

namespace GD3
{
    public class SketchResolver_MechCluster_Giant : SketchResolver
    {
		protected override void ResolveInt(SketchResolveParams parms)
		{
			MechClusterGenerator_Giant.ResolveSketch(parms);
		}

		protected override bool CanResolveInt(SketchResolveParams parms)
		{
			return true;
		}
	}
}
