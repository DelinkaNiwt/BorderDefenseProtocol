using System.Collections.Generic;
using Verse;

namespace Milira;

public class CompProperties_MilianHairSwitch : CompProperties
{
	public string gizmoIconPath = "AncotLibrary/Gizmos/SwitchA";

	public List<string> frontHairPaths;

	public List<string> behindHairPaths;

	public CompProperties_MilianHairSwitch()
	{
		compClass = typeof(CompMilianHairSwitch);
	}
}
