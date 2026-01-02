using System.Collections.Generic;
using RimWorld;

namespace AncotLibrary;

public class CompProperties_ReadDiaLog_Nested : CompProperties_Usable
{
	public List<CustomDiaLog_Nested> customDiaLog;

	public bool oneUse = false;

	public CompProperties_ReadDiaLog_Nested()
	{
		compClass = typeof(CompReadDiaLog_Nested);
	}
}
