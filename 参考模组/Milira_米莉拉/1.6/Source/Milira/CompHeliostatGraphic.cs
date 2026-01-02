using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class CompHeliostatGraphic : ThingComp
{
	private float angle = 0f;

	private CompProperties_HeliostatGraphic Props => (CompProperties_HeliostatGraphic)props;

	private CompFacility compFacility => parent.TryGetComp<CompFacility>();

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref angle, "angle", 0f);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		FindLinkedTowerAngle();
	}

	public override void PostDraw()
	{
		base.PostDraw();
		Mesh mesh = Props.graphicData.Graphic.MeshAt(parent.Rotation);
		Vector3 drawPos = parent.DrawPos;
		drawPos.y = Props.altitudeLayer.AltitudeFor();
		Graphics.DrawMesh(mesh, drawPos + Props.graphicData.drawOffset.RotatedBy(parent.Rotation), Quaternion.AngleAxis(angle, Vector3.up), Props.graphicData.Graphic.MatAt(parent.Rotation), 0);
	}

	public override void CompTickLong()
	{
		base.CompTickLong();
		FindLinkedTowerAngle();
	}

	public void FindLinkedTowerAngle()
	{
		Thing thing = compFacility.LinkedBuildings.FirstOrDefault();
		if (thing != null)
		{
			Vector3 vector = parent.Position.ToVector3Shifted();
			Vector3 vector2 = thing.Position.ToVector3Shifted();
			Vector3 v = vector2 - vector;
			angle = v.AngleFlat();
		}
	}
}
