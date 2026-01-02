using Verse;

namespace AncotLibrary;

public class ThinkNode_ConditionalEnemyInTurretRange : ThinkNode_ConditionalUnderCombatPressureIgnoreDowned
{
	protected override bool Satisfied(Pawn pawn)
	{
		CompTurretGun_Custom compTurretGun_Custom = pawn.TryGetComp<CompTurretGun_Custom>();
		if (pawn.Spawned && !pawn.Downed)
		{
			return (compTurretGun_Custom != null) ? ThinkNode_ConditionalUnderCombatPressureIgnoreDowned.EnemiesAreNearby(pawn, 25, passDoors: true, compTurretGun_Custom.CurrentEffectiveVerb.EffectiveRange, minCloseTargets) : ThinkNode_ConditionalUnderCombatPressureIgnoreDowned.EnemiesAreNearby(pawn, 9, passDoors: true, MaxThreatDistance, minCloseTargets);
		}
		return false;
	}
}
