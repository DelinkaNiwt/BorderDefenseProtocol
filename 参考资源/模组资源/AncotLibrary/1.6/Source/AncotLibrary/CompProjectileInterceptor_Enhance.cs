using System;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProjectileInterceptor_Enhance : ThingComp
{
	public CompProperties_ProjectileInterceptor_Enhance Props => (CompProperties_ProjectileInterceptor_Enhance)props;

	private QualityCategory Quality
	{
		get
		{
			if (parent.TryGetComp<CompQuality>() != null)
			{
				return parent.TryGetComp<CompQuality>().Quality;
			}
			return QualityCategory.Normal;
		}
	}

	private int HitPoint => (int)(QualityMultiplier(Quality) * (float)Props.hitPointBase);

	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				return null;
			}
			return wearer;
		}
	}

	private float QualityMultiplier(QualityCategory qc)
	{
		return qc switch
		{
			QualityCategory.Awful => Props.factorAwful, 
			QualityCategory.Poor => Props.factorPoor, 
			QualityCategory.Normal => Props.factorNormal, 
			QualityCategory.Good => Props.factorGood, 
			QualityCategory.Excellent => Props.factorExcellent, 
			QualityCategory.Masterwork => Props.factorMasterwork, 
			QualityCategory.Legendary => Props.factorLegendary, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		SetOverrideHitPoint(PawnOwner);
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		SetOverrideHitPoint(pawn);
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		SetNullHitPoint(pawn);
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		SetNullHitPoint(PawnOwner);
	}

	private void SetOverrideHitPoint(Pawn pawn)
	{
		CompProjectileInterceptor compProjectileInterceptor = PawnOwner.TryGetComp<CompProjectileInterceptor>();
		if (pawn != null && compProjectileInterceptor != null)
		{
			compProjectileInterceptor.maxHitPointsOverride = HitPoint;
		}
	}

	private void SetNullHitPoint(Pawn pawn)
	{
		CompProjectileInterceptor compProjectileInterceptor = pawn.TryGetComp<CompProjectileInterceptor>();
		if (pawn != null && compProjectileInterceptor != null)
		{
			compProjectileInterceptor.maxHitPointsOverride = null;
			compProjectileInterceptor.currentHitPoints = compProjectileInterceptor.HitPointsMax;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
	}
}
