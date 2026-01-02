using RimWorld;
using Verse;

namespace AncotLibrary;

public class StorytellerDef_Custom : StorytellerDef
{
	private string overrideLabel;

	private string overrideDesc;

	public virtual string OverrideLabel => overrideLabel;

	public virtual string OverrideDesc => overrideDesc;

	public override void ResolveReferences()
	{
		base.ResolveReferences();
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			if (OverrideDesc != null)
			{
				description = OverrideDesc;
			}
			if (OverrideLabel != null)
			{
				label = OverrideLabel;
			}
		});
		for (int num = 0; num < comps.Count; num++)
		{
			comps[num].ResolveReferences(this);
		}
	}
}
