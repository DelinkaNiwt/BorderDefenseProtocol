using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class CompFloatingSystem : ThingComp
{
	private float floatOffset = 0f;

	private float randTime = Rand.Range(0f, 600f);

	private CompProperties_FloatingSystem Props => (CompProperties_FloatingSystem)props;

	private bool drawGraphic
	{
		get
		{
			CompTurretGun comp = parent.GetComp<CompTurretGun>();
			if (parent is Pawn pawn)
			{
				if (!pawn.Spawned || pawn.Downed || pawn.Dead || !pawn.Awake())
				{
					return false;
				}
				if (comp != null && comp.TurretDestroyed)
				{
					return false;
				}
			}
			CompCanBeDormant compCanBeDormant = parent.TryGetComp<CompCanBeDormant>();
			if (compCanBeDormant != null && !compCanBeDormant.Awake)
			{
				return false;
			}
			return true;
		}
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (drawGraphic)
		{
			Mesh mesh = Props.graphicData.Graphic.MeshAt(parent.Rotation);
			Vector3 drawPos = parent.DrawPos;
			drawPos.z = parent.DrawPos.z + floatOffset;
			drawPos.y = Props.altitudeLayer.AltitudeFor() + Props.graphicData.drawOffset.y;
			switch (parent.Rotation.AsInt)
			{
			case 0:
				drawPos += Props.graphicData.drawOffsetNorth ?? Vector3.zero;
				break;
			case 1:
				drawPos += Props.graphicData.drawOffsetEast ?? Vector3.zero;
				break;
			case 2:
				drawPos += Props.graphicData.drawOffsetSouth ?? Vector3.zero;
				break;
			case 3:
				drawPos += Props.graphicData.drawOffsetWest ?? Vector3.zero;
				break;
			}
			Graphics.DrawMesh(mesh, drawPos + Props.graphicData.drawOffset, Quaternion.identity, Props.graphicData.Graphic.MatAt(parent.Rotation), 0);
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		floatOffset = Mathf.Sin(((float)Find.TickManager.TicksGame + randTime) * Props.floatSpeed) * Props.floatAmplitude;
	}
}
