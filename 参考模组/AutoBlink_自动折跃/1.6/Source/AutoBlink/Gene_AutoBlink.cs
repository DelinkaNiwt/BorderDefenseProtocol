using Verse;

namespace AutoBlink;

public class Gene_AutoBlink : Gene
{
	private CompAutoBlink runtimeComp;

	private bool previouslyActive;

	private GeneExtension_AutoBlink Ext => def?.GetModExtension<GeneExtension_AutoBlink>();

	public override void PostAdd()
	{
		base.PostAdd();
		TryAttachComp();
	}

	public override void PostRemove()
	{
		base.PostRemove();
		DetachComp();
	}

	public override void Tick()
	{
		base.Tick();
		if (Active != previouslyActive)
		{
			if (Active)
			{
				TryAttachComp();
			}
			else
			{
				DetachComp();
			}
			previouslyActive = Active;
		}
	}

	private void TryAttachComp()
	{
		if (runtimeComp == null && pawn?.AllComps != null && Ext != null)
		{
			runtimeComp = new CompAutoBlink
			{
				parent = pawn,
				props = Ext.ToThingCompProps()
			};
			pawn.AllComps.Add(runtimeComp);
			runtimeComp.PostSpawnSetup(respawningAfterLoad: false);
		}
	}

	private void DetachComp()
	{
		if (runtimeComp != null && pawn?.AllComps != null)
		{
			pawn.AllComps.Remove(runtimeComp);
			runtimeComp = null;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Deep.Look(ref runtimeComp, "runtimeComp");
		if (Scribe.mode != LoadSaveMode.PostLoadInit)
		{
			return;
		}
		previouslyActive = Active;
		if (runtimeComp != null)
		{
			runtimeComp.parent = pawn;
			if (runtimeComp.props == null && Ext != null)
			{
				runtimeComp.props = Ext.ToThingCompProps();
			}
			if (!pawn.AllComps.Contains(runtimeComp))
			{
				pawn.AllComps.Add(runtimeComp);
			}
			runtimeComp.PostSpawnSetup(respawningAfterLoad: true);
		}
	}
}
