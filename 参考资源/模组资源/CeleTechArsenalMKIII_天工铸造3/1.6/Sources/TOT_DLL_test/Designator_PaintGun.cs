using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Designator_PaintGun : Designator_Paint
{
	private static HashSet<Thing> tmpPaintThings = new HashSet<Thing>();

	protected override Texture2D IconTopTex => ContentFinder<Texture2D>.Get("UI/Designators/Paint_Top");

	protected override DesignationDef Designation => DesignationDefOf.PaintBuilding;

	public Designator_PaintGun()
	{
		defaultLabel = "CMC_GunColorPicker".Translate();
		defaultDesc = "CMC_GunColorPickerDesciption".Translate();
		icon = ContentFinder<Texture2D>.Get("UI/Designators/Paint_Bottom");
	}

	public override AcceptanceReport CanDesignateCell(IntVec3 c)
	{
		if (eyedropMode)
		{
			return eyedropper.CanDesignateCell(c);
		}
		if (!c.InBounds(base.Map) || c.Fogged(base.Map))
		{
			return false;
		}
		List<Thing> thingList = c.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if ((bool)CanDesignateThing(thingList[i]))
			{
				return true;
			}
		}
		return "MessageMustSelectCMCGun".Translate(colorDef);
	}

	public override void DesignateSingleCell(IntVec3 c)
	{
		if (eyedropMode)
		{
			eyedropper.DesignateSingleCell(c);
			return;
		}
		List<Thing> thingList = c.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (CanDesignateThing(thingList[i]).Accepted)
			{
				DesignateThing(thingList[i]);
			}
		}
	}

	public override AcceptanceReport CanDesignateThing(Thing t)
	{
		if (t is Pawn)
		{
			Pawn pawn = (Pawn)t;
			Pawn_EquipmentTracker equipment = pawn.equipment;
			ThingWithComps primary = pawn.equipment.Primary;
			Comp_WeaponRenderStatic comp_WeaponRenderStatic = primary.TryGetComp<Comp_WeaponRenderStatic>();
			if (comp_WeaponRenderStatic == null)
			{
				return false;
			}
			if (primary != null && comp_WeaponRenderStatic.Camocolor == colorDef.color)
			{
				return false;
			}
			return true;
		}
		Comp_WeaponRenderStatic comp_WeaponRenderStatic2 = t.TryGetComp<Comp_WeaponRenderStatic>();
		if (comp_WeaponRenderStatic2 == null)
		{
			return false;
		}
		if (comp_WeaponRenderStatic2 != null && comp_WeaponRenderStatic2.Camocolor == colorDef.color)
		{
			return true;
		}
		return true;
	}

	public override void DesignateThing(Thing t)
	{
		Comp_WeaponRenderStatic comp_WeaponRenderStatic;
		if (t is Pawn)
		{
			Pawn pawn = t as Pawn;
			Pawn_EquipmentTracker equipment = pawn.equipment;
			ThingWithComps primary = pawn.equipment.Primary;
			comp_WeaponRenderStatic = primary.TryGetComp<Comp_WeaponRenderStatic>();
		}
		else
		{
			comp_WeaponRenderStatic = t.TryGetComp<Comp_WeaponRenderStatic>();
		}
		if (comp_WeaponRenderStatic != null)
		{
			comp_WeaponRenderStatic.Camocolor = colorDef.color;
			comp_WeaponRenderStatic.UpdateCamoColor();
		}
	}

	protected override int NumHighlightedCells()
	{
		tmpPaintThings.Clear();
		Find.DesignatorManager.Dragger.UpdateCellBuffer();
		foreach (IntVec3 item in Find.DesignatorManager.Dragger.CellBuffer)
		{
			if (item.Fogged(base.Map))
			{
				continue;
			}
			List<Thing> thingList = item.GetThingList(base.Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				if (!tmpPaintThings.Contains(thingList[i]) && (bool)CanDesignateThing(thingList[i]))
				{
					tmpPaintThings.Add(thingList[i]);
				}
			}
		}
		return tmpPaintThings.Count;
	}
}
