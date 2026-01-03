using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CMCTurretTop_MainBattery
{
	private Building_CMCTurretGun_MainBattery parentTurret;

	public float curRotationInt;

	public float destRotationInt;

	public static readonly int ArtworkRotation;

	public float IdledestRotation;

	public float recoiloffsetdistance;

	public float CurRotation
	{
		get
		{
			return curRotationInt;
		}
		set
		{
			curRotationInt = value;
			if (curRotationInt > 360f)
			{
				curRotationInt -= 360f;
			}
			if (curRotationInt < 0f)
			{
				curRotationInt += 360f;
			}
		}
	}

	public float DestRotation
	{
		get
		{
			return destRotationInt;
		}
		set
		{
			destRotationInt = value;
			if (destRotationInt > 360f)
			{
				destRotationInt -= 360f;
			}
			if (destRotationInt < 0f)
			{
				destRotationInt += 360f;
			}
		}
	}

	static CMCTurretTop_MainBattery()
	{
		ArtworkRotation = -90;
		ArtworkRotation = -90;
	}

	public CMCTurretTop_MainBattery(Building_CMCTurretGun_MainBattery ParentTurret)
	{
		parentTurret = ParentTurret;
	}

	public virtual void DrawTurret(Vector3 drawLoc, Vector3 recoilDrawOffset, float recoilAngleOffset)
	{
		Vector3 vector = new Vector3(parentTurret.def.building.turretTopOffset.x, 0f, parentTurret.def.building.turretTopOffset.y).RotatedBy(CurRotation);
		float turretTopDrawSize = parentTurret.def.building.turretTopDrawSize;
		float num = parentTurret.CurrentEffectiveVerb?.AimAngleOverride ?? CurRotation;
		Vector3 vector2 = drawLoc + Altitudes.AltIncVect + vector;
		vector2.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.13f;
		Quaternion q = ((float)TurretTop.ArtworkRotation + num).ToQuat();
		Vector3 s = new Vector3(turretTopDrawSize, 1f, turretTopDrawSize);
		Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(vector2, q, s), parentTurret.TurretTopMaterial, 0);
		string texPath = parentTurret.def.building.turretGunDef.graphicData.texPath + "_Light";
		Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.MoteGlow, new Color(255f, 255f, 255f));
		Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(vector2, q, s), material, 0);
		Vector3 vector3 = new Vector3(0f, 0f, 0.97f - parentTurret.CalculateRecoil()).RotatedBy(CurRotation);
		Vector3 pos = vector2 + vector3;
		pos.y -= 0.11f;
		Quaternion q2 = ((float)TurretTop.ArtworkRotation + num).ToQuat();
		string texPath2 = parentTurret.def.building.turretGunDef.graphicData.texPath + "_Ext";
		Material material2 = MaterialPool.MatFrom(texPath2, ShaderDatabase.DefaultShader, new Color(255f, 255f, 255f));
		Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, q2, s), material2, 0);
	}

	public void ForceFaceTarget(LocalTargetInfo targ)
	{
		if (!parentTurret.IsTargrtingWorld)
		{
			if (targ.IsValid)
			{
				float destRotation = (targ.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
				DestRotation = destRotation;
			}
		}
		else
		{
			PlanetLayer selected = PlanetLayer.Selected;
			DestRotation = selected.GetHeadingFromTo(parentTurret.Map.Tile, GameComponent_CeleTech.Instance.ASEA_observedMap.Tile);
		}
	}

	public void TurretTopTick()
	{
		LocalTargetInfo currentTarget = parentTurret.CurrentTarget;
		if (currentTarget.IsValid && !parentTurret.IsTargrtingWorld)
		{
			float destRotation = (currentTarget.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
			DestRotation = destRotation;
		}
		else if (GameComponent_CeleTech.Instance.ASEA_observedMap != null && parentTurret.IsTargrtingWorld)
		{
			DestRotation = Find.World.grid.GetHeadingFromTo(parentTurret.Map.Tile, GameComponent_CeleTech.Instance.ASEA_observedMap.Tile);
		}
		if (Mathf.Abs(CurRotation - DestRotation) <= parentTurret.rotationVelocity * 1.225f)
		{
			CurRotation = DestRotation;
			return;
		}
		bool flag = DestRotation - CurRotation < 180f && CurRotation < DestRotation;
		bool flag2 = CurRotation - DestRotation >= 180f && CurRotation > DestRotation;
		if (flag || flag2)
		{
			CurRotation += parentTurret.rotationVelocity;
		}
		else
		{
			CurRotation -= parentTurret.rotationVelocity;
		}
	}

	public void SetRotationFromOrientation()
	{
		curRotationInt = 0f;
		destRotationInt = 0f;
	}
}
