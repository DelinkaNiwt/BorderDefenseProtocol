using RimWorld;
using UnityEngine;
using Verse;

namespace TowerLaserDefense;

public class PlaceWorker_TurretTop : PlaceWorker
{
	public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing thing = null)
	{
		CompProperties_LaserDefence compProperties = def.GetCompProperties<CompProperties_LaserDefence>();
		if (compProperties?.laserDefenceProperties?.graphicData != null)
		{
			GraphicData graphicData = compProperties.laserDefenceProperties.graphicData;
			Shader shader = graphicData.shaderType?.Shader ?? ShaderDatabase.Cutout;
			Graphic baseGraphic = GraphicDatabase.Get<Graphic_Single>(graphicData.texPath, shader, graphicData.drawSize, Color.white);
			Vector3 loc2 = GenThing.TrueCenter(loc, rot, def.Size, AltitudeLayer.MetaOverlays.AltitudeFor());
			GhostUtility.GhostGraphicFor(baseGraphic, def, ghostCol).DrawFromDef(loc2, rot, def, TurretTop.ArtworkRotation);
		}
	}
}
