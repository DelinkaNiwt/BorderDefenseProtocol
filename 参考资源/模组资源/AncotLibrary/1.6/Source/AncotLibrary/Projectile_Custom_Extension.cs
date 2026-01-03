using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Projectile_Custom_Extension : DefModExtension
{
	public EffecterDef impactEffecter;

	public FleckDef trailFleck;

	public int trailFreauency = 1;

	public bool fixedTrailRotation = false;

	public Color trailColor = new Color(1f, 1f, 1f);
}
