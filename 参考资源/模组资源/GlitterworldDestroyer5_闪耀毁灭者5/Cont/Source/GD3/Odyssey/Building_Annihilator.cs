using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public class Building_Annihilator : Building
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Pawn pawn = PawnGenerator.GeneratePawn(GDDefOf_Another.Mech_Annihilator);
            pawn.ageTracker.AgeBiologicalTicks = 0;
            GenSpawn.Spawn(pawn, PositionHeld, map);
            pawn.SetFaction(Faction.OfPlayer);
            Destroy();
        }
    }
}
