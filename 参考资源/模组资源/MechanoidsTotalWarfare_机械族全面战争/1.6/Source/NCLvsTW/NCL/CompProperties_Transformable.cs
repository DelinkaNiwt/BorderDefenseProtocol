using Verse;

namespace NCL;

public class CompProperties_Transformable : CompProperties
{
	public ThingDef targetBuildingDef;

	public string gizmoLabel = "Transform Building";

	public string gizmoDescription = "Transform this building into another form";

	public string gizmoIconPath;

	public EffecterDef transformationEffect;

	public CompProperties_Transformable()
	{
		compClass = typeof(CompTransformable);
	}
}
