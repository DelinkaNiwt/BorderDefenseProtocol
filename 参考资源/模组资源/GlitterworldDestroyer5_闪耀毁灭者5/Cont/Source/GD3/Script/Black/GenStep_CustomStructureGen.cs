using System;
using System.Collections.Generic;
using KCSG;
using RimWorld.BaseGen;
using System.Reflection;
using Verse;
using RimWorld;
using System.Linq;
using Verse.AI.Group;
using Verse.AI;

namespace GD3
{
    public class GenStep_CustomStructureGen : GenStep
	{
		public bool fullClear = false;

		public bool clearFogInRect = false;

		public bool preventBridgeable = false;

		public List<KCSG.StructureLayoutDef> structureLayoutDefs = new List<KCSG.StructureLayoutDef>();

		public List<TiledStructureDef> tiledStructures = new List<TiledStructureDef>();

		public List<string> symbolResolvers = new List<string>();

		public List<ThingDef> scatterThings = new List<ThingDef>();

		public List<ThingDef> filthTypes = new List<ThingDef>();

		public float scatterChance = 0.4f;

		public bool scaleWithQuest = false;

		public FactionDef forcedfaction;

		public float pointMultiplier = 1f;

		public bool spawnOnEdge = false;

		public bool generateBlack = false;

		public FloatRange defaultPointsRange = new FloatRange(300f, 500f);

		public override int SeedPart => 916595355;

		public override void Generate(Map map, GenStepParams parms)
		{
			GenOption.customGenExt = new CustomGenOption
			{
				symbolResolvers = symbolResolvers,
				filthTypes = filthTypes,
				scatterThings = scatterThings,
				scatterChance = scatterChance
			};
			if (!tiledStructures.NullOrEmpty())
			{
				tiledStructures.RandomElement().Generate(map.Center, map, scaleWithQuest ? CustomGenOption.GetRelatedQuest(map) : null);
				return;
			}
			KCSG.StructureLayoutDef structureLayoutDef = structureLayoutDefs.RandomElement();
			FieldInfo value = typeof(KCSG.StructureLayoutDef).GetField("sizes", BindingFlags.Instance | BindingFlags.NonPublic);
			IntVec2 vec2 = (IntVec2)value.GetValue(structureLayoutDef);
			CellRect rect = CellRect.CenteredOn(map.Center, vec2.x, vec2.z);
			GenOption.GetAllMineableIn(rect, map);
			LayoutUtils.CleanRect(structureLayoutDef, map, rect, fullClear);
			structureLayoutDef.Generate(rect, map);
			List<string> list = GenOption.customGenExt.symbolResolvers;
			if (list != null && list.Count > 0)
			{
				Debug.Message("GenStep_CustomStructureGen - Additional symbol resolvers");
				BaseGen.symbolStack.Push("kcsg_runresolvers", new ResolveParams
				{
					faction = map.ParentFaction,
					rect = rect
				});
			}
			if (map.mapPawns.FreeColonistsSpawned.Count > 0)
			{
				FloodFillerFog.DebugRefogMap(map);
			}

			GenerateSub(map, parms);

			if (generateBlack)
            {
				GenerateBlack(map, parms);
            }

			if (!clearFogInRect)
			{
				return;
			}
			foreach (IntVec3 item in rect)
			{
				if (map.fogGrid.IsFogged(item))
				{
					map.fogGrid.Unfog(item);
				}
				else
				{
					MapGenerator.rootsToUnfog.Add(item);
				}
			}
		}

		private void GenerateSub(Map map, GenStepParams parms)
        {
			Faction faction = (forcedfaction != null) ? Find.FactionManager.FirstFactionOfDef(forcedfaction) : parms.sitePart.site.Faction;
			if (faction == null)
			{
				Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: true, TechLevel.Neolithic);
				parms.sitePart.site.SetFaction(faction);
			}
			else
			{
				parms.sitePart.site.SetFaction(faction);
			}

			Lord lord = LordMaker.MakeNewLord(faction, new LordJob_MechanoidsDefend(new List<Thing>(), faction, 24f, map.Center, false, false), map);
			List<Pawn> pawns = GeneratePawns(this, map, faction, parms).ToList();

