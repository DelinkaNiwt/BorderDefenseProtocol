using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CMCTurretTop
{
	private Building_CMCTurretGun parentTurret;

	public float curRotationInt;

	public float destRotationInt;

	public static readonly int ArtworkRotation;

	public float IdledestRotation;

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

	static CMCTurretTop()
	{
		ArtworkRotation = -90;
		ArtworkRotation = -90;
	}

	public CMCTurretTop(Building_CMCTurretGun ParentTurret)
	{
		parentTurret = ParentTurret;
	}

	public virtual void DrawTurret(Vector3 drawLoc, Vector3 recoilDrawOffset)
	{
		Vector3 vector = new Vector3(parentTurret.def.building.turretTopOffset.x, 0f, parentTurret.def.building.turretTopOffset.y).RotatedBy(CurRotation);
		float turretTopDrawSize = parentTurret.def.building.turretTopDrawSize;
		float num = parentTurret.CurrentEffectiveVerb?.AimAngleOverride ?? CurRotation;
		Vector3 pos = drawLoc + Altitudes.AltIncVect + vector;
		pos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.13f;
		Quaternion q = ((float)TurretTop.ArtworkRotation + num).ToQuat();
		Vector3 s = new Vector3(turretTopDrawSize, 1f, turretTopDrawSize);
		Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, q, s), parentTurret.TurretTopMaterial, 0);
		ThingDef def = parentTurret.def;
		string texPath = ((def == CMC_Def.CMCML) ? "Things/Buildings/CMC_MissileLauncherTop_Light" : (def.building.turretGunDef.graphicData.texPath + "_Light"));
		Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.MoteGlow, new Color(255f, 255f, 255f));
		Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, q, s), material, 0);
	}

	public void ForceFaceTarget(LocalTargetInfo targ)
	{
		if (targ.IsValid)
		{
			float destRotation = (targ.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
			DestRotation = destRotation;
		}
	}

	public void TurretTopTick()
	{
		LocalTargetInfo currentTarget = parentTurret.CurrentTarget;
		if (currentTarget.IsValid)
		{
			float destRotation = (currentTarget.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
			DestRotation = destRotation;
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
