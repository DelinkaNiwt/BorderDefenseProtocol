using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_RepulsiveTrap : CompProperties
{
	public float range;

	public float distance;

	public string signalTag;

	public float fieldStrength = 1f;

	public bool onlyTargetHostile;

	public bool explodeOnKilled;

	public bool doVisualEffects = true;

	public EffecterDef explosionEffect;

	public SoundDef explosionSound;

	public List<DamageDef> startWickOnDamageTaken;

	public float startWickHitPointsPercent = 0.2f;

	public IntRange wickTicks = new IntRange(140, 150);

	public float wickScale = 1f;

	public List<DamageDef> startWickOnInternalDamageTaken;

	public bool drawWick = true;

	public float chanceNeverExplodeFromDamage;

	public float destroyThingOnExplosionSize;

	public DamageDef requiredDamageTypeToExplode;

	public IntRange? countdownTicks;

	public string extraInspectStringKey;

	public List<WickMessage> wickMessages;

	public List<HediffDef> removeHediffsAffected;

	public CompProperties_RepulsiveTrap()
	{
		compClass = typeof(CompRepulsiveTrap);
	}

	public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
	{
		foreach (string item in base.ConfigErrors(parentDef))
		{
			yield return item;
		}
		if (parentDef.tickerType != TickerType.Normal)
		{
			yield return "CompRepulsiveTrap requires Normal ticker type";
		}
	}
}
