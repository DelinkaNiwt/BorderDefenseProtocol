using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AlienRace;

public class ChannelColorGenerator_GenderBased : ChannelColorGenerator_PawnBased
{
	public Dictionary<Gender, ColorGenerator> colors = new Dictionary<Gender, ColorGenerator>();

	public override Color NewRandomizedColor(Pawn pawn)
	{
		return GetGenerator(pawn.gender).NewRandomizedColor();
	}

	public override List<Color> AvailableColors(Pawn pawn)
	{
		return new List<Color>();
	}

	public override List<ColorGenerator> AvailableGenerators(Pawn pawn)
	{
		return new List<ColorGenerator>(1) { GetGenerator(pawn.gender) };
	}

	public ColorGenerator GetGenerator(Gender gender)
	{
		if (!colors.ContainsKey(gender))
		{
			gender = colors.Keys.RandomElement();
		}
		return colors[gender];
	}
}
