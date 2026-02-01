using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class CompositableLoadoutsIntegration
{
	public static Assembly CLAss;

	public static MethodInfo IsValidLoadoutHolderMI;

	public static MethodInfo EditXLoadoutMI;

	public static PropertyInfo EditOrCreateTagsPI;

	internal static void DoPatch()
	{
		IsValidLoadoutHolderMI = CLAss.GetType("Inventory.Utility").GetMethod("IsValidLoadoutHolder", BindingFlags.Static | BindingFlags.Public);
		EditXLoadoutMI = CLAss.GetType("Inventory.Strings").GetMethod("EditXLoadout", BindingFlags.Static | BindingFlags.Public);
		EditOrCreateTagsPI = CLAss.GetType("Inventory.Strings").GetProperty("EditOrCreateTags", BindingFlags.Static | BindingFlags.Public);
		if (IsValidLoadoutHolderMI == null || EditXLoadoutMI == null || EditOrCreateTagsPI == null)
		{
			Log.Warning(ModIntegration.ModLogPrefix + "CompositableLoadouts method not found! Integration disabled.");
			ModIntegration.CLActive = false;
		}
	}

	internal static void DrawEditButtons(Rect rect, Pawn pawn)
	{
		if (Settings.CLI_VanillaButtons)
		{
			GUI.color = Color.white;
			if (Widgets.ButtonText(rect.LeftHalf(), (string)EditXLoadoutMI.Invoke(null, new object[1] { pawn.LabelShort })))
			{
				OpenDialog("Inventory.Dialog_LoadoutEditor", pawn);
			}
			if (Widgets.ButtonText(rect.RightHalf(), (string)EditOrCreateTagsPI.GetValue(null)))
			{
				OpenDialog("Inventory.Dialog_TagEditor");
			}
			return;
		}
		GUI.color = Color.white;
		var (left, left2) = Utils.SplitRect(rect, 0.5f, 6f);
		if (TransparentButton(left, (string)EditXLoadoutMI.Invoke(null, new object[1] { pawn.LabelShort })))
		{
			OpenDialog("Inventory.Dialog_LoadoutEditor", pawn);
		}
		if (TransparentButton(left2, (string)EditOrCreateTagsPI.GetValue(null)))
		{
			OpenDialog("Inventory.Dialog_TagEditor");
		}
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private static bool TransparentButton(Rect left, string v)
	{
		Text.Font = GameFont.Small;
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(left, v);
		if (Mouse.IsOver(left))
		{
			Widgets.DrawBoxSolid(left, new Color(1f, 1f, 1f, 0.4f));
			return Widgets.ButtonInvisible(left);
		}
		Widgets.DrawBoxSolid(left, new Color(1f, 1f, 1f, 0.1f));
		return false;
	}

	private static void OpenDialog(string className, Pawn pawn = null)
	{
		Type type = CLAss.GetType(className);
		if (!(type == null))
		{
			object obj = ((pawn == null) ? Activator.CreateInstance(type) : Activator.CreateInstance(type, pawn));
			Find.WindowStack.Add((Window)obj);
		}
	}

	internal static bool IsValidLoadoutHolder(Pawn pawn)
	{
		return (bool)IsValidLoadoutHolderMI.Invoke(null, new object[1] { pawn });
	}
}
