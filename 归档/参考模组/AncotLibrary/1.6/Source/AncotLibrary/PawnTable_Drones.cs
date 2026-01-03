using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class PawnTable_Drones : PawnTable
{
	public PawnTable_Drones(PawnTableDef def, Func<IEnumerable<Pawn>> pawnsGetter, int uiWidth, int uiHeight)
		: base(def, pawnsGetter, uiWidth, uiHeight)
	{
	}
}
