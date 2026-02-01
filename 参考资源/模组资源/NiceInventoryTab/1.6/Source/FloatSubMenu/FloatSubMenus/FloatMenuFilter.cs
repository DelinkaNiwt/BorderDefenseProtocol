using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace FloatSubMenus;

internal class FloatMenuFilter
{
	private List<FloatMenuOption> options;

	private List<FloatMenuOption> filtered;

	private FloatMenuSizeMode sizeMode;

	private FloatMenu initialized;

	private bool updateSize;

	private (Func<FloatMenuOption, bool> predicate, bool reset, bool recursive) delayed;

	public IEnumerable<FloatMenuOption> Unfiltered => options;

	public IEnumerable<FloatMenuOption> Filtered => filtered;

	public int Count => filtered.Count;

	public void Filter(Func<FloatMenuOption, bool> predicate, bool reset = false, bool recursive = false)
	{
		if (initialized == null)
		{
			delayed = (predicate: predicate, reset: reset, recursive: recursive);
			return;
		}
		filtered.Clear();
		foreach (FloatMenuOption option in options)
		{
			FloatSubMenu floatSubMenu = (recursive ? (option as FloatSubMenu) : null);
			bool flag = reset || predicate(option);
			if (flag || (floatSubMenu != null && floatSubMenu.AnyMatches(predicate, recursive)))
			{
				filtered.Add(option);
				floatSubMenu?.FilterSubMenu(predicate, flag, recursive);
			}
		}
		updateSize = true;
	}

	public void Update(FloatMenu floatMenu, Action onInit = null, Action onResize = null)
	{
		if (initialized != floatMenu)
		{
			Init(floatMenu, onInit);
		}
		if (updateSize)
		{
			UpdateSize(floatMenu, onResize);
		}
	}

	protected void Init(FloatMenu floatMenu, Action action)
	{
		Traverse<List<FloatMenuOption>> traverse = Traverse.Create(floatMenu).Field<List<FloatMenuOption>>("options");
		options = traverse.Value;
		traverse.Value = (filtered = options.ToList());
		initialized = floatMenu;
		action?.Invoke();
		if (delayed.predicate != null)
		{
			Filter(delayed.predicate, delayed.reset, delayed.recursive);
			delayed.predicate = null;
		}
	}

	protected void UpdateSize(FloatMenu floatMenu, Action action)
	{
		FloatMenuSizeMode mode = floatMenu.SizeMode;
		if (sizeMode != mode)
		{
			options.ForEach(delegate(FloatMenuOption x)
			{
				x.SetSizeMode(mode);
			});
			sizeMode = mode;
		}
		floatMenu.windowRect.size = floatMenu.InitialSize;
		floatMenu.windowRect.xMax = Mathf.Min(floatMenu.windowRect.xMax, UI.screenWidth);
		floatMenu.windowRect.yMax = Mathf.Min(floatMenu.windowRect.yMax, UI.screenHeight);
		updateSize = false;
		action?.Invoke();
	}
}
