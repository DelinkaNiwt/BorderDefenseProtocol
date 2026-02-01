using Verse;

namespace NCL;

public class Projectile_Fragmented : Projectile
{
	private Comp_FragmentedExplosive fragComp;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		fragComp = this.TryGetComp<Comp_FragmentedExplosive>();
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		base.Impact(hitThing, blockedByShield);
		if (fragComp != null)
		{
			fragComp.TriggerExplosion(base.Map);
		}
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		if (fragComp != null && !base.Destroyed)
		{
			fragComp.TriggerExplosion(base.Map);
		}
		base.Destroy(mode);
	}
}
