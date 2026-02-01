using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FloatSubMenus;

public static class ExtensionMethods
{
	public static FloatMenuOption MenuOption(this Pawn pawn, Action action, bool shortName = true)
	{
		return new FloatMenuOption(shortName ? pawn.LabelShortCap : pawn.LabelCap, action, null, Color.white, MenuOptionPriority.Default, null, null, 30f, (Rect r) => DrawPawn(r, pawn), null, playSelectionSound: true, 0, HorizontalJustification.Left, extraPartRightJustified: true);
	}

	public static FloatMenuOption MenuOption(this Pawn pawn, Action<Pawn> action, bool shortName = true)
	{
		return pawn.MenuOption((Action)delegate
		{
			action(pawn);
		}, shortName);
	}

	private static bool DrawPawn(Rect r, Pawn p)
	{
		Widgets.ThingIcon(r.ExpandedBy(6f), p);
		return false;
	}

	public static void OpenMenu(this IEnumerable<FloatMenuOption> menu)
	{
		Find.WindowStack.Add(new FloatMenu(menu.ToList()));
	}

	public static void OpenMenu(this List<FloatMenuOption> menu)
	{
		Find.WindowStack.Add(new FloatMenu(menu));
	}

	public static void OpenMenu(this FloatMenu menu)
	{
		Find.WindowStack.Add(menu);
	}
}
