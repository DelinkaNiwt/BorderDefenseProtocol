using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class CompProperties_FleckEmitter : CompProperties
{
	public FleckDef fleck;

	public float scale = 1f;

	public Vector3 offset;

	public int emissionInterval = -1;

	public SoundDef soundOnEmission;

	public string saveKeysPrefix;

	public CompProperties_FleckEmitter()
	{
		compClass = typeof(CompFleckEmitter);
	}

	public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
	{
		if (fleck == null)
		{
			yield return "CompFleckEmitter must have a fleck assigned.";
		}
	}
}
