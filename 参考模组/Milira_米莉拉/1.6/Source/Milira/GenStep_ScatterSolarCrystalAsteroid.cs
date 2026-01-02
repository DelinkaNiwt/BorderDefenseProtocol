using RimWorld;
using Verse;

namespace Milira;

public class GenStep_ScatterSolarCrystalAsteroid : GenStep_ScatterThings
{
	public IntRange crystalGroup = new IntRange(10, 20);

	public IntRange crystalInGroup = new IntRange(2, 5);

	public override int SeedPart => 1158126098;

	protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int stackCount = 1)
	{
		if (clearSpaceSize > 0)
		{
			foreach (IntVec3 item in GridShapeMaker.IrregularLump(loc, map, clearSpaceSize))
			{
				item.GetEdifice(map)?.Destroy();
			}
		}
		int randomInRange = crystalInGroup.RandomInRange;
		foreach (IntVec3 item2 in GridShapeMaker.IrregularLump(loc, map, randomInRange))
		{
			if (item2.InBounds(map) && loc.IsValid)
			{
				Thing newThing = GenerateThing();
				GenSpawn.Spawn(newThing, item2, map);
			}
		}
	}

	public override void Generate(Map map, GenStepParams parms)
	{
		count = crystalGroup.RandomInRange;
		thingDef = MiliraDefOf.Milira_SolarCrystalDruse;
		base.Generate(map, parms);
	}
}
