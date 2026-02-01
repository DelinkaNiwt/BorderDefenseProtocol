using UnityEngine;
using Verse;

namespace NCL_Storyteller;

public class NCL_Storyteller_Mod : Mod
{
	public NCL_Storyteller_Settings settings;

	public NCL_Storyteller_Mod(ModContentPack content)
		: base(content)
	{
		settings = GetSettings<NCL_Storyteller_Settings>();
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		string buffer = settings.maxDefaultThreatPoints.ToString();
		string buffer2 = settings.maxPawns.ToString();
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(inRect);
		listing_Standard.Label("maxDefaultThreatPoints".Translate(), -1f, "maxDefaultThreatPointsDesc".Translate());
		listing_Standard.TextFieldNumeric(ref settings.maxDefaultThreatPoints, ref buffer);
		listing_Standard.Gap(5f);
		listing_Standard.Label("maxPawns".Translate(), -1f, "maxPawnsDesc".Translate());
		listing_Standard.TextFieldNumeric(ref settings.maxPawns, ref buffer2);
		listing_Standard.Gap(5f);
		listing_Standard.End();
	}

	public override string SettingsCategory()
	{
		return "NCL_Storyteller_Mod".Translate();
	}
}
