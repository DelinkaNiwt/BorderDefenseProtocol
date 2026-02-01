using RimWorld;
using Verse;
using Verse.Sound;

namespace NCL;

public class Verb_AdvancedShoot : Verb_Shoot
{
	private bool usingSecondaryProjectile = false;

	private int primaryProjectileShotsFired = 0;

	private int secondaryProjectileShotsFired = 0;

	private bool initialized = false;

	protected Comp_AdvancedAmmo AmmoComp => base.EquipmentSource?.GetComp<Comp_AdvancedAmmo>();

	protected int PrimaryProjectileCount => AmmoComp?.GetPrimaryProjectileCount() ?? 1;

	protected int SecondaryProjectileCount => AmmoComp?.GetSecondaryProjectileCount() ?? 0;

	protected bool IsBonusShot => AmmoComp?.Props.isBonusShot ?? false;

	protected bool IsSimultaneousShot => AmmoComp?.Props.isSimultaneousShot ?? false;

	protected SoundDef SecondarySoundCast => AmmoComp?.Props.secondarySoundCast;

	protected SoundDef SecondarySoundCastTail => AmmoComp?.Props.secondarySoundCastTail;

	public override ThingDef Projectile
	{
		get
		{
			if (base.EquipmentSource != null)
			{
				CompChangeableProjectile comp = base.EquipmentSource.GetComp<CompChangeableProjectile>();
				if (comp != null && comp.Loaded)
				{
					return comp.Projectile;
				}
				Comp_AdvancedAmmo ammoComp = AmmoComp;
				if (ammoComp != null)
				{
					ammoComp.SetUsingSecondaryProjectile(usingSecondaryProjectile);
					if (usingSecondaryProjectile && ammoComp.HasSecondaryProjectile())
					{
						return ammoComp.GetSecondaryProjectile();
					}
					ThingDef primaryProjectile = ammoComp.GetPrimaryProjectile();
					if (primaryProjectile != null)
					{
						return primaryProjectile;
					}
				}
			}
			return verbProps.defaultProjectile;
		}
	}

	public override void WarmupComplete()
	{
		Comp_AdvancedAmmo ammoComp = AmmoComp;
		if (ammoComp != null && ammoComp.HasSecondaryProjectile())
		{
			usingSecondaryProjectile = false;
			primaryProjectileShotsFired = (secondaryProjectileShotsFired = 0);
			initialized = false;
			ammoComp.SetUsingSecondaryProjectile(value: false);
		}
		base.WarmupComplete();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref usingSecondaryProjectile, "usingSecondaryProjectile", defaultValue: false);
		Scribe_Values.Look(ref primaryProjectileShotsFired, "primaryProjectileShotsFired", 0);
		Scribe_Values.Look(ref secondaryProjectileShotsFired, "secondaryProjectileShotsFired", 0);
		Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);
	}

	protected override bool TryCastShot()
	{
		Comp_AdvancedAmmo ammoComp = AmmoComp;
		if (ammoComp == null || !ammoComp.HasSecondaryProjectile())
		{
			return base.TryCastShot();
		}
		if (!initialized)
		{
			usingSecondaryProjectile = false;
			primaryProjectileShotsFired = (secondaryProjectileShotsFired = 0);
			initialized = true;
			ammoComp.SetUsingSecondaryProjectile(value: false);
		}
		bool result = true;
		if (IsSimultaneousShot)
		{
			bool primaryResult = base.TryCastShot();
			usingSecondaryProjectile = true;
			ammoComp.SetUsingSecondaryProjectile(value: true);
			PlaySecondarySound();
			bool secondaryResult = base.TryCastShot();
			usingSecondaryProjectile = false;
			ammoComp.SetUsingSecondaryProjectile(value: false);
			result = primaryResult && secondaryResult;
		}
		else if (!usingSecondaryProjectile)
		{
			result = base.TryCastShot();
			primaryProjectileShotsFired++;
			if (primaryProjectileShotsFired >= PrimaryProjectileCount)
			{
				usingSecondaryProjectile = true;
				ammoComp.SetUsingSecondaryProjectile(value: true);
				secondaryProjectileShotsFired = 0;
			}
		}
		else
		{
			PlaySecondarySound();
			result = base.TryCastShot();
			secondaryProjectileShotsFired++;
			if (secondaryProjectileShotsFired >= SecondaryProjectileCount)
			{
				usingSecondaryProjectile = false;
				ammoComp.SetUsingSecondaryProjectile(value: false);
				primaryProjectileShotsFired = 0;
			}
			if (IsBonusShot)
			{
				bool wasUsingSecondary = usingSecondaryProjectile;
				usingSecondaryProjectile = false;
				ammoComp.SetUsingSecondaryProjectile(value: false);
				base.TryCastShot();
				usingSecondaryProjectile = wasUsingSecondary;
				ammoComp.SetUsingSecondaryProjectile(wasUsingSecondary);
			}
		}
		return result;
	}

	private void PlaySecondarySound()
	{
		SecondarySoundCast?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
		SecondarySoundCastTail?.PlayOneShotOnCamera(caster.Map);
	}
}
