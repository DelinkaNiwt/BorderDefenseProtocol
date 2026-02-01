using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompTransformable : ThingComp
{
	private Gizmo transformGizmo;

	public CompProperties_Transformable Props => (CompProperties_Transformable)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		Texture2D gizmoIcon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath, reportFailure: false);
		transformGizmo = new Command_Action
		{
			icon = gizmoIcon,
			defaultLabel = Props.gizmoLabel,
			defaultDesc = Props.gizmoDescription,
			action = TransformBuilding
		};
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (parent.Faction == Faction.OfPlayer)
		{
			yield return transformGizmo;
		}
	}

	private void TransformBuilding()
	{
		if (Props.targetBuildingDef == null)
		{
			Log.Error("Target building def is not set in CompProperties_Transformable");
			return;
		}
		IntVec3 position = parent.Position;
		Map map = parent.Map;
		Rot4 rotation = parent.Rotation;
		Faction faction = parent.Faction;
		parent.Destroy();
		Thing newBuilding = ThingMaker.MakeThing(Props.targetBuildingDef);
		newBuilding.SetFactionDirect(faction);
		GenSpawn.Spawn(newBuilding, position, map, rotation);
		if (Props.transformationEffect != null)
		{
			Props.transformationEffect.Spawn(position, map);
		}
	}
}
