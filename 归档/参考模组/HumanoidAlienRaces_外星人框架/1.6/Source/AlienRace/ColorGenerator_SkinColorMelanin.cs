using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

public class ColorGenerator_SkinColorMelanin : ColorGenerator
{
	public float minMelanin;

	public float maxMelanin = 1f;

	public bool naturalMelanin;

	public override Color NewRandomizedColor()
	{
		return PawnSkinColors.GetSkinColor(Rand.Range(minMelanin, maxMelanin));
	}
}
