using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompBuildingDrawExtraFourRot : ThingComp
{
	private CompPowerTrader _powerComp;

	public CompProperties_BuildingDrawExtraFourRot Properties => (CompProperties_BuildingDrawExtraFourRot)props;

	private CompPowerTrader PowerComp
	{
		get
		{
			if (_powerComp == null)
			{
				_powerComp = parent.GetComp<CompPowerTrader>();
			}
			return _powerComp;
		}
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (PowerComp.PowerOn || PowerComp == null)
		{
			Mesh mesh = Properties.graphicDataExtra.Graphic.MeshAt(parent.Rotation);
			Graphics.DrawMesh(mesh, parent.DrawPos + new Vector3(0f, 1f, 0f) + Properties.graphicDataExtra.DrawOffsetForRot(parent.Rotation), Quaternion.AngleAxis(0f, Vector3.up), Properties.graphicDataExtra.Graphic.MatAt(parent.Rotation), 0);
		}
	}
}
