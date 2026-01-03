using System.Collections.Generic;

namespace AlienRace;

public class PawnKindSettings
{
	public List<PawnKindEntry> alienslavekinds = new List<PawnKindEntry>();

	public List<PawnKindEntry> alienrefugeekinds = new List<PawnKindEntry>();

	public List<FactionPawnKindEntry> startingColonists = new List<FactionPawnKindEntry>();

	public List<FactionPawnKindEntry> alienwandererkinds = new List<FactionPawnKindEntry>();
}
