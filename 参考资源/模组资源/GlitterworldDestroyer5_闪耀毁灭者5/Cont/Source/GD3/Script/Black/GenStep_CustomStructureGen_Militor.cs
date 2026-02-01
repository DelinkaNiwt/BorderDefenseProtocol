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
	public class GenStep_CustomStructureGen_Militor : GenStep
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

			//GenerateSub(map, parms);

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

			Lord lord = LordMaker.MakeNewLord(faction, new LordJob_MechanoidsDefend(new List<Thing>(), faction, 12f, map.Center, false, false), map);
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(GDDefOf.Mech_Militor, faction));
			List<Pawn> pawns = new List<Pawn>() { pawn };

			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn item = pawns[i];
				IntVec3 result;
				if (!TryFindRandomSpawnCellForPawnNear(map.Center, map, out result, 2))
				{
					item.Discard();
					Log.Warning("TryFindRandomSpawnCellForPawnNear not found.");
					break;
				}

				GenSpawn.Spawn(item, result, map);
				lord.AddPawn(item);
			}
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
	}
}
