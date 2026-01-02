using System;
using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class SettingUtility
{
	public static string TabInfoLabel(TabAvailable enable)
	{
		return enable switch
		{
			TabAvailable.Button => "Ancot.TabAvailable_Button".Translate(), 
			TabAvailable.Menu => "Ancot.TabAvailable_Menu".Translate(), 
			TabAvailable.ButtonAndMenu => "Ancot.TabAvailable_ButtonAndMenu".Translate(), 
			_ => "", 
		};
	}

	public static List<FloatMenuOption> TabSettingMenu(Action<TabAvailable> setTab)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (TabAvailable enable in Enum.GetValues(typeof(TabAvailable)))
		{
			list.Add(new FloatMenuOption(TabInfoLabel(enable), delegate
			{
				setTab(enable);
			}));
		}
		return list;
	}
}
