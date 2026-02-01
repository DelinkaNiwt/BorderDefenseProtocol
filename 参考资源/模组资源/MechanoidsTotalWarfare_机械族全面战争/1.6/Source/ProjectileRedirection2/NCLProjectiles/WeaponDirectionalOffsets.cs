using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class WeaponDirectionalOffsets
{
	public Vector3 northSize = Vector3.one;

	public Vector3 eastSize = Vector3.one;

	public Vector3 westSize = Vector3.one;

	public Vector3 southSize = Vector3.one;

	public Vector3 north = Vector3.zero;

	public Vector3 east = Vector3.zero;

	public Vector3 west = Vector3.zero;

	public Vector3 south = Vector3.zero;

	public float northAngle;

	public float eastAngle;

	public float westAngle;

	public float southAngle;

	public Vector3 GetSize(Rot4 rotation)
	{
		return rotation.AsInt switch
		{
			0 => northSize, 
			1 => eastSize, 
			3 => westSize, 
			_ => southSize, 
		};
	}

	public Vector3 GetOffset(Rot4 rotation)
	{
		return rotation.AsInt switch
		{
			0 => north, 
			1 => east, 
			3 => west, 
			_ => south, 
		};
	}

	public float GetAngle(Rot4 rotation)
	{
		return rotation.AsInt switch
		{
			0 => northAngle, 
			1 => eastAngle, 
			3 => westAngle, 
			_ => southAngle, 
		};
	}
}
