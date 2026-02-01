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
	public class GenStep_CustomStructureGen_Platform : GenStep_CustomStructureGen_WithMusic
	{
		public override int SeedPart => 433925312;

		public override void Generate(Map map, GenStepParams parms)
		{
			forcedfaction = FactionDefOf.Mechanoid;

			base.Generate(map, parms);

			List<Building> buildings = map.listerBuildings.allBuildingsNonColonist;
			foreach (Building building in buildings)
			{
				CompMechGestatorTank comp = building.TryGetComp<CompMechGestatorTank>();
				if (comp != null && Rand.Chance(0.4f))
				{
					comp.State = CompMechGestatorTank.TankState.Proximity;
				}
			}
		}
	}
}
