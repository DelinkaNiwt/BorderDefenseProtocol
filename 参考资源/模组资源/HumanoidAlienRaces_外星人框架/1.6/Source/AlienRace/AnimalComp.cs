using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AlienRace;

public class AnimalComp : ThingComp
{
	public List<Graphic> addonGraphics;

	public List<int> addonVariants;

	public Vector2 customDrawSize = Vector2.one;

	public Vector2 customHeadDrawSize = Vector2.one;

	public Vector2 customPortraitDrawSize = Vector2.one;

	public Vector2 customPortraitHeadDrawSize = Vector2.one;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Collections.Look(ref addonVariants, "addonVariants", LookMode.Undefined);
	}
}
