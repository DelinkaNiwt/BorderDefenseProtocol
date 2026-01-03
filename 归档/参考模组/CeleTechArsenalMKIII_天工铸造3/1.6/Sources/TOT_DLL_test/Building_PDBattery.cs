using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_PDBattery : Building_CMCTurretGun
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

	protected override void Tick()
	{
		base.Tick();
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
