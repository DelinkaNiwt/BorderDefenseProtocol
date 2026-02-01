using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCLWorm;

public class Comp_BattleCrush : ThingComp
{
	private Effecter effecter;

	private bool isInCombat;

	private bool wasInCombatLastTick;

	private int combatCheckCooldown;

	private int damageCooldown;

	private const int CombatCheckInterval = 240;

	private CompProperties_BattleCrush Props => (CompProperties_BattleCrush)props;

	public override void CompTick()
	{
		base.CompTick();
		if (parent is Pawn { Spawned: not false, Dead: false, Downed: false } pawn)
		{
			combatCheckCooldown--;
			if (combatCheckCooldown <= 0)
			{
				combatCheckCooldown = 240;
				isInCombat = CheckCombatState(pawn);
			}
			HandleCombatStateChange(pawn);
			if (isInCombat)
			{
				damageCooldown--;
				if (damageCooldown <= 0)
				{
					damageCooldown = Props.damageInterval;
					ApplyAreaDamage(pawn);
				}
			}
		}
		else
		{
			ClearCombatState();
		}
	}

	private bool CheckCombatState(Pawn pawn)
	{
		Map map = pawn.Map;
		if (map == null)
		{
			return false;
		}
		float num = Props.enemyScanRadius / 1.5f;
		float num2 = num * num;
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if (item != pawn && !item.Dead && !item.Downed && item.HostileTo(pawn) && !item.IsPrisoner)
			{
				float num3 = item.Position.DistanceToSquared(pawn.Position);
				if (num3 <= num2)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void HandleCombatStateChange(Pawn pawn)
	{
		if (isInCombat && !wasInCombatLastTick)
		{
			CreateEffecter(pawn);
		}
		else if (!isInCombat && wasInCombatLastTick)
		{
			CleanupEffecter();
		}
		if (effecter != null)
		{
			effecter.EffectTick(new TargetInfo(pawn.Position, pawn.Map), TargetInfo.Invalid);
		}
		wasInCombatLastTick = isInCombat;
	}

	private void CreateEffecter(Pawn pawn)
	{
		if (Props.battleEffect != null)
		{
			effecter = Props.battleEffect.Spawn();
			effecter.Trigger(new TargetInfo(pawn.Position, pawn.Map), TargetInfo.Invalid);
		}
	}

	private void CleanupEffecter()
	{
		if (effecter != null)
		{
			effecter.Cleanup();
			effecter = null;
		}
	}

	private void ClearCombatState()
	{
		if (isInCombat)
		{
			isInCombat = false;
			CleanupEffecter();
		}
	}

	private void ApplyAreaDamage(Pawn attacker)
	{
		if (effecter != null)
		{
			effecter.Trigger(new TargetInfo(attacker.Position, attacker.Map), TargetInfo.Invalid);
		}
		Map map = attacker.Map;
		IntVec3 position = attacker.Position;
		int damageRadius = Props.damageRadius;
		foreach (IntVec3 item in CellRect.CenteredOn(position, damageRadius))
		{
			if (!item.InBounds(map))
			{
				continue;
			}
			List<Thing> list = map.thingGrid.ThingsListAt(item);
			for (int i = 0; i < list.Count; i++)
			{
				Thing thing = list[i];
				if (thing != attacker && thing is Pawn pawn && pawn.HostileTo(attacker) && !pawn.Downed && !pawn.IsPrisoner)
				{
					ApplyCrushDamage(attacker, pawn);
				}
			}
		}
	}

	private void ApplyCrushDamage(Pawn attacker, Pawn target)
	{
		float num = GetAveragePartHealth(target) * Props.damageFactor;
		float num2 = target.GetStatValue(StatDefOf.IncomingDamageFactor);
		if (num2 <= 0f)
		{
			num2 = 0.01f;
		}
		float f = num / num2;
		DamageInfo dinfo = new DamageInfo(DamageDefOf.Crush, Mathf.Max(1, Mathf.RoundToInt(f)), 2f, -1f, attacker);
		target.TakeDamage(dinfo);
	}

	private float GetAveragePartHealth(Pawn pawn)
	{
		float num = 0f;
		int num2 = 0;
		foreach (BodyPartRecord allPart in pawn.RaceProps.body.AllParts)
		{
			if (allPart.depth == BodyPartDepth.Outside)
			{
				num += allPart.def.GetMaxHealth(pawn);
				num2++;
			}
		}
		return (num2 > 0) ? (num / (float)num2) : 50f;
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		CleanupEffecter();
	}

	public virtual void PostDeSpawn(Map map)
	{
		base.PostDeSpawn(map);
		CleanupEffecter();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref isInCombat, "isInCombat", defaultValue: false);
		Scribe_Values.Look(ref wasInCombatLastTick, "wasInCombatLastTick", defaultValue: false);
		Scribe_Values.Look(ref combatCheckCooldown, "combatCheckCooldown", 0);
		Scribe_Values.Look(ref damageCooldown, "damageCooldown", 0);
	}
}
