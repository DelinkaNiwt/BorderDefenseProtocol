using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.AI.Group;

namespace GD3
{
	public class CompSpawnThingOnDestroy : ThingComp
	{
		public CompProperties_SpawnThingOnDestroy Props
		{
			get
			{
				return (CompProperties_SpawnThingOnDestroy)this.props;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			this.map = this.parent.Map;
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			if (Props.thingDef != null)
            {
				Thing thing = ThingMaker.MakeThing(this.Props.thingDef, null);
				if (!thing.def.MadeFromStuff)
				{
					GenPlace.TryPlaceThing(thing, this.parent.Position, this.map, ThingPlaceMode.Near, null, null, default(Rot4));
				}
			}
			if (this.pawnKindDef != null)
            {
				Faction faction = Find.FactionManager.FirstFactionOfDef(this.faction);
				Pawn pawn = PawnGenerator.GeneratePawn(this.pawnKindDef, faction);
				CompBlackFaction comp = pawn.TryGetComp<CompBlackFaction>();
				if (comp != null)
                {
					comp.inMission = true;
                }
				GenPlace.TryPlaceThing(pawn, this.parent.Position, this.map, ThingPlaceMode.Near, null, null, default(Rot4));
				Lord lordInit = FindLordToJoin(pawn, destroyAllEnemies ? typeof(LordJob_AssaultColony) : typeof(LordJob_AssaultThings));
				if (lordInit == null)
                {
					LordJob lordJob;
					if (destroyAllEnemies) lordJob = new LordJob_AssaultColony(faction, false, false, false, false, false, false, false);
					else lordJob = new LordJob_AssaultThings(faction, map.listerThings.AllThings.FindAll(p => p is Pawn && p.Faction != null && p.Faction.HostileTo(faction)), 1f, false);
					lordInit = LordMaker.MakeNewLord(faction, lordJob, map);
				}
				lordInit.AddPawn(pawn);
            }
			base.PostDestroy(mode, previousMap);
		}

		public Lord FindLordToJoin(Pawn pawn, Type lordJobType)
		{
			if (pawn.Spawned && pawn.Map != null)
			{
				Predicate<Pawn> hasJob = delegate (Pawn x)
				{
					Lord lord2 = x.GetLord();
					return x.Faction == pawn.Faction && lord2 != null && lord2.LordJob.GetType() == lordJobType;
				};
				List<Pawn> list = pawn.Map.mapPawns.AllPawnsSpawned.ToList();
				Pawn foundPawn = list.Find(hasJob);
				if (foundPawn != null)
				{
					return foundPawn.GetLord();
				}
			}
			return null;
		}

		public override void PostExposeData()
        {
			Scribe_Defs.Look(ref pawnKindDef, "pawnKindDef");
			Scribe_Defs.Look(ref faction, "faction");
			Scribe_Values.Look(ref destroyAllEnemies, "destroyAllEnemies", false);
			Scribe_References.Look(ref map, "map");
        }

        public PawnKindDef pawnKindDef = null;

		public FactionDef faction = null;

		public bool destroyAllEnemies = false;

		private Map map;
	}
}
