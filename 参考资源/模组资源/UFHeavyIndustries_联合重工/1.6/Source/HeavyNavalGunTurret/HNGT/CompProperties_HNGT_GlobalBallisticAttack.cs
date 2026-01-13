using Verse;

namespace HNGT;

public class CompProperties_HNGT_GlobalBallisticAttack : CompProperties
{
	public int cooldownSeconds = 900;

	public string iconPath;

	public string worldObjectDefName;

	public string payloadThingDefName;

	public CompProperties_HNGT_GlobalBallisticAttack()
	{
		compClass = typeof(Comp_HNGT_GlobalBallisticAttack);
	}
}
