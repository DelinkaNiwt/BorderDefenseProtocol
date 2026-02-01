using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompFuelBasedDamageReduction : ThingComp
{
	private CompMechCarrier mechCarrier;

	public float CurrentDamageFactor
	{
		get
		{
			if (!IsActive)
			{
				return 1f;
			}
			float fuelPercent = GetFuelPercentage();
			if (fuelPercent >= 0.5f)
			{
				return Mathf.Lerp(1f, 0.5f, (fuelPercent - 0.5f) * 2f);
			}
			return Mathf.Lerp(1.5f, 1f, fuelPercent * 2f);
		}
	}

	private bool IsActive
	{
		get
		{
			if (!(parent is Pawn { Dead: false, Spawned: not false }))
			{
				return false;
			}
			if (mechCarrier == null)
			{
				mechCarrier = parent.TryGetComp<CompMechCarrier>();
			}
			return mechCarrier != null;
		}
	}

	private float GetFuelPercentage()
	{
		if (mechCarrier == null || mechCarrier.Props == null)
		{
			return 0f;
		}
		if (mechCarrier.Props.maxIngredientCount <= 0)
		{
			return 0f;
		}
		return (float)mechCarrier.IngredientCount / (float)mechCarrier.Props.maxIngredientCount;
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		base.PostPreApplyDamage(ref dinfo, out absorbed);
		if (!absorbed && IsActive)
		{
			float newAmount = dinfo.Amount * CurrentDamageFactor;
			dinfo.SetAmount(newAmount);
		}
	}
}
