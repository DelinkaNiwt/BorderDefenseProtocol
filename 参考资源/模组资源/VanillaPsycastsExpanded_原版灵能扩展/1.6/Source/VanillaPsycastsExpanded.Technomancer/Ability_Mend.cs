using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Ability_Mend : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			if (globalTargetInfo.Thing is Pawn pawn)
			{
				if (pawn.RaceProps.Humanlike)
				{
					float amountTotal = ((Ability)this).GetPowerForPawn();
					List<ThingWithComps> toHeal = (from t in pawn.equipment.AllEquipmentListForReading.Concat(pawn.apparel.WornApparel)
						where t.def.useHitPoints && t.HitPoints < t.MaxHitPoints
						select t).ToList();
					int num = (int)amountTotal * toHeal.Count;
					int num2 = 0;
					while (amountTotal >= 1f && toHeal.Count > 0 && num2++ <= num)
					{
						toHeal.RemoveAll(delegate(ThingWithComps t)
						{
							amountTotal -= Mend(t, (amountTotal >= (float)toHeal.Count) ? (amountTotal / (float)toHeal.Count) : amountTotal);
							return t.HitPoints == t.MaxHitPoints;
						});
					}
					if (num2 >= num)
					{
						Log.Warning($"[VPE] Too many iterations in Ability_Mend.Cast by {base.pawn} on {pawn}");
					}
				}
				else
				{
					if (!pawn.RaceProps.IsMechanoid)
					{
						continue;
					}
					float amountTotal2 = ((Ability)this).GetPowerForPawn();
					List<Hediff_Injury> toHeal2 = (from h in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>()
						where !h.IsPermanent()
						select h).ToList();
					int num3 = (int)amountTotal2 * toHeal2.Count;
					int num4 = 0;
					while (amountTotal2 >= 0f && toHeal2.Count > 0 && num4++ <= num3)
					{
						toHeal2.RemoveAll(delegate(Hediff_Injury injury)
						{
							float num5 = Mathf.Clamp((amountTotal2 >= 1f) ? (amountTotal2 / (float)toHeal2.Count) : amountTotal2, 0f, injury.Severity);
							injury.Heal(num5);
							amountTotal2 -= num5;
							return injury.Severity == 0f;
						});
					}
					if (num4 >= num3)
					{
						Log.Warning($"[VPE] Too many iterations in Ability_Mend.Cast by {base.pawn} on {pawn}");
					}
					if (toHeal2.Count == 0)
					{
						MechRepairUtility.RepairTick(pawn, 1);
					}
				}
			}
			else
			{
				Mend(globalTargetInfo.Thing, ((Ability)this).GetPowerForPawn());
			}
		}
	}

	public override float GetPowerForPawn()
	{
		return (base.pawn.GetStatValue(StatDefOf.PsychicSensitivity) - 1f) * 100f;
	}

	private static int Mend(Thing t, int amount)
	{
		int hitPoints = t.HitPoints;
		t.HitPoints = Mathf.Clamp(t.HitPoints + amount, t.HitPoints, t.MaxHitPoints);
		return t.HitPoints - hitPoints;
	}

	private static float Mend(Thing t, float amount)
	{
		return (float)Mend(t, (int)amount) + (amount - (float)(int)amount);
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!((Ability)this).ValidateTarget(target, showMessages))
		{
			return false;
		}
		if (target.Thing is Pawn pawn)
		{
			if (pawn.RaceProps.Humanlike)
			{
				if (pawn.Faction != base.pawn.Faction)
				{
					if (showMessages)
					{
						Messages.Message("VPE.MustBeAlly".Translate(), MessageTypeDefOf.RejectInput, historical: false);
					}
					return false;
				}
				if (!pawn.equipment.AllEquipmentListForReading.Any((ThingWithComps t) => t.def.useHitPoints && t.HitPoints < t.MaxHitPoints) && !pawn.apparel.WornApparel.Any((Apparel t) => t.def.useHitPoints && t.HitPoints < t.MaxHitPoints))
				{
					if (showMessages)
					{
						Messages.Message("VPE.MustHaveDamagedEquipment".Translate(), MessageTypeDefOf.RejectInput, historical: false);
					}
					return false;
				}
				return true;
			}
			if (pawn.RaceProps.IsMechanoid)
			{
				if (!ModsConfig.BiotechActive || !pawn.IsMechAlly(base.pawn))
				{
					if (showMessages)
					{
						Messages.Message("VPE.MustBeAlly".Translate(), MessageTypeDefOf.RejectInput, historical: false);
					}
					return false;
				}
				if (!MechRepairUtility.CanRepair(pawn))
				{
					if (showMessages)
					{
						Messages.Message("VPE.MustBeDamaged".Translate(), MessageTypeDefOf.RejectInput, historical: false);
					}
					return false;
				}
				return true;
			}
			if (showMessages)
			{
				Messages.Message("VPE.NoAnimals".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		Thing thing = target.Thing;
		if (thing != null)
		{
			ThingDef def = thing.def;
			if (def != null && def.useHitPoints)
			{
				if (thing.HitPoints < thing.MaxHitPoints)
				{
					return true;
				}
				if (showMessages)
				{
					Messages.Message("VPE.MustBeDamaged".Translate(), MessageTypeDefOf.RejectInput, historical: false);
				}
				return false;
			}
		}
		return false;
	}
}
