using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CharacterEditor;

internal static class WorkTool
{
	internal static List<WorkTypeDef> GetAllWorkTypeDefs()
	{
		return DefDatabase<WorkTypeDef>.AllDefs.ToList();
	}

	internal static string GetWorkPrioritiesAsSeparatedString(this Pawn p)
	{
		if (!p.HasWorkTracker())
		{
			return "";
		}
		string text = "";
		List<WorkTypeDef> allWorkTypeDefs = GetAllWorkTypeDefs();
		p.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
		foreach (WorkTypeDef item in allWorkTypeDefs)
		{
			text = text + item.defName + "|";
			text += p.workSettings.GetPriority(item);
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetWorkPrioritiesFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasWorkTracker())
		{
			return;
		}
		try
		{
			p.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
			p.workSettings.DisableAll();
			if (s.NullOrEmpty())
			{
				return;
			}
			string[] array = s.SplitNo(":");
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length == 2)
				{
					WorkTypeDef workTypeDef = DefTool.WorkTypeDef(array3[0]);
					if (workTypeDef != null)
					{
						int priority = array3[1].AsInt32();
						p.workSettings.SetPriority(workTypeDef, priority);
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}
}
