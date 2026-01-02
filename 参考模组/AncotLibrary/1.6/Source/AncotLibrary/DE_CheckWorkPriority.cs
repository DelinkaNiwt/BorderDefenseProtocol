using Verse;

namespace AncotLibrary;

public class DE_CheckWorkPriority : ILoadReferenceable
{
	public WorkTypeDef workType;

	public int newPriority = -1;

	public int oldPriority = -1;

	public string GetUniqueLoadID()
	{
		return string.Format("DE_WorkPriority_{0}_{1}_{2}", workType?.defName ?? "null", newPriority, oldPriority);
	}
}
