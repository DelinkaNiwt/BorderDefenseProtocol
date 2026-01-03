using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_CMCSniperTurret : Building_CMCTurretGun
{
	public CompPowerTrader powerTraderComp;

	private Pawn pawnTarget;

	public CompPowerTrader PowerTraderComp
	{
		get
		{
			CompPowerTrader result;
			if ((result = powerTraderComp) == null)
			{
				result = (powerTraderComp = this.TryGetComp<CompPowerTrader>());
			}
			return result;
		}
	}

	public override bool CanSetForcedTarget => true;

	private void RunDetection()
	{
		if (!PowerTraderComp.PowerOn)
		{
			return;
		}
		IReadOnlyList<Pawn> allPawnsSpawned = base.Map.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			if (allPawnsSpawned[i].Position.InHorDistOf(base.Position, 19.9f) && allPawnsSpawned[i].IsPsychologicallyInvisible())
			{
				pawnTarget = allPawnsSpawned[i];
				break;
			}
		}
	}

	protected override void Tick()
	{
		if (Find.TickManager.TicksGame % 300 == 0)
		{
			RunDetection();
		}
		base.Tick();
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		if (Find.Selector.IsSelected(this))
		{
			GenDraw.DrawRadiusRing(DrawPos.ToIntVec3(), 19.9f, Color.cyan);
		}
	}

	public override LocalTargetInfo TryFindNewTarget()
	{
		LocalTargetInfo result;
		if (pawnTarget != null && !pawnTarget.DeadOrDowned && pawnTarget.Faction.HostileTo(base.Faction))
		{
			result = pawnTarget;
		}
		else
		{
			IAttackTargetSearcher attackTargetSearcher = TargSearcher();
			Faction faction = attackTargetSearcher.Thing.Faction;
			float range = AttackVerb.verbProps.range;
			if (Rand.Value < 0.5f && AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate(Building x)
			{
				float num = AttackVerb.verbProps.EffectiveMinRange(x, this);
				float num2 = x.Position.DistanceToSquared(base.Position);
				return num2 > num * num && num2 < range * range;
			}).TryRandomElement(out var result2))
			{
				return result2;
			}
			TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
			if (!AttackVerb.ProjectileFliesOverhead())
			{
				targetScanFlags |= TargetScanFlags.NeedLOSToAll;
			}
			if (AttackVerb.IsIncendiary_Ranged())
			{
				targetScanFlags |= TargetScanFlags.NeedNonBurning;
			}
			if (base.IsMortar)
			{
				targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
			}
			result = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, IsValidTarget);
		}
		return result;
	}
}
