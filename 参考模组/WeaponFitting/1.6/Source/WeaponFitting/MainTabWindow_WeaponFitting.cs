using System.Collections.Generic;
using AncotLibrary;
using Verse;

namespace WeaponFitting;

public class MainTabWindow_WeaponFitting : MainTabWindow_ThingTable
{
	private GameComponent_AncotLibrary GC => GameComponent_AncotLibrary.GC;

	protected override IEnumerable<Thing> Things
	{
		get
		{
			GC.RefreshCached();
			GC.RefreshStarCached();
			return GC.AllWeapons;
		}
	}

	protected override ThingTableDef ThingTableDef => WF_DefOf.FC_WeaponFitting;
}
