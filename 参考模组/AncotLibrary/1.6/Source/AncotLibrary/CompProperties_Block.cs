using Verse;

namespace AncotLibrary;

public class CompProperties_Block : CompProperties
{
	public bool blockMelee = true;

	public bool blockRanged = false;

	public bool useWeaponCharge = false;

	public float baseBlockChance = 0.1f;

	public float maxBlockChance = 0.9f;

	public float meleeSkillBonusChance = 0.03f;

	public int blockStanceTick = 30;

	public CompProperties_Block()
	{
		compClass = typeof(CompBlock);
	}
}
