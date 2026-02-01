using RimWorld;
using Verse;

namespace NCL;

public class HediffComp_ForceBody : HediffComp
{
	private BodyTypeDef originalBodyType;

	private BodyDef originalBodyDef;

	public HediffCompProperties_ForceBody Props => (HediffCompProperties_ForceBody)props;

	public override void CompPostMake()
	{
		base.CompPostMake();
		StoreOriginalState();
		ApplyNewBody();
	}

	private void StoreOriginalState()
	{
		originalBodyType = base.Pawn.story?.bodyType;
		originalBodyDef = base.Pawn.def.race.body;
	}

	private void ApplyNewBody()
	{
		if (Props.bodyType != null && base.Pawn.story != null)
		{
			base.Pawn.story.bodyType = Props.bodyType;
		}
		if (Props.bodyDef != null)
		{
			base.Pawn.def.race.body = Props.bodyDef;
		}
		UpdateGraphics();
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		RestoreOriginalState();
	}

	private void RestoreOriginalState()
	{
		if (originalBodyType != null && base.Pawn.story != null)
		{
			base.Pawn.story.bodyType = originalBodyType;
		}
		if (originalBodyDef != null)
		{
			base.Pawn.def.race.body = originalBodyDef;
		}
		UpdateGraphics();
	}

	private void UpdateGraphics()
	{
		PawnRenderer renderer = base.Pawn.Drawer?.renderer;
		if (renderer != null)
		{
			renderer.SetAllGraphicsDirty();
			if (renderer.renderTree.Resolved)
			{
				renderer.renderTree.SetDirty();
			}
		}
		PortraitsCache.SetDirty(base.Pawn);
		GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(base.Pawn);
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Defs.Look(ref originalBodyType, "originalBodyType");
		Scribe_Defs.Look(ref originalBodyDef, "originalBodyDef");
	}
}
