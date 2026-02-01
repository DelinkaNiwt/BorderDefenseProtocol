using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FloatSubMenus;

public class FloatMenuSearch : FloatMenuOption
{
	private const float Margin = 2f;

	private const float Width = 240f;

	private const float Height = 28f;

	private readonly FloatMenuFilter filter = new FloatMenuFilter();

	private readonly QuickSearchWidget search = new QuickSearchWidget();

	private readonly Traverse<float> widthField;

	private readonly Traverse<float> heightField;

	private readonly bool subMenus;

	public FloatMenuSearch(bool subMenus = false)
		: base(" ", delegate
		{
		})
	{
		extraPartOnGUI = ExtraPart;
		extraPartWidth = 240f;
		extraPartRightJustified = true;
		action = OnClicked;
		this.subMenus = subMenus;
		Traverse traverse = Traverse.Create(this);
		widthField = traverse.Field<float>("cachedRequiredWidth");
		heightField = traverse.Field<float>("cachedRequiredHeight");
	}

	private void OnClicked()
	{
		search.Focus();
	}

	private void Filter()
	{
		filter.Filter((FloatMenuOption x) => x == this || search.filter.Matches(x.Label), !search.filter.Active, subMenus);
		search.noResultsMatched = filter.Count <= 1;
	}

	private bool ExtraPart(Rect rect)
	{
		rect.height = 28f;
		search.OnGUI(rect.ContractedBy(2f), Filter);
		return false;
	}

	public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
	{
		filter.Update(floatMenu, Filter, AfterSizeMode);
		extraPartWidth = rect.width;
		base.DoGUI(rect, colonistOrdering, floatMenu);
		return false;
	}

	private void AfterSizeMode()
	{
		widthField.Value = 240f;
		heightField.Value = 28f;
	}
}
