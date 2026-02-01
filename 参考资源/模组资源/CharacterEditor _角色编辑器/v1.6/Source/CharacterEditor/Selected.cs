using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class Selected
{
	internal Name pawnName;

	internal IntVec3 location = default(IntVec3);

	internal Gender gender = Gender.None;

	internal int age = 1;

	internal PawnKindDef pkd = null;

	internal ThingStyleDef style;

	internal Thing tempThing;

	internal ThingDef thingDef;

	internal ThingDef stuff;

	internal int quality;

	internal int styleIndex;

	internal int oldstyleIndex;

	internal int stuffIndex;

	internal int oldStuffIndex;

	internal int buyPrice;

	internal int oldStackVal;

	internal int stackVal;

	internal Color DrawColor;

	internal HashSet<ThingDef> lOfStuff;

	internal HashSet<ThingStyleDef> lOfStyle;

	internal Texture2D GetTexture2D => thingDef.GetTexture((stackVal >= thingDef.stackLimit) ? thingDef.stackLimit : stackVal, style);

	internal bool HasQuality => thingDef != null && thingDef.HasComp(typeof(CompQuality));

	internal bool HasStack => thingDef != null && thingDef.stackLimit > 1;

	internal bool HasRace => thingDef != null && thingDef.race != null;

	internal Func<ThingStyleDef, string> StyleLabelGetter => (ThingStyleDef x) => (x == null) ? Label.NONE : (x.label.NullOrEmpty() ? x.defName : x.label.CapitalizeFirst());

	internal Func<ThingStyleDef, string> StyleDescrGetter => (ThingStyleDef x) => x?.description;

	internal Func<ThingDef, string> StuffLabelGetter => (ThingDef x) => (x == null) ? Label.NONE : (x.label.NullOrEmpty() ? x.defName : x.label.CapitalizeFirst());

	internal Func<ThingDef, string> StuffDescrGetter => (ThingDef x) => x?.description;

	private Selected(ThingDef t)
	{
		tempThing = null;
		Init(t);
	}

	private Selected(Thing t)
	{
		tempThing = t;
		location = t.Position;
		Init(t.def, random: false, t);
	}

	internal static Selected Random(HashSet<ThingDef> l, bool originalColors, bool randomStakcount = false)
	{
		if (l.NullOrEmpty())
		{
			return null;
		}
		ThingDef thingDef = l.RandomElement();
		if (thingDef == null)
		{
			return null;
		}
		Selected selected = new Selected(thingDef);
		selected.DrawColor = (originalColors ? selected.DrawColor : ColorTool.RandomColor);
		if (randomStakcount)
		{
			selected.stackVal = CEditor.zufallswert.Next(1, selected.thingDef.stackLimit);
		}
		return selected;
	}

	internal static Selected ByName(string defname, string stuffdefname, string styledefname, Color col, int quali, int stack)
	{
		ThingDef t = (defname.NullOrEmpty() ? null : DefTool.ThingDef(defname));
		ThingDef thingDef = (stuffdefname.NullOrEmpty() ? null : DefTool.ThingDef(stuffdefname));
		ThingStyleDef thingStyleDef = (styledefname.NullOrEmpty() ? null : DefTool.ThingStyleDef(styledefname));
		Selected selected = new Selected(t);
		selected.Set(thingDef, thingStyleDef, col, quali, stack);
		return selected;
	}

	internal static Selected ByThing(Thing _thing)
	{
		return new Selected(_thing);
	}

	internal static Selected ByThingDef(ThingDef _thingDef)
	{
		return new Selected(_thingDef);
	}

	private void Set(ThingDef _stuff, ThingStyleDef _style, Color col, int quali, int _stackVal)
	{
		stuff = _stuff;
		style = _style;
		stackVal = _stackVal;
		stuffIndex = ((stuff != null) ? lOfStuff.IndexOf(stuff) : 0);
		styleIndex = ((style != null) ? lOfStyle.IndexOf(style) : 0);
		quality = ((quali < 0) ? ((int)QualityUtility.AllQualityCategories.RandomElement()) : quali);
		DrawColor = col;
	}

	internal void SetStyle(bool next, bool random)
	{
		styleIndex = lOfStyle.NextOrPrevIndex(styleIndex, next, random);
		oldstyleIndex = styleIndex;
		style = lOfStyle.At(styleIndex);
		UpdateBuyPrice();
	}

	internal void SetStyle(ThingStyleDef _style)
	{
		styleIndex = lOfStyle.IndexOf(_style);
		oldstyleIndex = styleIndex;
		style = _style;
		UpdateBuyPrice();
	}

	internal void CheckSetStyle()
	{
		if (styleIndex != oldstyleIndex)
		{
			SetStyle(lOfStyle.At(styleIndex));
		}
	}

	internal void SetStuff(bool next, bool random)
	{
		stuffIndex = lOfStuff.NextOrPrevIndex(stuffIndex, next, random);
		oldStuffIndex = stuffIndex;
		stuff = lOfStuff.At(stuffIndex);
		DrawColor = thingDef.GetColor(stuff);
		UpdateBuyPrice();
	}

	internal void SetStuff(ThingDef _stuff)
	{
		stuffIndex = lOfStuff.IndexOf(_stuff);
		oldStuffIndex = stuffIndex;
		stuff = _stuff;
		DrawColor = thingDef.GetColor(stuff);
		UpdateBuyPrice();
	}

	internal void CheckSetStuff()
	{
		if (stuffIndex != oldStuffIndex)
		{
			SetStuff(lOfStuff.At(stuffIndex));
		}
	}

	internal void UpdateStuffList()
	{
		stuff = ((thingDef != null) ? thingDef.GetStuff(ref lOfStuff, ref stuffIndex) : null);
		UpdateBuyPrice();
		thingDef?.UpdateRecipes();
	}

	internal void Init(ThingDef _thingDef, bool random = true, Thing thing = null, Action postProcess = null)
	{
		thingDef = _thingDef;
		int num = CEditor.zufallswert.Next(100);
		gender = ((num <= 50) ? Gender.Male : Gender.Female);
		num = CEditor.zufallswert.Next(30);
		age = num;
		pkd = ((thingDef != null && thingDef.race != null) ? thingDef.race.AnyPawnKind : null);
		if (thing != null)
		{
			pkd = ((thing.def.race == null) ? null : ((thing.GetType() == typeof(Pawn)) ? ((Pawn)thing).kindDef : thing.def.race.AnyPawnKind));
		}
		stuff = ((thingDef != null) ? thingDef.GetStuff(ref lOfStuff, ref stuffIndex, random) : null);
		DrawColor = ((stuff != null) ? thingDef.GetColor(stuff) : ((thingDef != null) ? thingDef.uiIconColor : Color.white));
		style = ((thingDef != null) ? thingDef.GetStyle(ref lOfStyle, ref styleIndex, random) : null);
		RandomQuality();
		if (thing != null)
		{
			SZWidgets.tDefName = thing.def.defName;
			Set(thing.Stuff, thing.StyleDef, thing.DrawColor, thing.GetQuality(), thing.stackCount);
		}
		stuffIndex = ((stuff != null) ? stuffIndex : 0);
		oldStuffIndex = ((stuff != null) ? stuffIndex : 0);
		styleIndex = ((style != null) ? styleIndex : 0);
		oldstyleIndex = ((style != null) ? styleIndex : 0);
		buyPrice = 0;
		stackVal = thing?.stackCount ?? 1;
		oldStackVal = thing?.stackCount ?? 1;
		UpdateBuyPrice();
		postProcess?.Invoke();
	}

	internal void UpdateBuyPrice()
	{
		if (thingDef == null)
		{
			return;
		}
		double num;
		if (thingDef.HasComp(typeof(CompQuality)))
		{
			num = 0.333333333 + (double)quality * 0.333333333;
			if (quality == 6)
			{
				num += 1.0;
			}
		}
		else
		{
			num = 1.0;
		}
		double num2 = (double)thingDef.GetStatValueAbstract(StatDefOf.MarketValue, stuff) * num;
		double num3 = num2 * (double)stackVal;
		if (num3 < 1.0)
		{
			buyPrice = 1;
		}
		else
		{
			buyPrice = (int)Math.Round(num3);
		}
	}

	internal void RandomQuality()
	{
		quality = (int)QualityUtility.AllQualityCategories.RandomElement();
	}

	internal Selected NewCopy()
	{
		Selected selected = new Selected(thingDef);
		selected.age = age;
		selected.buyPrice = buyPrice;
		selected.DrawColor = DrawColor;
		selected.gender = gender;
		lOfStuff.CopyHashSet(ref selected.lOfStuff);
		lOfStyle.CopyHashSet(ref selected.lOfStyle);
		selected.oldStackVal = oldStackVal;
		selected.oldStuffIndex = oldstyleIndex;
		selected.oldstyleIndex = oldstyleIndex;
		selected.pkd = pkd;
		selected.quality = quality;
		selected.stackVal = stackVal;
		selected.stuff = stuff;
		selected.stuffIndex = stuffIndex;
		selected.style = style;
		selected.styleIndex = styleIndex;
		selected.tempThing = tempThing;
		selected.thingDef = thingDef;
		return selected;
	}

	internal List<Selected> SplitInStacks()
	{
		if (thingDef == null)
		{
			return null;
		}
		if (thingDef.stackLimit >= stackVal)
		{
			return new List<Selected> { this };
		}
		List<Selected> list = new List<Selected>();
		int num = stackVal;
		do
		{
			Selected selected = NewCopy();
			selected.stackVal = ((num > thingDef.stackLimit) ? thingDef.stackLimit : num);
			selected.oldStackVal = selected.stackVal;
			num -= thingDef.stackLimit;
			list.Add(selected);
		}
		while (num > 0);
		return list;
	}

	internal List<Thing> Generate()
	{
		if (thingDef == null)
		{
			return null;
		}
		if (thingDef.IsApparel)
		{
			return new List<Thing> { ApparelTool.GenerateApparel(this) };
		}
		if (thingDef.IsWeapon)
		{
			return new List<Thing> { WeaponTool.GenerateWeapon(this) };
		}
		List<Thing> list = new List<Thing>();
		List<Selected> list2 = SplitInStacks();
		foreach (Selected item2 in list2)
		{
			Thing item = ThingTool.GenerateItem(item2);
			list.Add(item);
		}
		return list;
	}
}
