using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class WeaponClass_EXRender : ThingWithComps
{
	public Color GunCamoColor = Color.gray;

	public Color GunLightColor = Color.white;

	public Pawn Holder
	{
		get
		{
			if (!(this?.ParentHolder is Pawn_EquipmentTracker { pawn: var pawn }))
			{
				return null;
			}
			return pawn;
		}
	}
}
