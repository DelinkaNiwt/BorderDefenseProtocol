using UnityEngine;
using Verse;

namespace TurbojetBackpack;

public class CompProperties_TurbojetShield : CompProperties
{
	public float maxEnergy = 100f;

	public float energyRegenRate = 0.15f;

	public float energyLossPerDamage = 0.5f;

	public int resetDelayTicks = 1200;

	public float maxDamageCap = 999f;

	public float minDamageThreshold = 0f;

	public float minDrawSize = 1.2f;

	public float maxDrawSize = 1.55f;

	public string shieldTexPath = "Things/Mote/ShieldBubble";

	public Color shieldColor = new Color(0.2f, 0.9f, 1f);

	public CompProperties_TurbojetShield()
	{
		compClass = typeof(CompTurbojetShield);
	}
}
