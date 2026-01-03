using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProjectileInterceptor_Apparel : ThingComp
{
	private MechShield mechShield;

	private CompProjectileInterceptor projectileInterceptor;

	public CompProperties_ProjectileInterceptor_Apparel Props => (CompProperties_ProjectileInterceptor_Apparel)props;

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

	private QualityCategory quality
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

	private int shieldHitPoint => (int)(AncotUtility.QualityFactor(quality) * (float)Props.hitPointBase);

	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	public override void CompTickInterval(int delta)
	{
		if (mechShield == null && PawnOwner != null)
		{
			GenerateShield();
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (PawnOwner != null && mechShield == null)
		{
			GenerateShield();
		}
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		if (ProjectileInterceptor != null)
		{
			yield return new Gizmo_ProjectileInterceptorHitPoints
			{
				interceptor = ProjectileInterceptor
			};
		}
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		DeSpawnShield();
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		DeSpawnShield();
	}

	private void GenerateShield()
	{
		mechShield = (MechShield)GenSpawn.Spawn(Props.mechShieldType, PawnOwner.Position, PawnOwner.Map);
		mechShield.SetFaction(PawnOwner.Faction);
		mechShield.SetTarget(PawnOwner);
		ProjectileInterceptor.maxHitPointsOverride = shieldHitPoint;
		ProjectileInterceptor.currentHitPoints = shieldHitPoint;
	}

	private void DeSpawnShield()
	{
		if (mechShield != null)
		{
			if (!mechShield.Destroyed)
			{
				mechShield.Destroy();
			}
			mechShield = null;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_References.Look(ref mechShield, "mechShield");
	}
}
