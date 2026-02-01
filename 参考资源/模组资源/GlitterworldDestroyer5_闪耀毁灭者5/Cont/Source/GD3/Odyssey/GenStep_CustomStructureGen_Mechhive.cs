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
	public class GenStep_CustomStructureGen_Mechhive : GenStep_CustomStructureGen_WithMusic
	{
		public override int SeedPart => 433925312;

		public override void Generate(Map map, GenStepParams parms)
		{
			forcedfaction = FactionDefOf.Mechanoid;

			base.Generate(map, parms);

			List<Building> buildings = map.listerBuildings.allBuildingsNonColonist;
			foreach (Building building in buildings)
			{
				if (building.def == ThingDefOf.AnticraftBeam)
				{
					IntVec3 pos = building.Position;
					if (pos.x > map.Center.x && pos.z < map.Center.z) building.Rotation = building.Rotation.Rotated(RotationDirection.Clockwise);
					else if (pos.x < map.Center.x && pos.z > map.Center.z) building.Rotation = building.Rotation.Rotated(RotationDirection.Counterclockwise);
					else if (pos.x < map.Center.x && pos.z < map.Center.z) building.Rotation = building.Rotation.Rotated(RotationDirection.Clockwise).Rotated(RotationDirection.Clockwise);
					continue;
				}
				CompMechGestatorTank comp = building.TryGetComp<CompMechGestatorTank>();
				if (comp != null && Rand.Chance(0.4f))
				{
					comp.State = CompMechGestatorTank.TankState.Proximity;
				}
			}
		}
    }
}

