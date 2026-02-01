using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_PsychicShock : Ability
{
	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!(target.Thing is Pawn thing) || thing.GetStatValue(StatDefOf.PsychicSensitivity) <= 0f)
		{
			return false;
		}
		return ((Ability)this).ValidateTarget(target, showMessages);
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			if (Rand.Chance(0.3f))
			{
				Pawn pawn = globalTargetInfo.Thing as Pawn;
				globalTargetInfo.Thing.TryAttachFire(0.5f, ((Ability)this).Caster);
				BodyPartRecord bodyPartRecord = pawn?.health.hediffSet.GetBrain();
				if (bodyPartRecord != null)
				{
					int num = Rand.RangeInclusive(1, 5);
					pawn.TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, base.pawn, bodyPartRecord));
				}
			}
		}
	}
}
