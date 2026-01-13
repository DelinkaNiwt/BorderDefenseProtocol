using System.Collections.Generic;
using Verse;

namespace TurbojetBackpack;

public class TurbojetExtension : DefModExtension
{
	public float landingDamageRadius = 3.9f;

	public float pushDistance = 10f;

	public float collisionScanRadius = 1.5f;

	public int damageAmount = 12;

	public int stunAmount = 20;

	public SoundDef landingSound;

	public EffecterDef landingEffecter;

	public ThingDef flightMote;

	public ThingDef standbyMote;

	public int standbyMoteInterval = 15;

	public FloatRange standbyMoteScale = new FloatRange(0.6f, 0.9f);

	public ThingDef shadowMote;

	public int shadowMoteInterval = 5;

	public string flightModeIconPath;

	public string combatModeIconPath;

	public float flightMoveSpeed = 6f;

	public float worldMapSpeedFactor = 1f;

	public float flightHeight = 1.2f;

	public float takeoffSpeed = 0.08f;

	public float landingSpeed = 0.05f;

	public float hoverAmplitude = 0.15f;

	public float hoverFrequency = 2f;

	public float flightSmokeScaleMin = 1.2f;

	public float flightSmokeScaleMax = 3.6f;

	public List<MoteSettings> thrustMoteEffects;

	public List<MoteSettings> jumpMoteEffects;

	public List<JetDef> jetsVertical;

	public List<JetDef> jetsHorizontal;
}
