using RimWorld;
using Verse;

namespace NCLWorm;

public class CompCauseHediff_AoEPlus : ThingComp
{
	public float range;

	public CompProperties_CauseHediff_AoEAndRing Props => (CompProperties_CauseHediff_AoEAndRing)props;

	private bool IsPawnAffected(Pawn target)
	{
		if (target.Dead || target.health == null)
		{
			return false;
		}
		if (!target.HostileTo(parent))
		{
			return false;
		}
		if (target == parent)
		{
			return false;
		}
		return target.PositionHeld.DistanceTo(parent.PositionHeld) <= range;
	}

	public override void PostPostMake()
	{
		base.PostPostMake();
		range = Props.range;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref range, "range", 0f);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && range <= 0f)
		{
			range = Props.range;
		}
	}

	public override void CompTick()
	{
		if (!parent.IsHashIntervalTick(Props.checkInterval) || !parent.SpawnedOrAnyParentSpawned)
		{
			return;
		}
		foreach (Pawn item in parent.MapHeld.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnAffected(item))
			{
				GiveOrUpdateHediff(item);
			}
			if (item.carryTracker.CarriedThing is Pawn target && IsPawnAffected(target))
			{
				GiveOrUpdateHediff(target);
			}
		}
	}

	private void GiveOrUpdateHediff(Pawn target)
	{
		Hediff orAddHediff = target.health.GetOrAddHediff(Props.hediff);
		HediffComp_Disappears hediffComp_Disappears = orAddHediff.TryGetComp<HediffComp_Disappears>();
		if (hediffComp_Disappears == null)
		{
			Log.ErrorOnce("CompCauseHediff_AoE has a hediff in props which does not have a HediffComp_Disappears", 78939939);
		}
		else
		{
			hediffComp_Disappears.ticksToDisappear = Props.checkInterval;
		}
	}

	public override void PostDraw()
	{
		if (Props.drawLines && Find.Selector.IsSelected(parent))
		{
			GenDraw.DrawRadiusRing(parent.Position, Props.range);
		}
	}
}
