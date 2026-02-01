using Verse;

namespace NCL;

public class TotalWarfareSettings : ModSettings
{
	public static float AirstrikeWealthThreshold = 500000f;

	public static float WealthTriggerThreshold = 1000000f;

	public bool InvisibilityVisibleToPlayer = true;

	public static bool EnableMechEnhancement = true;

	public static int MaxAirstrikeHediffsPerDay = 1;

	public static int MaxSpecialHediffsPerDay = 25;

	public static int MaxSpecialHediffsPerDayA = 50;

	public static bool EnableAutoTrigger = true;

	public static bool EnableCoreTrigger = true;

	public static bool ShowDevLogs = false;

	internal static int AdvancedNodeCount = 5;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref EnableMechEnhancement, "EnableMechEnhancement", defaultValue: true);
		Scribe_Values.Look(ref EnableAutoTrigger, "EnableAutoTrigger", defaultValue: true);
		Scribe_Values.Look(ref EnableCoreTrigger, "EnableCoreTrigger", defaultValue: true);
		Scribe_Values.Look(ref MaxSpecialHediffsPerDay, "MaxSpecialHediffsPerDay", 25);
		Scribe_Values.Look(ref MaxSpecialHediffsPerDayA, "MaxSpecialHediffsPerDayA", 50);
		Scribe_Values.Look(ref MaxAirstrikeHediffsPerDay, "MaxAirstrikeHediffsPerDay", 1);
		Scribe_Values.Look(ref WealthTriggerThreshold, "WealthTriggerThreshold", 1000000f);
		Scribe_Values.Look(ref AirstrikeWealthThreshold, "AirstrikeWealthThreshold", 500000f);
		Scribe_Values.Look(ref InvisibilityVisibleToPlayer, "InvisibilityVisibleToPlayer", defaultValue: true);
	}
}
