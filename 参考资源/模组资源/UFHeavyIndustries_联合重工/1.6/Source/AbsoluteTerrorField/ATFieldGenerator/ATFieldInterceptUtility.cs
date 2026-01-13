using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

public static class ATFieldInterceptUtility
{
	public static bool TryRandomElementWithAbductionCheck(IEnumerable<Pawn> source, out Pawn result)
	{
		if (!source.TryRandomElement(out result))
		{
			return false;
		}
		if (CheckAbductionAttempt(null, result, forceHostile: true))
		{
			return false;
		}
		return true;
	}

	public static bool CheckAbductionAttempt(Pawn instigator, Pawn victim, bool forceHostile = false)
	{
		if (victim == null || victim.Map == null)
		{
			return false;
		}
		if (!forceHostile && instigator != null && !instigator.HostileTo(victim))
		{
			return false;
		}
		ATFieldManager aTFieldManager = ATFieldManager.Get(victim.Map);
		if (aTFieldManager == null || aTFieldManager.activeFields.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < aTFieldManager.activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = aTFieldManager.activeFields[i];
			if (comp_AbsoluteTerrorField.Active && comp_AbsoluteTerrorField.antiTeleport && victim.Position.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
			{
				float amount = 2000f;
				if (comp_AbsoluteTerrorField.TryConsumeEnergy(amount))
				{
					FleckMaker.ThrowLightningGlow(victim.Position.ToVector3Shifted(), victim.Map, 3f);
					Messages.Message("ATField_TeleportIntercepted".Translate(), new TargetInfo(victim.Position, victim.Map), MessageTypeDefOf.NeutralEvent);
					comp_AbsoluteTerrorField.SpawnInterceptEffect(victim.Position.ToVector3Shifted(), 100f);
					return true;
				}
			}
		}
		return false;
	}

	public static IntVec3 TryInterceptTeleportDestination(Map map, IntVec3 targetCell, Thing teleporter)
	{
		ATFieldManager aTFieldManager = ATFieldManager.Get(map);
		if (aTFieldManager == null || aTFieldManager.activeFields.Count == 0)
		{
			return targetCell;
		}
		IntVec3 intVec = targetCell;
		bool flag = false;
		for (int i = 0; i < 5; i++)
		{
			bool flag2 = false;
			for (int j = 0; j < aTFieldManager.activeFields.Count; j++)
			{
				Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = aTFieldManager.activeFields[j];
				if (!comp_AbsoluteTerrorField.Active || !intVec.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
				{
					continue;
				}
				bool flag3 = false;
				if (comp_AbsoluteTerrorField.antiTeleport && teleporter != null && teleporter.HostileTo(comp_AbsoluteTerrorField.parent))
				{
					flag3 = true;
				}
				if (flag3)
				{
					comp_AbsoluteTerrorField.SpawnInterceptEffect(intVec.ToVector3Shifted(), 50f);
					Vector3 vector = comp_AbsoluteTerrorField.parent.Position.ToVector3Shifted();
					Vector3 vector2 = (intVec.ToVector3Shifted() - vector).normalized;
					if (vector2 == Vector3.zero)
					{
						vector2 = Vector3.right;
					}
					Vector3 vect = vector + vector2 * (comp_AbsoluteTerrorField.radius + 4f);
					intVec = vect.ToIntVec3().ClampInsideMap(map);
					flag2 = true;
					flag = true;
					break;
				}
			}
			if (!flag2)
			{
				break;
			}
		}
		if (flag)
		{
			IntVec3 intVec2 = CellFinder.StandableCellNear(intVec, map, 5f);
			if (intVec2.IsValid)
			{
				intVec = intVec2;
			}
			Messages.Message("ATField_TeleportIntercepted".Translate(), new TargetInfo(intVec, map), MessageTypeDefOf.NeutralEvent);
		}
		return intVec;
	}
}
