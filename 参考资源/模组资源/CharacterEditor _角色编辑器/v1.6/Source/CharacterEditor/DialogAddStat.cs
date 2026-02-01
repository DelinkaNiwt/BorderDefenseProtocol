using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogAddStat : Window
{
	private List<StatCategoryDef> lOfStatCat;

	private List<StatDef> lOfStat;

	private Func<StatCategoryDef, string> FGetStatCategoryLabel;

	private Func<StatDef, string> FGetStatLabel;

	private StatCategoryDef selectedStatCategoryDef;

	private StatDef selectedStatDef;

	private ThingDef thingDef;

	private bool isWeapon;

	private bool isEquip;

	private bool doOnce;

	public override Vector2 InitialSize => new Vector2(600f, 100f);

	private string GetStatLabel(StatDef s)
	{
		return (s == null) ? Label.NONE : s.label;
	}

	private string GetStatCategoryLabel(StatCategoryDef s)
	{
		return (s == null) ? Label.ALL : s.label;
	}

	internal DialogAddStat(ThingDef _thingDef, bool _isWeapon, bool _isEquip)
	{
		thingDef = _thingDef;
		isWeapon = _isWeapon;
		isEquip = _isEquip;
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.AddStat);
		if (isWeapon)
		{
			lOfStatCat = CEditor.API.ListOf<StatCategoryDef>(EType.StatCategoryWeapon);
		}
		else if (isEquip)
		{
			lOfStatCat = CEditor.API.ListOf<StatCategoryDef>(EType.StatCategoryOnEquip);
		}
		else
		{
			lOfStatCat = CEditor.API.ListOf<StatCategoryDef>(EType.StatCategoryApparel);
		}
		lOfStat = new List<StatDef>();
		FGetStatLabel = GetStatLabel;
		FGetStatCategoryLabel = GetStatCategoryLabel;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	public override void DoWindowContents(Rect inRect)
	{
		float w = InitialSize.x - 40f;
		float num = InitialSize.y - 104f;
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.AddStat, ref windowRect, ref doOnce, 0);
		}
		Text.Font = GameFont.Medium;
		SZWidgets.Label(0f, 0f, w, 30f, Label.ADDSTAT);
		Text.Font = GameFont.Small;
		SZWidgets.Label(0f, 35f, 100f, 30f, Label.STATTYPE);
	}

	private void ASetStatCategory(StatCategoryDef statCategoryDef)
	{
		selectedStatCategoryDef = statCategoryDef;
		selectedStatDef = null;
		lOfStat = CEditor.API.ListOfStatDef(statCategoryDef, isWeapon, isEquip);
	}

	private void ASetStat(StatDef stat)
	{
		selectedStatDef = stat;
	}

	private void AAddStat()
	{
		if (thingDef != null)
		{
			if (isEquip)
			{
				thingDef.AddEquipStat(selectedStatDef, 0f);
			}
			else
			{
				thingDef.AddStat(selectedStatDef, 0f);
			}
			if (selectedStatDef == StatDefOf.EnergyShieldEnergyMax || selectedStatDef == StatDefOf.EnergyShieldRechargeRate)
			{
				MessageTypeDef mt = (thingDef.apparel.layers.Contains(ApparelLayerDefOf.Belt) ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.RejectInput);
				MessageTool.Show(Label.ONLYFORSHIELD, mt);
				thingDef.ResolveReferences();
				thingDef.PostLoad();
			}
			Close();
		}
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.AddStat, windowRect.position);
		base.Close(doCloseSound);
	}
}
