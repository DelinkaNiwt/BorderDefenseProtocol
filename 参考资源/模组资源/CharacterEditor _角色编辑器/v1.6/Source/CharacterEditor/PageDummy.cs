using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class PageDummy : Page
{
	public override void DoWindowContents(Rect inRect)
	{
		Current.Game.InitData.startingAndOptionalPawns.Add(PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer));
		Current.Game.InitData.startingPawnCount = 1;
		Close();
	}
}
