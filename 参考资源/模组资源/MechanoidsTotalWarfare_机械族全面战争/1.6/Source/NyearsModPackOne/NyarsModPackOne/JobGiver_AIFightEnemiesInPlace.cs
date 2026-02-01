using RimWorld;
using Verse;

namespace NyarsModPackOne;

public class JobGiver_AIFightEnemiesInPlace : JobGiver_AIFightEnemy
{
	protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
	{
		Thing enemyTarget = pawn.mindState.enemyTarget;
		bool allowManualCastWeapons = !pawn.IsColonist && !pawn.IsColonySubhuman;
		Verb verb = verbToUse ?? pawn.TryGetAttackVerb(enemyTarget, allowManualCastWeapons, allowTurrets);
		if (verb == null || !pawn.Position.InHorDistOf(enemyTarget.Position, verb.EffectiveRange))
		{
			dest = IntVec3.Invalid;
			return false;
		}
		dest = pawn.Position;
		return true;
	}
}
