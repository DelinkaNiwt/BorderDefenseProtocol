using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class Ability_MaturePlants : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (Plant item in targets.SelectMany((GlobalTargetInfo target) => GenRadial.RadialDistinctThingsAround(target.Cell, target.Map, ((Ability)this).GetRadiusForPawn(), useCenter: true)).OfType<Plant>().Distinct())
		{
			item.Growth += item.GrowthRate * (3.5f / item.def.plant.growDays);
			item.DirtyMapMesh(item.Map);
		}
	}
}
