using System.Collections.Generic;
using RimWorld;

namespace AlienRace;

public class ChemicalSettings
{
	public ChemicalDef chemical;

	public bool ingestible = true;

	public List<IngestionOutcomeDoer> reactions;
}
