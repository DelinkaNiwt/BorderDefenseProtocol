using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class SpinTurretTop
{
	private Building_SpinTurretGun parentTurret;

	private float curRotationInt;

	private int ticksUntilIdleTurn;

	private int idleTurnTicksLeft;

	private bool idleTurnClockwise;

	private const float IdleTurnDegreesPerTick = 0.26f;

	private const int IdleTurnDuration = 140;

	private const int IdleTurnIntervalMin = 150;

	private const int IdleTurnIntervalMax = 350;

	public static readonly int ArtworkRotation = -90;

	public LocalTargetInfo CurrentTarget => parentTurret.CurrentTarget;

	public bool TargetIsValid => CurrentTarget.IsValid;

	public float TargetRotation
	{
		get
		{
			if (TargetIsValid)
			{
				return (CurrentTarget.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
			}
			return parentTurret.CurRotation;
		}
	}

	public float deltaRotation
	{
		get
		{
			if (TargetIsValid)
			{
				float num = TargetRotation - parentTurret.CurRotation;
				if (num <= -180f)
				{
					num += 360f;
				}
				if (num > 180f)
				{
					num -= 360f;
				}
				return num;
			}
			return 0f;
		}
	}

	public bool CanShoot => deltaRotation <= RotateSpeed && deltaRotation >= 0f - RotateSpeed && TargetIsValid;

	public bool CanContinueShoot => deltaRotation <= RotateSpeed + ShootAngleLimit && deltaRotation >= 0f - RotateSpeed - ShootAngleLimit && TargetIsValid;

	private float RotateSpeed => parentTurret.TurretRotateSpeed;

	private float ShootAngleLimit => parentTurret.ShootAngleLimit;

	public SpinTurretTop(Building_SpinTurretGun ParentTurret)
	{
		parentTurret = ParentTurret;
	}

	public void TurretTopTick()
	{
		if (TargetIsValid)
		{
			if (CanShoot)
			{
				parentTurret.CurRotation = TargetRotation;
			}
			else if (deltaRotation > 0f)
			{
				parentTurret.CurRotation += RotateSpeed;
			}
			else if (deltaRotation < 0f)
			{
				parentTurret.CurRotation -= RotateSpeed;
			}
			ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
		}
		else if (ticksUntilIdleTurn > 0)
		{
			ticksUntilIdleTurn--;
			if (ticksUntilIdleTurn == 0)
			{
				if (Rand.Value < 0.5f)
				{
					idleTurnClockwise = true;
				}
				else
				{
					idleTurnClockwise = false;
				}
				idleTurnTicksLeft = 140;
			}
		}
		else
		{
			if (idleTurnClockwise)
			{
				parentTurret.CurRotation += 0.26f;
			}
			else
			{
				parentTurret.CurRotation -= 0.26f;
			}
			idleTurnTicksLeft--;
			if (idleTurnTicksLeft <= 0)
			{
				ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
			}
		}
	}

	public void DrawTurret(Vector3 drawLoc, Vector3 recoilDrawOffset, float recoilAngleOffset)
	{
		Vector3 v = new Vector3(parentTurret.def.building.turretTopOffset.x, 0f, parentTurret.def.building.turretTopOffset.y).RotatedBy(parentTurret.CurRotation);
		float turretTopDrawSize = parentTurret.def.building.turretTopDrawSize;
		v = v.RotatedBy(recoilAngleOffset);
		v += recoilDrawOffset;
		float num = parentTurret.CurrentEffectiveVerb?.AimAngleOverride ?? parentTurret.CurRotation;
		Vector3 pos = drawLoc + Altitudes.AltIncVect + v;
		Quaternion q = ((float)TurretTop.ArtworkRotation + num).ToQuat();
		Graphics.DrawMesh(matrix: Matrix4x4.TRS(pos, q, new Vector3(turretTopDrawSize, 1f, turretTopDrawSize)), mesh: MeshPool.plane10, material: parentTurret.TurretTopMaterial, layer: 0);
	}
}
