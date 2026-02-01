using Verse;

public class TWSettings : ModSettings
{
	public static bool ReinforceNotApply;

	public static bool DeveloperMode;

	public bool TWtriggered = false;

	public bool TWtriggered2 = false;

	public bool TWtriggered1 = false;

	public bool TWtriggered3 = false;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ReinforceNotApply, "ReinforceNotApply", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref DeveloperMode, "DeveloperMode", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref TWtriggered, "TWtriggered", defaultValue: false);
		Scribe_Values.Look(ref TWtriggered2, "TWtriggered2", defaultValue: false);
		Scribe_Values.Look(ref TWtriggered1, "TWtriggered1", defaultValue: false);
		Scribe_Values.Look(ref TWtriggered3, "enableCoreTrigger", defaultValue: false);
	}
}
