using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public class CompContinuousExhaust : ThingComp
{
	private static readonly Material exhaustMaterial = MaterialPool.MatFrom("Things/Mote/Smoke", ShaderDatabase.MoteGlow, new Color(1f, 0.7f, 0.2f));

	private int fleckCounter;

	public CompProperties_ContinuousExhaust Props => (CompProperties_ContinuousExhaust)props;

	public override void CompTick()
	{
		base.CompTick();
		if (parent != null && !parent.Destroyed && parent.Spawned)
		{
			fleckCounter++;
			if (fleckCounter >= Props.fleckInterval)
			{
				fleckCounter = 0;
				EmitExhaustFleck();
			}
		}
	}

	private void EmitExhaustFleck()
	{
		if (parent.Map == null || Props.fleckTypes.Count == 0)
		{
			return;
		}
		Vector3 position = parent.DrawPos;
		if (Props.offsetDirection != Vector3.zero)
		{
			position += Props.offsetDirection * Props.offsetDistance;
		}
		foreach (FleckDef fleckDef in Props.fleckTypes)
		{
			float altitude = ((fleckDef == FleckDefOf.Smoke) ? AltitudeLayer.FogOfWar.AltitudeFor() : AltitudeLayer.MoteOverhead.AltitudeFor());
			Vector3 fleckPos = new Vector3(position.x, altitude, position.z);
			float scale = ((fleckDef == FleckDefOf.Smoke) ? Props.smokeScale.RandomInRange : Props.fleckScale.RandomInRange);
			float speed = ((fleckDef == FleckDefOf.Smoke) ? Props.smokeSpeedRange.RandomInRange : Props.fleckSpeedRange.RandomInRange);
			float angle = ((fleckDef == FleckDefOf.Smoke) ? Props.smokeAngleRange.RandomInRange : Props.fleckAngleRange.RandomInRange);
			FleckCreationData data = FleckMaker.GetDataStatic(fleckPos, parent.Map, fleckDef, scale);
			data.velocityAngle = angle;
			data.velocitySpeed = speed;
			data.rotation = Props.fleckRotationRange.RandomInRange;
			data.rotationRate = Props.fleckRotationRate.RandomInRange;
			parent.Map.flecks.CreateFleck(data);
		}
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (parent != null && !parent.Destroyed && parent.Spawned)
		{
			Vector3 drawPos = parent.DrawPos;
			drawPos.y = AltitudeLayer.FogOfWar.AltitudeFor();
			Vector3 offset = Props.offsetDirection * Props.offsetDistance;
			drawPos += offset;
			Quaternion rotation = Quaternion.identity;
			if (Props.offsetDirection != Vector3.zero)
			{
				rotation = Quaternion.LookRotation(Props.offsetDirection);
			}
			else if (parent is Pawn pawn && pawn.pather.Moving)
			{
				rotation = Quaternion.LookRotation(-pawn.pather.nextCell.ToVector3());
			}
			Graphics.DrawMesh(MeshPool.plane10, drawPos, rotation, exhaustMaterial, 0);
		}
	}
}
