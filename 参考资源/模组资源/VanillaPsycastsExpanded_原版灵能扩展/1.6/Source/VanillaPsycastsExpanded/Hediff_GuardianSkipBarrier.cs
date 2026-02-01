using UnityEngine;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded;

public class Hediff_GuardianSkipBarrier : Hediff_Overshield
{
	private Sustainer sustainer;

	public override Color OverlayColor => new ColorInt(79, 141, 247).ToColor;

	public override float OverlaySize => 9f;

	protected override void DestroyProjectile(Projectile projectile)
	{
		base.DestroyProjectile(projectile);
		AddEntropy();
	}

	public override void PostTick()
	{
		((HediffWithComps)(object)this).PostTick();
		AddEntropy();
		if (sustainer == null || sustainer.Ended)
		{
			sustainer = VPE_DefOf.VPE_GuardianSkipbarrier_Sustainer.TrySpawnSustainer(SoundInfo.InMap(((Hediff)(object)this).pawn, MaintenanceType.PerTick));
		}
		sustainer.Maintain();
	}

	public override void PostRemoved()
	{
		((HediffWithComps)(object)this).PostRemoved();
		if (!sustainer.Ended)
		{
			sustainer?.End();
		}
	}

	private void AddEntropy()
	{
		if (Find.TickManager.TicksGame % 10 == 0)
		{
			((Hediff)(object)this).pawn.psychicEntropy.TryAddEntropy(1f, null, scale: true, overLimit: true);
		}
		if (((Hediff)(object)this).pawn.psychicEntropy.EntropyValue >= ((Hediff)(object)this).pawn.psychicEntropy.MaxEntropy)
		{
			((Hediff)(object)this).pawn.health.RemoveHediff((Hediff)(object)this);
		}
	}
}
