using RimWorld;
using Verse;

namespace NCL;

public class HediffComp_ForceBodyType : HediffComp
{
	private BodyTypeDef originalBodyType;

	public HediffCompProperties_ForceBodyType Props => (HediffCompProperties_ForceBodyType)props;

	public override void CompPostMake()
	{
		base.CompPostMake();
		originalBodyType = base.Pawn.story.bodyType;
		if (Props.bodyType != null)
		{
			base.Pawn.story.bodyType = Props.bodyType;
		}
		PortraitsCache.SetDirty(base.Pawn);
		GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(base.Pawn);
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		if (originalBodyType != null)
		{
			base.Pawn.story.bodyType = originalBodyType;
		}
		PortraitsCache.SetDirty(base.Pawn);
		GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(base.Pawn);
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Defs.Look(ref originalBodyType, "originalBodyType");
	}
}
