using Verse;

namespace AncotLibrary;

public class Projectile_ExplosiveCustom : Projectile_Explosive
{
	public Projectile_ExplosiveCustom_Extension Props => def.GetModExtension<Projectile_ExplosiveCustom_Extension>();

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (Props.impactEffecter != null)
		{
			Props.impactEffecter.Spawn().Trigger(hitThing, launcher);
		}
		base.Impact(hitThing, blockedByShield);
	}
}
