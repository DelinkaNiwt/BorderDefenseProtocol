using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CharacterEditor;

public class GenStep_ScatterThings2 : GenStep_ScatterThings
{
	public ThingStyleDef styleDef;

	private List<Rot4> possibleRotationsInt2;

	private static List<Rot4> tmpRotations2 = new List<Rot4>();

	private List<Rot4> PossibleRotations2
	{
		get
		{
			if (possibleRotationsInt2 == null)
			{
				possibleRotationsInt2 = new List<Rot4>();
				if (thingDef.rotatable)
				{
					possibleRotationsInt2.Add(Rot4.North);
					possibleRotationsInt2.Add(Rot4.East);
					possibleRotationsInt2.Add(Rot4.South);
					possibleRotationsInt2.Add(Rot4.West);
				}
				else
				{
					possibleRotationsInt2.Add(Rot4.North);
				}
			}
			return possibleRotationsInt2;
		}
	}

	public bool TryGetRandomValidRotation2(IntVec3 loc, Map map, out Rot4 rot)
	{
		List<Rot4> possibleRotations = PossibleRotations2;
		for (int i = 0; i < possibleRotations.Count; i++)
		{
			if (IsRotationValid2(loc, possibleRotations[i], map))
			{
				tmpRotations2.Add(possibleRotations[i]);
			}
		}
		if (tmpRotations2.TryRandomElement(out rot))
		{
			tmpRotations2.Clear();
			return true;
		}
		rot = Rot4.Invalid;
		return false;
	}

	private bool IsRotationValid2(IntVec3 loc, Rot4 rot, Map map)
	{
		if (!GenAdj.OccupiedRect(loc, rot, thingDef.size).InBounds(map))
		{
			return false;
		}
		if (GenSpawn.WouldWipeAnythingWith(loc, rot, thingDef, map, (Thing x) => x.def == thingDef || (x.def.category != Verse.ThingCategory.Plant && x.def.category != Verse.ThingCategory.Filth)))
		{
			return false;
		}
		return true;
	}

	protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int stackCount = 1)
	{
		if (!TryGetRandomValidRotation2(loc, map, out var rot))
		{
			Log.Warning("Could not find any valid rotation for " + thingDef);
			return;
		}
		if (clearSpaceSize > 0)
		{
			foreach (IntVec3 item in GridShapeMaker.IrregularLump(loc, map, clearSpaceSize))
			{
				item.GetEdifice(map)?.Destroy();
			}
		}
		Thing thing = ThingMaker.MakeThing(thingDef, stuff);
		thing.StyleDef = styleDef;
		if (thingDef.Minifiable)
		{
			thing = thing.MakeMinified();
		}
		if (thing.def.category == Verse.ThingCategory.Item)
		{
			thing.stackCount = stackCount;
			thing.SetForbidden(value: true, warnOnFail: false);
			GenPlace.TryPlaceThing(thing, loc, map, ThingPlaceMode.Near, out var lastResultingThing);
			if (nearPlayerStart && lastResultingThing != null && lastResultingThing.def.category == Verse.ThingCategory.Item && TutorSystem.TutorialMode)
			{
				Find.TutorialState.AddStartingItem(lastResultingThing);
			}
		}
		else
		{
			GenSpawn.Spawn(thing, loc, map, rot);
		}
		if (filthDef == null)
		{
			return;
		}
		foreach (IntVec3 item2 in thing.OccupiedRect().ExpandedBy(filthExpandBy))
		{
			if (Rand.Chance(filthChance) && item2.InBounds(thing.Map))
			{
				FilthMaker.TryMakeFilth(item2, thing.Map, filthDef);
			}
		}
	}
}
