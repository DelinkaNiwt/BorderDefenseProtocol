using Verse;

namespace NCL;

public class CompProperties_AutoLightningStrike : CompProperties
{
	public int empRadius = 20;

	public bool consumeFromBatteriesOnly = true;

	public int autoStrikeInterval = 300;

	public float autoStrikePowerCost = 500f;

	public DamageDef damageType;

	public int maxTargets = 3;

	public int maxConcurrentStrikes = 3;

	public string uiIconPath = "ModIcon/AutoLightningStrike";

	public string uiLabel = "NCL_AutoStrike_Mode";

	public string uiDescription = "NCL_AutoStrike_ToggleDesc";

	public string overdriveLabel = "NCL_Overdrive_Mode";

	public string overdriveDescription = "NCL_Overdrive_Description";

	public int damageAmount = 50;

	public bool enableOverdrive = true;

	public string overdriveIconPath = "ModIcon/overdrive";

	public float overdrivePowerMultiplier = 2f;

	public int overdriveTargetMultiplier = 2;

	public float overdriveIntervalDivider = 2f;

	public CompProperties_AutoLightningStrike()
	{
		compClass = typeof(CompAutoLightningStrike);
	}
}
