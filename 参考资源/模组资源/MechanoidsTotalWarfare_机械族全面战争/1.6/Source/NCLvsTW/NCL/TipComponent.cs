using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NCL;

public class TipComponent : WorldComponent
{
	public int TWCurrentDaySpecialHediffCount;

	private int lastCheckedDay = -1;

	public int TWCurrentDaySpecialHediffCountA;

	public bool TWtriggered3 = false;

	public int lastActivationTick = -1;

	public int TWCurrentDayAirstrikeCount = 0;

	public float num = 0f;

	public bool TWtriggered = false;

	public bool TWtriggered2 = false;

	public bool TWtriggered1 = false;

	public int defeated = 0;

	private bool puzzleFlag = false;

	public List<string> list_str = new List<string>();

	public static bool ReinforceNotApply;

	public static bool DeveloperMode;

	public TipComponent(World world)
		: base(world)
	{
	}

	public override void WorldComponentTick()
	{
		base.WorldComponentTick();
		Map currentMap = Find.CurrentMap;
		if (currentMap != null)
		{
			int currentDay = GenLocalDate.DayOfYear(currentMap);
			if (currentDay != lastCheckedDay)
			{
				TWCurrentDaySpecialHediffCount = 0;
				TWCurrentDaySpecialHediffCountA = 0;
				TWCurrentDayAirstrikeCount = 0;
				lastCheckedDay = currentDay;
			}
		}
		if (TWSettings.ReinforceNotApply || (Find.TickManager.TicksGame & 0x3FF) != 511)
		{
			return;
		}
		bool hasPlayerHomeMaps = false;
		float totalWealth = 0f;
		foreach (Map map in Find.Maps)
		{
			if (map != null && map.IsPlayerHome)
			{
				totalWealth += map.wealthWatcher.WealthTotal;
				hasPlayerHomeMaps = true;
			}
		}
		if (DebugSettings.ShowDevGizmos)
		{
			num = totalWealth;
			if (totalWealth < TotalWarfareSettings.WealthTriggerThreshold)
			{
				TWtriggered2 = false;
			}
		}
		if (TWSettings.DeveloperMode)
		{
			Log.Message($"NCL_TOTALWARFARE_START_LOG: Wealth={totalWealth}, Threshold={TotalWarfareSettings.WealthTriggerThreshold}, Triggered={TWtriggered2}");
		}
		if (!(!TWtriggered2 && hasPlayerHomeMaps))
		{
			return;
		}
		num = totalWealth;
		if (totalWealth > TotalWarfareSettings.WealthTriggerThreshold)
		{
			TWtriggered2 = true;
			string triggerText = "NCL_TOTALWARFARE_AUTO_TRIGGER_TEXT".Translate(TotalWarfareSettings.WealthTriggerThreshold.ToString("N0"));
			if (Find.LetterStack != null)
			{
				Find.LetterStack.ReceiveLetter("NCL_TOTALWARFARE_AUTO_TRIGGER_TITLE".Translate(), triggerText, LetterDefOf.NeutralEvent);
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref TWSettings.ReinforceNotApply, "ReinforceNotApply", defaultValue: false);
		Scribe_Values.Look(ref TWSettings.DeveloperMode, "DeveloperMode", defaultValue: false);
		Scribe_Values.Look(ref TWtriggered, "TWtriggered", defaultValue: false);
		Scribe_Values.Look(ref TWtriggered2, "TWtriggered2", defaultValue: false);
		Scribe_Values.Look(ref TWtriggered1, "TWtriggered1", defaultValue: false);
		Scribe_Values.Look(ref TWtriggered3, "TWtriggered3", defaultValue: false);
		Scribe_Values.Look(ref lastActivationTick, "lastActivationTick", -1);
		Scribe_Values.Look(ref TWCurrentDaySpecialHediffCount, "TWCurrentDaySpecialHediffCount", 0);
		Scribe_Values.Look(ref TWCurrentDaySpecialHediffCountA, "TWCurrentDaySpecialHediffCountA", 0);
		Scribe_Values.Look(ref TWCurrentDayAirstrikeCount, "TWCurrentDayAirstrikeCount", 0);
		Scribe_Values.Look(ref lastCheckedDay, "lastCheckedDay", -1);
	}
}
