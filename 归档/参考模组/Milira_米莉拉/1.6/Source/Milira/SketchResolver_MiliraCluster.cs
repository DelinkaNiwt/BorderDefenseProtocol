using RimWorld.SketchGen;

namespace Milira;

public class SketchResolver_MiliraCluster : SketchResolver
{
	protected override void ResolveInt(SketchResolveParams parms)
	{
		MiliraClusterGenerator.ResolveSketch(parms);
	}

	protected override bool CanResolveInt(SketchResolveParams parms)
	{
		return true;
	}
}
