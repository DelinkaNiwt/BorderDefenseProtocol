using Verse;

namespace AncotLibrary;

public class CompDraftable : ThingComp
{
	public virtual bool Draftable => true;

	public CompProperties_Draftable Props => (CompProperties_Draftable)props;
}
