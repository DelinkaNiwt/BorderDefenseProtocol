using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Skipmaster;

public class Ability_Waterskip : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		Map map = targets[0].Map;
		foreach (IntVec3 item in AffectedCells(targets[0].Cell, map))
		{
			List<Thing> thingList = item.GetThingList(map);
			for (int num = thingList.Count - 1; num >= 0; num--)
			{
				Thing thing = thingList[num];
				if (!(thing is Filth) && !(thing is Fire))
				{
					if (thing is ThingWithComps thingWithComps)
					{
						if (thingWithComps.TryGetComp<CompPower>() == null)
						{
							if (thing is Pawn pawn)
							{
								pawn.GetInvisibilityComp()?.DisruptInvisibility();
							}
						}
						else
						{
							thingWithComps.TryGetComp<CompBreakdownable>()?.DoBreakdown();
							CompFlickable compFlickable = thingWithComps.TryGetComp<CompFlickable>();
							if (compFlickable != null)
							{
								compFlickable.SwitchIsOn = false;
							}
							if (thingWithComps.TryGetComp<CompProjectileInterceptor>() != null || thingWithComps is Building_Turret)
							{
								thingWithComps.TakeDamage(new DamageInfo(DamageDefOf.EMP, 10f, 10f, -1f, base.pawn));
							}
						}
					}
				}
				else
				{
					thingList[num].Destroy();
				}
			}
			if (!item.Filled(map))
			{
				FilthMaker.TryMakeFilth(item, map, ThingDefOf.Filth_Water);
			}
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(item.ToVector3Shifted(), map, FleckDefOf.WaterskipSplashParticles);
			dataStatic.rotationRate = Rand.Range(-30, 30);
			dataStatic.rotation = 90 * Rand.RangeInclusive(0, 3);
			map.flecks.CreateFleck(dataStatic);
		}
	}

	private IEnumerable<IntVec3> AffectedCells(IntVec3 cell, Map map)
	{
		if (cell.Filled(base.pawn.Map))
		{
			yield break;
		}
		foreach (IntVec3 item in GenRadial.RadialCellsAround(cell, ((Ability)this).GetRadiusForPawn(), useCenter: true))
		{
			if (item.InBounds(map) && GenSight.LineOfSightToEdges(cell, item, map, skipFirstCell: true))
			{
				yield return item;
			}
		}
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		float rangeForPawn = ((Ability)this).GetRangeForPawn();
		if (GenRadial.MaxRadialPatternRadius > rangeForPawn && rangeForPawn >= 1f)
		{
			GenDraw.DrawRadiusRing(base.pawn.Position, rangeForPawn, Color.cyan);
		}
		if (target.IsValid)
		{
			GenDraw.DrawTargetHighlight(target);
			GenDraw.DrawFieldEdges(AffectedCells(target.Cell, base.pawn.Map).ToList(), ((Ability)this).ValidateTarget(target, false) ? Color.white : Color.red);
		}
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (target.Cell.Filled(base.pawn.Map))
		{
			if (showMessages)
			{
				Messages.Message("AbilityOccupiedCells".Translate(((Def)(object)base.def).LabelCap), target.ToTargetInfo(base.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return ((Ability)this).ValidateTarget(target, showMessages);
	}
}
