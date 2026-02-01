using System.Linq;
using RimWorld;
using Verse;

namespace NCL;

public class HediffComp_RemoveWhenEnemyNearby : HediffComp
{
	private const float DefaultCheckRadius = 35f;

	private const int CheckInterval = 60;

	private int ticksUntilNextCheck;

	public HediffCompProperties_RemoveWhenEnemyNearby Props => (HediffCompProperties_RemoveWhenEnemyNearby)props;

	private float CheckRadius => Props.checkRadius ?? 35f;

	public override void CompPostMake()
	{
		base.CompPostMake();
		ticksUntilNextCheck = 60;
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (ticksUntilNextCheck > 0)
		{
			ticksUntilNextCheck--;
			return;
		}
		ticksUntilNextCheck = 60;
		if (EnemyPawnNearby())
		{
			parent.pawn.health.RemoveHediff(parent);
		}
	}

	private bool EnemyPawnNearby()
	{
		Map map = parent.pawn.MapHeld;
		if (map == null)
		{
			return false;
		}
		return (from p in GenRadial.RadialDistinctThingsAround(parent.pawn.PositionHeld, map, CheckRadius, useCenter: true).OfType<Pawn>()
			where p != parent.pawn
			select p).Any((Pawn pawn) => parent.pawn.HostileTo(pawn));
	}

	public override string CompDebugString()
	{
		return $"检测半径: {CheckRadius}格\n下次检查: {ticksUntilNextCheck} ticks";
	}
}
