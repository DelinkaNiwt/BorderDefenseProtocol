using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AlienRace;

public abstract class ChannelColorGenerator_PawnBased : ColorGenerator, IAlienChannelColorGenerator
{
	public override Color NewRandomizedColor()
	{
		return Color.clear;
	}

	public abstract Color NewRandomizedColor(Pawn pawn);

	public virtual List<Color> AvailableColors(Pawn pawn)
	{
		return new List<Color>();
	}

	public virtual List<ColorGenerator> AvailableGenerators(Pawn pawn)
	{
		return new List<ColorGenerator>();
	}
}
