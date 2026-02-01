using UnityEngine;
using Verse;

namespace NCL;

public class CompProperties_AntiInvisibilityField : CompProperties
{
	public float effectiveRadius = 12f;

	public int checkIntervalTicks = 60;

	public bool affectsThroughWalls = false;

	public bool affectsAllFactions = false;

	public HediffDef applyHediff;

	public SoundDef soundOnReveal;

	public EffecterDef instantEffecterDef;

	public EffecterDef continuousEffecterDef;

	public bool drawRadius = true;

	public bool drawLines = true;

	public Color radiusColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

	public bool requiresPower = true;

	public bool startActivated = true;

	public string toggleCommandLabel = "Toggle Scanner";

	public string toggleCommandDesc = "Enable/disable invisibility detection";

	public CompProperties_AntiInvisibilityField()
	{
		compClass = typeof(CompAntiInvisibilityField);
	}
}
