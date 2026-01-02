using Verse;

namespace AutoBlink;

public class HediffComp_AutoBlink : HediffComp
{
	private CompAutoBlink runtimeComp;

	public HediffCompProperties_AutoBlink HProps => (HediffCompProperties_AutoBlink)props;

	public override void CompPostPostAdd(DamageInfo? dinfo)
	{
		base.CompPostPostAdd(dinfo);
		runtimeComp = new CompAutoBlink();
		runtimeComp.parent = parent.pawn;
		runtimeComp.props = HProps.ToThingCompProps();
		runtimeComp.InitFromHediff(this);
		parent.pawn.AllComps.Add(runtimeComp);
		runtimeComp.PostSpawnSetup(respawningAfterLoad: false);
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		if (runtimeComp != null && parent?.pawn?.AllComps != null)
		{
			parent.pawn.AllComps.Remove(runtimeComp);
			runtimeComp = null;
		}
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Deep.Look(ref runtimeComp, "runtimeComp");
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (runtimeComp == null)
			{
				runtimeComp = new CompAutoBlink();
			}
			runtimeComp.parent = parent.pawn;
			if (runtimeComp.props == null)
			{
				runtimeComp.props = HProps.ToThingCompProps();
			}
			runtimeComp.InitFromHediff(this);
			if (!parent.pawn.AllComps.Contains(runtimeComp))
			{
				parent.pawn.AllComps.Add(runtimeComp);
			}
			runtimeComp.PostSpawnSetup(respawningAfterLoad: true);
		}
	}
}