			if (pawns.Count == 0)
            {
				Log.Error("Fortress generated 0 mechanoid pawns.");
				int i = 0;
				while (pawns.Count == 0 && i < 10)
                {
					i++;
					pawns = GeneratePawns(this, map, faction, parms).ToList();
				}
			}

			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn item = pawns[i];
				IntVec3 result;
				if (spawnOnEdge)
				{
					if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.Standable(map) && map.reachability.CanReachColony(x), map, CellFinder.EdgeRoadChance_Ignore, out result))
					{
						item.Discard();
						Log.Warning("TryFindRandomEdgeCellWith not found.");
						break;
					}
				}
				else if (!TryFindRandomSpawnCellForPawnNear(map.Center, map, out result, 4))
				{
					item.Discard();
					Log.Warning("TryFindRandomSpawnCellForPawnNear not found.");
					break;
				}

				GenSpawn.Spawn(item, result, map);
				lord.AddPawn(item);
				if (i == pawns.Count - 1)
				{
					item.inventory.TryAddItemNotForSale(ThingMaker.MakeThing(GDDefOf.GD_ServerKey));
					Log.Message("Key Spawned");
				}
			}
		}

		private IEnumerable<Pawn> GeneratePawns(GenStep_CustomStructureGen genStep, Map map, Faction faction, GenStepParams parms)
		{
			float num = genStep.defaultPointsRange.RandomInRange;

			num = Math.Max(num, 150f) * genStep.pointMultiplier;
			Debug.Message($"Final threat points: {num}");
			return PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
			{
				groupKind = PawnGroupKindDefOf.Combat,
				tile = map.Tile,
				faction = faction,
				points = num
			});
		}

		private bool TryFindRandomSpawnCellForPawnNear(IntVec3 root, Map map, out IntVec3 result, int firstTryWithRadius = 4, Predicate<IntVec3> extraValidator = null)
		{
			if (root.Standable(map) && root.GetFirstPawn(map) == null)
			{
				result = root;
				return true;
			}

			int num = firstTryWithRadius;
			for (int i = 0; i < 3; i++)
			{
				if (CellFinder.TryFindRandomReachableNearbyCell(root, map, num, TraverseParms.For(TraverseMode.PassDoors), (IntVec3 c) => c.Standable(map) && c.GetFirstPawn(map) == null && (extraValidator == null || extraValidator(c)), null, out result))
				{
					return true;
				}

				num *= 2;
			}

			num = firstTryWithRadius + 1;
			while (true)
			{
				if (CellFinder.TryRandomClosewalkCellNear(root, map, num, out result))
				{
					return true;
				}

				if (num > map.Size.x / 2 && num > map.Size.z / 2)
				{
					break;
				}

				num *= 2;
			}

			result = root;
			return false;
		}

		private void GenerateBlack(Map map, GenStepParams parms)
		{
			Faction faction = Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid);

			List<Building> buildings = map.listerBuildings.allBuildingsNonColonist.FindAll(b => b is Building_TurretGun && b.Position.GetTerrain(map).natural && !b.Fogged());
			List<Thing> list = buildings.OfType<Thing>().ToList();
			Lord lord = LordMaker.MakeNewLord(faction, new LordJob_AssaultThings(faction, list, 1, false), map);

			Pawn tesseron = PawnGenerator.GeneratePawn(new PawnGenerationRequest(GDDefOf.Mech_BlackTesseron, faction));
			CompBlackFaction comp = tesseron.TryGetComp<CompBlackFaction>();
			comp.inMission = true;
			Pawn Legionary = PawnGenerator.GeneratePawn(new PawnGenerationRequest(GDDefOf.Mech_BlackLegionary, faction));
			comp = Legionary.TryGetComp<CompBlackFaction>();
			comp.inMission = true;

			List<Pawn> pawns = new List<Pawn>() { tesseron , Legionary };

			CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.Standable(map) && map.reachability.CanReach(x, map.Center, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassAllDestroyableThings)), map, CellFinder.EdgeRoadChance_Ignore, out IntVec3 result);

			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn item = pawns[i];
				GenSpawn.Spawn(item, result, map);
				lord.AddPawn(item);
			}
		}
	}
}
