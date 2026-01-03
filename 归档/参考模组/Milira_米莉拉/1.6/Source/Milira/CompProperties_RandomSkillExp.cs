using RimWorld;

namespace Milira;

public class CompProperties_RandomSkillExp : CompProperties_Usable
{
	public int expPoints = 2000;

	public float interestChance = 1f;

	public CompProperties_RandomSkillExp()
	{
		compClass = typeof(CompRandomSkillExp);
	}
}
