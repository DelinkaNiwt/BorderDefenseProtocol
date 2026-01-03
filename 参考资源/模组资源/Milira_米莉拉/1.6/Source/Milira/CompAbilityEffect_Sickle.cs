using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class CompAbilityEffect_Sickle : CompAbilityEffect
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

	private new CompProperties_AbilitySickle Props => (CompProperties_AbilitySickle)props;

	public override IEnumerable<PreCastAction> GetPreCastActions()
	{
		yield return new PreCastAction
		{
			action = delegate
			{
				parent.AddEffecterToMaintain(MiliraDefOf.Milira_SicklePreCast.Spawn(Pawn, Pawn.Map), Pawn.Position, 10, Pawn.Map);
			},
			ticksAwayFromCast = 10
		};
	}

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
		List<Thing> list = new List<Thing>();
		IntVec3 position = Pawn.Position;
		Map map = Pawn.Map;
		foreach (IntVec3 item in AffectedCells(position, Props.radius))
		{
			list.AddRange(item.GetThingList(Pawn.Map));
		}
		list.AddRange(position.GetThingList(map));
		for (int i = 0; i < list.Count; i++)
		{
			if (!(list[i] is Pawn))
			{
				continue;
			}
			Pawn pawn = list[i] as Pawn;
			if (pawn.Faction != Pawn.Faction && !pawn.Downed)
			{
				AncotUtility.DoDamage((Thing)pawn, DamageDefOf.Cut, damageAmount, armorPenetration, (Thing)Pawn);
				if (pawn.health.summaryHealth.SummaryHealthPercent < 0.25f && !pawn.Dead)
				{
					DamageInfo value = new DamageInfo(DamageDefOf.Cut, damageAmount, armorPenetration, -1f, Pawn);
					pawn.Kill(value);
				}
			}
		}
		parent.AddEffecterToMaintain(MiliraDefOf.Milira_SickleRotation.Spawn(Pawn.Position, map), Pawn.Position, 17, map);
		base.Apply(target, dest);
	}

	private List<IntVec3> AffectedCells(IntVec3 target, float radius)
	{
		tmpCells.Clear();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target, radius, useCenter: true))
		{
			if (item.IsValid || item.InBounds(Pawn.Map))
			{
				tmpCells.Add(item);
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
			return true;
		}
	}
}
