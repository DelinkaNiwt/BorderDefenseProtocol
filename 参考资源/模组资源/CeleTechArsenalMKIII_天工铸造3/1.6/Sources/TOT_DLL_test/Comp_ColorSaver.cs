using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Comp_ColorSaver : ThingComp
{
	public Color GunCamoColor = Color.white;

	public CompProperties_ColorSaver Properties => (CompProperties_ColorSaver)props;

	public Pawn Holder
	{
		get
		{
			if (!(parent?.ParentHolder is Pawn_EquipmentTracker { pawn: var pawn }))
			{
				return null;
			}
			return pawn;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref GunCamoColor, "color", Color.white, forceSave: true);
	}
}
