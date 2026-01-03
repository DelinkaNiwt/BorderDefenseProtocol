using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class MainTabWindow_Drones : MainTabWindow_PawnTable
{
	protected override PawnTableDef PawnTableDef => AncotDefOf.Ancot_Drones;

	protected override float ExtraTopSpace => 35f;

	protected override IEnumerable<Pawn> Pawns => from p in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
		where p.RaceProps.IsMechanoid && p.TryGetComp<CompDrone>() != null
		select p;
}
