using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class HediffCompProperties_SetWorkPriority : HediffCompProperties
{
	public List<DE_WorkPriority> workPriorities = new List<DE_WorkPriority>();

	public DE_WorkPriority workPriority;

	public HediffCompProperties_SetWorkPriority()
	{
		compClass = typeof(HediffComp_SetWorkPriority);
	}
}
