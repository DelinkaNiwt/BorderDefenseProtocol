using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityProjectShield : CompAbilityEffect
{
	private MechShield mechShield;

	private CompProjectileInterceptor projectileInterceptor;

	public new CompProperties_AbilityProjectShield Props => (CompProperties_AbilityProjectShield)props;

	public virtual int shieldHitPoint => Props.hitPointBase;

	private CompProjectileInterceptor ProjectileInterceptor
	{
		get
		{
			if (projectileInterceptor == null && mechShield != null)
			{
				projectileInterceptor = mechShield.GetComp<CompProjectileInterceptor>();
			}
			return projectileInterceptor;
		}
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		GenerateShield(target.Pawn);
	}

	private void GenerateShield(Pawn pawn)
	{
		mechShield = (MechShield)GenSpawn.Spawn(Props.mechShieldType, pawn.Position, pawn.Map);
		mechShield.SetTarget(pawn);
		ProjectileInterceptor.maxHitPointsOverride = shieldHitPoint;
		ProjectileInterceptor.currentHitPoints = shieldHitPoint;
	}
}
