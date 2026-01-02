using Verse;

namespace AncotLibrary;

public class Verb_TurretShoot : Verb_Shoot
{
	protected override bool TryCastShot()
	{
		bool result = base.TryCastShot();
		if (caster != null)
		{
			caster.TryGetComp<CompTurretGun_Custom>()?.ShotOnce();
			caster.TryGetComp<CompTurretGun_Building>()?.ShotOnce();
		}
		return result;
	}
}
