using UnityEngine;

namespace NCLProjectiles;

public class WeaponOrientationData
{
	public bool initialized;

	public Mesh mesh;

	public Vector3 position;

	public float aimAngle;

	public float drawAngle;

	public Quaternion rotation;

	public string DebugString => $"(position={position},aim={aimAngle},draw={drawAngle})";

	public void CopyFrom(WeaponOrientationData other)
	{
		initialized = other.initialized;
		mesh = other.mesh;
		position = other.position;
		aimAngle = other.aimAngle;
		drawAngle = other.drawAngle;
		rotation = other.rotation;
	}

	public void CopyFromIfNotInitialized(WeaponOrientationData other)
	{
		if (!initialized)
		{
			CopyFrom(other);
		}
	}

	public WeaponOrientationData Clone()
	{
		WeaponOrientationData weaponOrientationData = new WeaponOrientationData();
		weaponOrientationData.CopyFrom(this);
		return weaponOrientationData;
	}
}
