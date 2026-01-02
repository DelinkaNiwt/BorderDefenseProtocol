using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Comp_PrismTowerTop : ThingComp
{
	public static readonly Material rotatorTexture0 = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/PT_0", ShaderDatabase.Cutout);

	public static readonly Material rotatorTexture1 = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/PT_1", ShaderDatabase.Cutout);

	public static readonly Material rotatorTexture2 = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/PT_2", ShaderDatabase.Cutout);

	public static readonly Material rotatorTexture3 = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/PT_3", ShaderDatabase.Cutout);

	public static readonly Material rotatorTexture4 = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/PT_4", ShaderDatabase.Cutout);

	public static readonly Material rotatorTexture5 = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/PT_5", ShaderDatabase.Cutout);

	public static readonly Material rotatorTexture6 = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/PT_6", ShaderDatabase.Cutout);

	public static readonly Material rotatorTexture7 = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/PT_7", ShaderDatabase.Cutout);

	public static readonly Material staticLight = MaterialPool.MatFrom("Things/Buildings/CMC_LaserTower_Light", ShaderDatabase.MoteGlow);

	public static List<Material> rotatorTexture = new List<Material> { rotatorTexture0, rotatorTexture1, rotatorTexture2, rotatorTexture3, rotatorTexture4, rotatorTexture5, rotatorTexture6, rotatorTexture7 };

	private Material rotatorMaterial;

	private CompPowerTrader compPowerTrader;

	private bool act = false;

	public int i = 0;

	public CompProperties_PrismTowerTop Properties => (CompProperties_PrismTowerTop)props;

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
			act = true;
			if (Find.TickManager.TicksGame % 7 == 0)
			{
				rotatorMaterial = rotatorTexture[i];
				i = (i + 1) % 8;
			}
		}
		else
		{
			act = false;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref rotatorMaterial, "materialTop", rotatorTexture0);
		Scribe_Values.Look(ref i, "i", 0);
	}

	public override void PostDraw()
	{
		Vector3 s = new Vector3(2f, 0f, 2f);
		Vector3 s2 = new Vector3(4.3f, 0f, 4.3f);
		Matrix4x4 matrix = default(Matrix4x4);
		Matrix4x4 matrix2 = default(Matrix4x4);
		Vector3 vector = parent.DrawPos + Altitudes.AltIncVect;
		vector.y = AltitudeLayer.BuildingOnTop.AltitudeFor() - 0.1f;
		Vector3 pos = vector;
		pos.z += 0.72f;
		matrix2.SetTRS(pos, Quaternion.identity, s2);
		vector.z += 2.2f;
		matrix.SetTRS(vector, Quaternion.AngleAxis(0f, Vector3.up), s);
		if (rotatorMaterial == null)
		{
			rotatorMaterial = rotatorTexture0;
		}
		if (act)
		{
			Graphics.DrawMesh(MeshPool.plane10, matrix2, staticLight, 0);
		}
		Graphics.DrawMesh(MeshPool.plane10, matrix, rotatorMaterial, 0);
	}
}
