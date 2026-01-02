using System.Collections.Generic;
using Verse;

namespace AlienRace;

[StaticConstructorOnStartup]
public class RaceSettings : Def
{
	public PawnKindSettings pawnKindSettings = new PawnKindSettings();

	public List<AlienPartGenerator.BodyAddon> universalBodyAddons = new List<AlienPartGenerator.BodyAddon>();
}
