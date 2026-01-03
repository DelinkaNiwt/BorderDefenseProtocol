using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompAdditionalGraphic : ThingComp
{
	public float floatOffset = 0f;

	private float randTime = Rand.Range(0f, 600f);

	public bool drawGraphic = false;

	private CompProperties_AdditionalGraphic Props => (CompProperties_AdditionalGraphic)props;

	public bool drawAdditionalGraphic
	{
		get
		{
			if (Props.drawAdditionalGraphicDefault)
			{
				return true;
			}
			return false;
		}
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (drawAdditionalGraphic || drawGraphic)
		{
			Mesh mesh = Props.graphicData.Graphic.MeshAt(parent.Rotation);
			Vector3 drawPos = parent.DrawPos;
			floatOffset = Mathf.Sin(((float)Find.TickManager.TicksGame + randTime) * Props.floatSpeed) * Props.floatAmplitude;
			drawPos.z = parent.DrawPos.z + floatOffset;
			drawPos.y = Props.altitudeLayer.AltitudeFor();
			Graphics.DrawMesh(mesh, drawPos + Props.graphicData.drawOffset.RotatedBy(parent.Rotation), Quaternion.identity, Props.graphicData.Graphic.MatAt(parent.Rotation), 0);
		}
	}
}
