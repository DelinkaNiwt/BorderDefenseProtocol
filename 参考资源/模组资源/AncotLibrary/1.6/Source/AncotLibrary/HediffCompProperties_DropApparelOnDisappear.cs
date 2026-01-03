using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class HediffCompProperties_DropApparelOnDisappear : HediffCompProperties
{
	public List<string> apparelTags = new List<string>();

	public HediffCompProperties_DropApparelOnDisappear()
	{
		compClass = typeof(HediffComp_DropApparelOnDisappear);
	}
}
