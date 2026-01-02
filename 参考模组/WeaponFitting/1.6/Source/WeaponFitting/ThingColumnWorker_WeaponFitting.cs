using System;
using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace WeaponFitting;

public class ThingColumnWorker_WeaponFitting : ThingColumnWorker
{
	public GameComponent_AncotLibrary GC => GameComponent_AncotLibrary.GC;

	public int Slots => GC.MaxTraitSlots;

	protected virtual int Width => Math.Max(def.width, def.width * Slots / 3);

	protected virtual int Padding => 2;

	public override void DoCell(Rect rect, Thing thing, ThingTable table)
	{
		CompUniqueWeapon comp = thing.TryGetComp<CompUniqueWeapon>();
		if (comp == null)
		{
			return;
		}
		List<WeaponTraitDef> traits = comp.TraitsListForReading;
		int traitAmount = traits.Count;
		int slots = comp.WeaponSlots();
		for (int i = 0; i < slots; i++)
		{
			Vector2 iconSize = GetIconSize(thing);
			Rect apRect = new Rect(rect.x + rect.height * (float)i, rect.y, iconSize.x, iconSize.y);
			if (i < traitAmount)
			{
				DrawFittingIcon(apRect, traits[i], isRecipe: false);
			}
		}
	}

	public override int GetMinWidth(ThingTable table)
	{
		return Mathf.Max(base.GetMinWidth(table), Width);
	}

	public override int GetMaxWidth(ThingTable table)
	{
		return Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table));
	}

	public override int GetMinCellHeight(Thing thing)
	{
		return Mathf.Max(base.GetMinCellHeight(thing), Mathf.CeilToInt(GetIconSize(thing).y));
	}

	private void DrawFittingIcon(Rect rect, WeaponTraitDef trait, bool isRecipe)
	{
		if (isRecipe)
		{
			GUI.color = new Color(1f, 1f, 1f, 0.3f);
		}
		ThingDef fittingDef = WeaponTraitsUtility.FittingDef(trait);
		GUI.DrawTexture(rect.ScaledBy(0.8f), (Texture)fittingDef.uiIcon);
		GUI.color = Color.white;
		if (Mouse.IsOver(rect))
		{
			string tooltipText = (isRecipe ? string.Concat("Milian.ComponentInRecipePrefix".Translate(), GetIconTip(fittingDef)) : GetIconTip(fittingDef));
			TooltipHandler.TipRegion(rect, tooltipText);
		}
	}

	protected virtual string GetIconTip(ThingDef def)
	{
		if (def == null)
		{
			return "";
		}
		return def.LabelCap;
	}

	protected virtual Vector2 GetIconSize(Thing thing)
	{
		return new Vector2(30f, 30f);
	}
}
