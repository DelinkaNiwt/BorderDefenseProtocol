using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AlienRace;

public interface IAlienChannelColorGenerator
{
	List<Color> AvailableColors(Pawn pawn);

	List<ColorGenerator> AvailableGenerators(Pawn pawn);
}
