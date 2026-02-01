using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Skipmaster;

public class Ability_Wallraise : Ability
{
	public AbilityExtension_Wallraise Props => ((Def)(object)base.def).GetModExtension<AbilityExtension_Wallraise>();

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			Map map = globalTargetInfo.Map;
			LocalTargetInfo target = (globalTargetInfo.HasThing ? new LocalTargetInfo(globalTargetInfo.Thing) : new LocalTargetInfo(globalTargetInfo.Cell));
			List<Thing> list = new List<Thing>();
			list.AddRange(Props.AffectedCells(target, map).SelectMany((IntVec3 c) => from t in c.GetThingList(map)
				where t.def.category == ThingCategory.Item
				select t));
			foreach (Thing item in list)
			{
				item.DeSpawn();
			}
			foreach (IntVec3 item2 in Props.AffectedCells(target, map))
			{
				GenSpawn.Spawn(ThingDefOf.RaisedRocks, item2, map);
				FleckMaker.ThrowDustPuffThick(item2.ToVector3Shifted(), map, Rand.Range(1.5f, 3f), CompAbilityEffect_Wallraise.DustColor);
			}
			foreach (Thing item3 in list)
			{
				IntVec3 intVec = IntVec3.Invalid;
				for (int num = 0; num < 9; num++)
				{
					IntVec3 intVec2 = item3.Position + GenRadial.RadialPattern[num];
					if (intVec2.InBounds(map) && intVec2.Walkable(map) && map.thingGrid.ThingsListAtFast(intVec2).Count <= 0)
					{
						intVec = intVec2;
						break;
					}
				}
				if (intVec != IntVec3.Invalid)
				{
					GenSpawn.Spawn(item3, intVec, map);
				}
				else
				{
					GenPlace.TryPlaceThing(item3, item3.Position, map, ThingPlaceMode.Near);
				}
			}
		}
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		((Ability)this).DrawHighlight(target);
		GenDraw.DrawFieldEdges(Props.AffectedCells(target, base.pawn.Map).ToList(), ((Ability)this).ValidateTarget(target, false) ? Color.white : Color.red);
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = false)
	{
		if (Props.AffectedCells(target, base.pawn.Map).Any((IntVec3 c) => c.Filled(base.pawn.Map)))
		{
			if (showMessages)
			{
				Messages.Message("AbilityOccupiedCells".Translate(((Def)(object)base.def).LabelCap), target.ToTargetInfo(base.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if (Props.AffectedCells(target, base.pawn.Map).Any((IntVec3 c) => !c.Standable(base.pawn.Map)))
		{
			if (showMessages)
			{
				Messages.Message("AbilityUnwalkable".Translate(((Def)(object)base.def).LabelCap), target.ToTargetInfo(base.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}
}
