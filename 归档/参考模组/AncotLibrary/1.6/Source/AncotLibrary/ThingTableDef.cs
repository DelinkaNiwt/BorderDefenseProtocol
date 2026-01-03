using System;
using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class ThingTableDef : Def
{
	public List<ThingColumnDef> columns;

	public Type workerClass = typeof(ThingTable);

	public int minWidth = 998;
}
