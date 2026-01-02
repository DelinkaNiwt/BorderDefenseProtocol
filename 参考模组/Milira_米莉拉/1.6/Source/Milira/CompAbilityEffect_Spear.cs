using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class CompAbilityEffect_Spear : CompAbilityEffect
{
	private List<IntVec3> tmpCells = new List<IntVec3>();

	private Pawn Pawn => parent.pawn;

	private ThingWithComps weapon => Pawn.equipment.Primary;

	public CompWeaponCharge compCharge => ((Thing)weapon).TryGetComp<CompWeaponCharge>();

	public new CompProperties_AbilitySpear Props => (CompProperties_AbilitySpear)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (compCharge == null || compCharge.CanBeUsed)
		{
			CompWeaponCharge obj = compCharge;
			if (obj != null)
			{
				obj.UsedOnce();
			}
			base.Apply(target, dest);
			((Projectile)GenSpawn.Spawn(Props.projectileDef, Pawn.Position, Pawn.Map)).Launch(Pawn, Pawn.DrawPos, target, target, ProjectileHitFlags.IntendedTarget);
			if (Props.sprayEffecter != null)
			{
				Props.sprayEffecter.Spawn(parent.pawn.Position, target.Cell, parent.pawn.Map).Cleanup();
			}
		}
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		GenDraw.DrawFieldEdges(AffectedCells(target));
	}

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Pawn.Faction != null)
		{
			foreach (IntVec3 item in AffectedCells(target))
			{
				List<Thing> thingList = item.GetThingList(Pawn.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i].Faction == Pawn.Faction)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private List<IntVec3> AffectedCells(LocalTargetInfo target)
	{
		tmpCells.Clear();
		IntVec3 position = Pawn.Position;
		IntVec3 cell = target.Cell;
		Vector3 v = (cell - position).ToVector3();
		v.Normalize();
		float num = v.ToAngleFlat();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target.Cell, Props.radius, useCenter: true))
		{
			float num2 = (item - cell).ToVector3().normalized.ToAngleFlat();
			if (num - 30f < -180f || num + 30f > 180f)
			{
				if (num < 0f)
				{
					if ((num2 >= -180f && num2 <= num + 30f) || (num2 >= 360f + num - 30f && num2 <= 180f))
					{
						tmpCells.Add(item);
					}
				}
				else if ((num2 >= num - 30f && num2 <= 180f) || (num2 >= -180f && num2 <= -360f + num + 30f))
				{
					tmpCells.Add(item);
				}
			}
			else if (num2 >= num - 30f && num2 <= num + 30f)
			{
				tmpCells.Add(item);
			}
		}
		tmpCells.AddRange(GenRadial.RadialCellsAround(target.Cell, 1.5f, useCenter: true));
		tmpCells = tmpCells.Distinct().ToList();
		tmpCells.RemoveAll((IntVec3 c) => !CanUseCell(c));
		return tmpCells;
		bool CanUseCell(IntVec3 c)
		{
			if (!c.InBounds(Pawn.Map))
			{
				return false;
			}
			if (!GenSight.LineOfSight(target.Cell, c, Pawn.Map, skipFirstCell: true))
			{
				return false;
			}
			return true;
		}
	}
}
