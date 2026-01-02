using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Comp_FCradar : ThingComp
{
	[StaticConstructorOnStartup]
	public static class Resources
	{
		public static Material rotatorTexture = MaterialPool.MatFrom("Things/Buildings/CMC_FC_tex", ShaderDatabase.Cutout);
	}

	private float rotatorAngle = Rand.Range(0, 360);

	private CompPowerTrader compPowerTrader;

	public CompProperties_FCradar Properties => (CompProperties_FCradar)props;

	public bool get_Active()
	{
		if (compPowerTrader == null)
		{
			compPowerTrader = parent.GetComp<CompPowerTrader>();
		}
		return compPowerTrader != null && compPowerTrader.PowerOn;
	}

	public override void CompTick()
	{
		if (get_Active())
		{
			rotatorAngle = (rotatorAngle + Properties.rotatorSpeed) % 360f;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref rotatorAngle, "angle", 0f);
	}

	public override void PostDraw()
	{
		Vector3 s = default(Vector3);
		s.x = 2.4f;
		s.z = 2.4f;
		s.y = AltitudeLayer.Building.AltitudeFor();
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = parent.DrawPos + Altitudes.AltIncVect;
		pos.z += 0.5f;
		matrix.SetTRS(pos, Quaternion.AngleAxis(rotatorAngle, Vector3.up), s);
		Graphics.DrawMesh(MeshPool.plane10, matrix, Resources.rotatorTexture, 0);
	}
}
