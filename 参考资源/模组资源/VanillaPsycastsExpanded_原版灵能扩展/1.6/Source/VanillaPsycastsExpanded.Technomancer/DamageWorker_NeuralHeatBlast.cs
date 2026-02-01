using System.Collections.Generic;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class DamageWorker_NeuralHeatBlast : DamageWorker
{
	protected override void ExplosionDamageThing(Explosion explosion, Thing t, List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell)
	{
		if (t is Pawn { psychicEntropy: not null, HasPsylink: not false } pawn && !damagedThings.Contains(t))
		{
			damagedThings.Add(t);
			if (ignoredThings == null || !ignoredThings.Contains(t))
			{
				pawn.psychicEntropy.TryAddEntropy(pawn.psychicEntropy.MaxEntropy - pawn.psychicEntropy.EntropyValue, explosion);
			}
		}
	}
}
