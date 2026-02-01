using System.Collections.Generic;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class PawnKindAbilityExtension_Psycasts : PawnKindAbilityExtension
{
	public List<PathUnlockData> unlockedPaths;

	public IntRange statUpgradePoints = IntRange.Zero;
}
