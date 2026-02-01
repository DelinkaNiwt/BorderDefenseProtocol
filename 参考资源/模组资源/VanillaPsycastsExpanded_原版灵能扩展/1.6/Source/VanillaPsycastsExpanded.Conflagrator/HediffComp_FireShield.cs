using RimWorld;
using VEF.Hediffs;
using Verse;

namespace VanillaPsycastsExpanded.Conflagrator;

public class HediffComp_FireShield : HediffComp_Shield
{
	protected override void ApplyDamage(DamageInfo dinfo)
	{
		dinfo.Instigator.TryAttachFire(25f, ((HediffComp)this).Pawn);
	}
}
