using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class CompAbilityEffect_Excalibur : CompAbilityEffect
{
	private List<IntVec3> tmpCells = new List<IntVec3>();

	private Pawn Pawn => parent.pawn;

	private ThingWithComps weapon => Pawn.equipment.Primary;

	private QualityCategory quality => weapon.TryGetComp<CompQuality>().Quality;

	private float damageAmountBase => weapon.def.tools.First().power;

	private float armorPenetrationBase => weapon.def.tools.First().armorPenetration;

	private float damageAmount => AncotUtility.QualityFactor(quality) * damageAmountBase;

	private float armorPenetration => AncotUtility.QualityFactor(quality) * armorPenetrationBase;

	public CompWeaponCharge compCharge => ((Thing)weapon).TryGetComp<CompWeaponCharge>();

	private new CompProperties_AbilityExcalibur Props => (CompProperties_AbilityExcalibur)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (compCharge != null && !compCharge.CanBeUsed)
		{
			return;
		}
		CompWeaponCharge obj = compCharge;
		if (obj != null)
		{
			obj.UsedOnce();
		}
		IntVec3 position = parent.pawn.Position;
		IntVec3 cell = target.Cell;
		Vector3 v = (cell - position).ToVector3();
		v.Normalize();
		float num = v.AngleFlat();
		Map map = Pawn.Map;
		List<IntVec3> list = AffectedCells(target);
		foreach (IntVec3 item in list)
		{
			GenExplosion.DoExplosion(item, map, 0.9f, MiliraDefOf.Milira_PlasmaBomb_TwoHandSword, Pawn, 3 * (int)damageAmount, 2f * armorPenetration, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, null, doVisualEffects: true, 1f, 0f, doSoundEffects: true, null, 4f);
		}
		parent.AddEffecterToMaintain(MiliraDefOf.Milira_TwoHandSwordFire.Spawn(parent.pawn.Position, cell, parent.pawn.Map), Pawn.Position, cell, 50, Pawn.MapHeld);
		base.Apply(target, dest);
	}

	public override IEnumerable<PreCastAction> GetPreCastActions()
	{
		yield return new PreCastAction
		{
			action = delegate
			{
				parent.AddEffecterToMaintain(MiliraDefOf.Milira_TwoHandSwordWarmup.Spawn(Pawn, Pawn.Map), Pawn.Position, 480, Pawn.Map);
			},
			ticksAwayFromCast = 600
		};
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		GenDraw.DrawFieldEdges(AffectedCells(target));
	}

	private List<IntVec3> AffectedCells(LocalTargetInfo target)
	{
		tmpCells.Clear();
		Vector3 vector = Pawn.Position.ToVector3Shifted().Yto0();
		IntVec3 intVec = TargetPosition(Pawn, target).ClampInsideMap(Pawn.Map);
		if (Pawn.Position == intVec)
		{
			return tmpCells;
		}
		float lengthHorizontal = (intVec - Pawn.Position).LengthHorizontal;
		List<IntVec3> list = GenSight.BresenhamCellsBetween(Pawn.Position, intVec);
		for (int i = 1; i < list.Count; i++)
		{
			IntVec3 intVec2 = list[i];
			tmpCells.AddRange(GenRadial.RadialCellsAround(intVec2, 1f, useCenter: true));
			if (!tmpCells.Contains(intVec2) && CanUseCell(intVec2))
			{
				tmpCells.Add(intVec2);
			}
		}
		tmpCells = tmpCells.Distinct().ToList();
		tmpCells.RemoveAll((IntVec3 cell) => !CanUseCell(cell));
		return tmpCells;
		bool CanUseCell(IntVec3 c)
		{
			if (!c.InBounds(Pawn.Map))
			{
				return false;
			}
			if (c == Pawn.Position)
			{
				return false;
			}
			if (!c.InHorDistOf(Pawn.Position, Props.distance))
			{
				return false;
			}
			return true;
		}
	}

	public IntVec3 TargetPosition(Pawn pawn, LocalTargetInfo currentTarget)
	{
		IntVec3 position = pawn.Position;
		IntVec3 cell = currentTarget.Cell;
		Vector3 vector = (cell - position).ToVector3();
		vector.Normalize();
		return position + (Props.distance * vector).ToIntVec3();
	}
}
