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
	public class GenStep_CustomStructureGen_Drysea : GenStep
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

			IntVec3 vec3 = map.Center;
			List<IntVec3> vecs = map.AllCells.ToList();
			for (int i = 0; i < vecs.Count; i++)
			{
				IntVec3 ivec = vecs[i];
				map.roofGrid.SetRoof(ivec, null);
				if (ivec.GetFirstMineable(map) != null)
				{
					ivec.GetFirstMineable(map).Destroy(DestroyMode.Vanish);
				}
			}
			if (vec3.GetFirstThing(map, GDDefOf.Plant_PeaceLily) == null)
            {
				GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.Plant_PeaceLily), vec3, map, ThingPlaceMode.Direct);
			}
			Plant plant = (Plant)vec3.GetFirstThing(map, GDDefOf.Plant_PeaceLily);
			int num = (int)((1f - plant.Growth) * plant.def.plant.growDays);
			plant.Age += num;
			plant.Growth = 1f;

			if (map.mapPawns.FreeColonistsSpawned.Count > 0)
			{
				FloodFillerFog.DebugRefogMap(map);
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
	}
}