using Verse;

namespace AncotLibrary;

public class CompProjectileTrail : ThingComp
{
	public Comproperties_ProjectileTrail Props => (Comproperties_ProjectileTrail)props;

	public override void PostExposeData()
	{
		base.PostExposeData();
	}

	public override void CompTick()
	{
		LeaveTrail();
		base.CompTick();
	}

	private void LeaveTrail()
	{
		Map map = parent.Map;
		if (map != null && GenTicks.TicksGame % Props.tickPerTrail == 0)
		{
			AncotFleckMaker.ThrowTrailFleckUp(parent.DrawPos, map, Props.color, Props.fleckDef);
		}
	}
}
