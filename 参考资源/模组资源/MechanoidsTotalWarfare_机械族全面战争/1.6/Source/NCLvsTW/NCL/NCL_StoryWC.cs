using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace NCL;

internal class NCL_StoryWC : WorldComponent
{
	private int ticks = -1;

	private string lastKnownVersion = string.Empty;

	private bool ranActionsOnceAtStartUp;

	public Dictionary<string, bool> storyFlags;

	private const string currentVersion = "2.0.1";

	private const bool forceShowUpdateInfo = false;

	public NCL_StoryWC(World world)
		: base(world)
	{
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref storyFlags, "storyFlags", LookMode.Value);
		Scribe_Values.Look(ref lastKnownVersion, "lastKnownVersion", string.Empty);
		Scribe_Values.Look(ref ticks, "ticks", -1);
	}

	public override void FinalizeInit(bool fromLoad)
	{
		base.FinalizeInit(fromLoad);
		if (storyFlags == null)
		{
			storyFlags = new Dictionary<string, bool>();
		}
		if (!storyFlags.ContainsKey("NCL_Display_Settings_Menu"))
		{
			storyFlags["NCL_Display_Settings_Menu"] = false;
		}
		if (CheckVersionUpdate())
		{
			storyFlags["NCL_Display_Settings_Menu"] = false;
			Log.Message("NCL_Log_VersionUpdated".Translate());
		}
	}

	public override void WorldComponentTick()
	{
		base.WorldComponentTick();
		if (!ranActionsOnceAtStartUp)
		{
			RunActionsOnceAtStartUp();
			ranActionsOnceAtStartUp = true;
		}
		if (ticks == 260)
		{
			bool shouldShowMenu = false;
			bool isVersionUpdated = CheckVersionUpdate();
			if (isVersionUpdated)
			{
				shouldShowMenu = true;
			}
			else if (!storyFlags["NCL_Display_Settings_Menu"] && !ModSettingsAbout.GG_Disable_Settings_Window)
			{
				shouldShowMenu = true;
			}
			if (shouldShowMenu)
			{
				Find.WindowStack.Add(new NewStorySettings(isVersionUpdated, "2.0.1"));
				if (isVersionUpdated)
				{
					Log.Message("更新信息菜单显示任务执行完成");
					lastKnownVersion = "2.0.1";
				}
				else
				{
					Log.Message("常规设置菜单显示任务执行完成");
				}
				storyFlags["NCL_Display_Settings_Menu"] = true;
			}
		}
		ticks++;
	}

	private void RunActionsOnceAtStartUp()
	{
		try
		{
			Log.Message("正在执行启动时的初始化操作");
		}
		catch (Exception ex)
		{
			Log.Error("执行启动操作时出错: " + ex.Message);
		}
	}

	private bool CheckVersionUpdate()
	{
		bool flag = false;
		return !string.IsNullOrEmpty(lastKnownVersion) && "2.0.1" != lastKnownVersion;
	}
}
