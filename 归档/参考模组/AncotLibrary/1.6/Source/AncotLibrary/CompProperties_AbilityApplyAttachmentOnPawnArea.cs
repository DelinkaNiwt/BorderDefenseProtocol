using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityApplyAttachmentOnPawnArea : CompProperties_AbilityEffect
{
	public float radius = 15f;

	public EffecterDef effecter;

	public ThingDef attachment;

	public bool ignoreAlly = true;

	public CompProperties_AbilityApplyAttachmentOnPawnArea()
	{
		compClass = typeof(CompAbilityApplyAttachmentOnPawnArea);
	}
}
