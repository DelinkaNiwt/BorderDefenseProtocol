using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCLProjectiles;

public static class WeaponUtility
{
	public const float DefaultIdleAngle = 143f;

	public const float ReverseIdleAngle = 217f;

	public static readonly Vector3 BaseEquippedDistanceOffset = new Vector3(0f, 0f, 0.4f);

	public static readonly Vector3 EquipOffsetNorth = new Vector3(0f, 0f, -0.11f);

	public static readonly Vector3 EquipOffsetEast = new Vector3(0.22f, 0f, -0.22f);

	public static readonly Vector3 EquipOffsetSouth = new Vector3(0f, 0f, -0.22f);

	public static readonly Vector3 EquipOffsetWest = new Vector3(-0.22f, 0f, -0.22f);

	public static (Vector3, float) CalculateEquipmentOrientation(Thing equipment, Pawn wielder)
	{
		if (equipment == null || wielder == null)
		{
			return (Vector3.zero, 0f);
		}
		float equipmentDrawDistanceFactor = wielder.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
		Vector3 vector = wielder.DrawPosHeld ?? Vector3.zero;
		float num = 0f;
		Job curJob = wielder.CurJob;
		if (curJob != null)
		{
			JobDef def = curJob.def;
			if (def != null && !def.neverShowWeapon && wielder.stances?.curStance is Stance_Busy stance_Busy && stance_Busy.focusTarg.IsValid)
			{
				Vector3 centerVector = stance_Busy.focusTarg.CenterVector3;
				if ((centerVector - vector).MagnitudeHorizontalSquared() > 0.001f)
				{
					num = (centerVector - vector).Yto0().AngleFlat();
				}
				Verb currentEffectiveVerb = wielder.CurrentEffectiveVerb;
				if (currentEffectiveVerb != null && currentEffectiveVerb.AimAngleOverride.HasValue)
				{
					num = currentEffectiveVerb.AimAngleOverride.Value;
				}
				vector += (BaseEquippedDistanceOffset + new Vector3(0f, 0f, equipment.def.equippedDistanceOffset)).RotatedBy(num) * equipmentDrawDistanceFactor;
				WeaponWithAttachments.isAiming = true;
				return (vector, num);
			}
		}
		WeaponWithAttachments.isAiming = false;
		num = 143f;
		switch (wielder.Rotation.AsInt)
		{
		case 0:
			vector += EquipOffsetNorth * equipmentDrawDistanceFactor;
			if (equipment is WeaponWithAttachments { DrawNorthIdleMirrored: not false })
			{
				num = 217f;
			}
			break;
		case 1:
			vector += EquipOffsetEast * equipmentDrawDistanceFactor;
			break;
		case 2:
			vector += EquipOffsetSouth * equipmentDrawDistanceFactor;
			break;
		case 3:
			vector += EquipOffsetWest * equipmentDrawDistanceFactor;
			num = 217f;
			break;
		}
		return (vector, num);
	}

	public static (Mesh, Vector3, float) CalculateEquipmentAiming(Thing parent, Thing equipment, Vector3 location, float aimAngle, float equippedAngleOffset, bool useRecoil, WeaponDirectionalIdleConfiguration idleConfig = null)
	{
		Mesh item = MeshPool.plane10;
		float num = aimAngle - 90f;
		if (idleConfig != null && parent != null && equipment != null && !WeaponWithAttachments.isAiming)
		{
			bool flipped = false;
			idleConfig.ApplyConfiguration(ref aimAngle, ref flipped, parent.Rotation);
			num = aimAngle - 90f;
			if (flipped)
			{
				item = MeshPool.plane10Flip;
				num -= 180f;
				num -= equippedAngleOffset;
			}
			else
			{
				num += equippedAngleOffset;
			}
			useRecoil = false;
		}
		else if (aimAngle > 200f && aimAngle < 340f)
		{
			item = MeshPool.plane10Flip;
			num -= 180f;
			num -= equippedAngleOffset;
		}
		else
		{
			num += equippedAngleOffset;
		}
		num %= 360f;
		if (useRecoil)
		{
			CompEquippable compEquippable = equipment.TryGetComp<CompEquippable>();
			if (compEquippable != null)
			{
				EquipmentUtility.Recoil(equipment.def, EquipmentUtility.GetRecoilVerb(compEquippable.AllVerbs), out var drawOffset, out var angleOffset, aimAngle);
				location += drawOffset;
				num += angleOffset;
			}
		}
		return (item, location, num);
	}
}
