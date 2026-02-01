using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NiceInventoryTab;

public class CraftableItem
{
	public ThingDef thingDef;

	public List<Building_WorkTable> workTables;

	public RecipeDef recipe;

	public Precept_Building precept;

	public string displayLabel;

	public CraftableItem(ThingDef thingDef)
	{
		this.thingDef = thingDef;
		workTables = new List<Building_WorkTable>();
	}

	public void AddWorkTable(Building_WorkTable table, RecipeDef recipeForTable = null, Precept_Building precept = null, string label = null)
	{
		if (!workTables.Contains(table))
		{
			workTables.Add(table);
		}
		if (recipe == null && recipeForTable != null)
		{
			recipe = recipeForTable;
		}
		if (precept != null)
		{
			this.precept = precept;
			displayLabel = label ?? ((string)"RecipeMake".Translate(precept.LabelCap).CapitalizeFirst());
		}
		else if (displayLabel == null)
		{
			displayLabel = recipeForTable?.LabelCap ?? thingDef.LabelCap;
		}
	}
}
