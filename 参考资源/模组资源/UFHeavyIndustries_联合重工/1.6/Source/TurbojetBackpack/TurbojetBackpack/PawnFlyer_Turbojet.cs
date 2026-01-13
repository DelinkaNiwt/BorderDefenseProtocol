using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TurbojetBackpack;

public class PawnFlyer_Turbojet : PawnFlyer
{
	protected Vector3 lastDrawPos = Vector3.zero;

	public bool combatMode = false;

	public TurbojetExtension cachedExtension;

	public TurbojetExtension apparelExtension;

	public float effectiveMoteScale = 1f;

	public int effectiveMoteCount = 0;

	public ThingDef effectiveMoteDef = null;

	public float effectiveMoteSpread = 0.5f;

	public float startHeight = 0f;

	public float targetHeight = 0f;

	private static EffecterDef _defaultWarmup;

	public static EffecterDef DefaultWarmup => _defaultWarmup ?? (_defaultWarmup = DefDatabase<EffecterDef>.GetNamedSilentFail("TurbojetWarmupEffect"));

	public override Vector3 DrawPos
	{
		get
		{
			Vector3 drawPos = base.DrawPos;
			float num = Mathf.Max(1, ticksFlightTime);
			float t = Mathf.Clamp01((float)ticksFlying / num);
			float num2 = Mathf.Lerp(startHeight, targetHeight, t);
			drawPos.z += num2;
			return drawPos;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		lastDrawPos = DrawPos;
		if (cachedExtension == null)
		{
			cachedExtension = def.GetModExtension<TurbojetExtension>();
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref lastDrawPos, "lastDrawPos", Vector3.zero);
		Scribe_Values.Look(ref combatMode, "combatMode", defaultValue: false);
		Scribe_Values.Look(ref startHeight, "startHeight", 0f);
		Scribe_Values.Look(ref targetHeight, "targetHeight", 0f);
		Scribe_Values.Look(ref effectiveMoteScale, "effectiveMoteScale", 1f);
		Scribe_Values.Look(ref effectiveMoteCount, "effectiveMoteCount", 0);
		Scribe_Defs.Look(ref effectiveMoteDef, "effectiveMoteDef");
		Scribe_Values.Look(ref effectiveMoteSpread, "effectiveMoteSpread", 0.5f);
	}

	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
	{
		if (base.FlyingPawn != null)
		{
			base.FlyingPawn.DynamicDrawPhaseAt(phase, DrawPos);
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		DrawShadow(DrawPos, (base.FlyingPawn != null) ? base.FlyingPawn.BodySize : 1f);
		if (base.CarriedThing != null && base.FlyingPawn != null)
		{
			PawnRenderUtility.DrawCarriedThing(base.FlyingPawn, DrawPos, base.CarriedThing);
		}
	}

	private void DrawShadow(Vector3 drawLoc, float size)
	{
		if (!(def.pawnFlyer.ShadowMaterial == null))
		{
			float num = Mathf.Max(1, ticksFlightTime);
			float num2 = (float)ticksFlying / num;
			Vector3 pos = Vector3.Lerp(startVec, base.DestinationPos, num2);
			pos.y = AltitudeLayer.Shadows.AltitudeFor();
			float num3 = 1f - Mathf.Sin(num2 * (float)Math.PI) * 0.3f;
			Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(size * num3, 1f, size * num3), pos: pos, q: Quaternion.identity);
			Graphics.DrawMesh(MeshPool.plane10, matrix, def.pawnFlyer.ShadowMaterial, 0);
		}
	}

	protected override void RespawnPawn()
	{
		Pawn flyingPawn = base.FlyingPawn;
		Vector3 center = base.Position.ToVector3Shifted();
		Map map = base.Map;
		base.RespawnPawn();
		if (flyingPawn == null || !flyingPawn.Spawned)
		{
			return;
		}
		SyncPawnHeight(flyingPawn);
		EffecterDef effecterDef = cachedExtension?.landingEffecter;
		if (effecterDef != null)
		{
			foreach (SubEffecterDef child in effecterDef.children)
			{
				if (child.soundDef != null)
				{
					child.soundDef.PlayOneShot(new TargetInfo(flyingPawn.Position, map));
				}
			}
		}
		if (effectiveMoteDef != null && effectiveMoteCount > 0)
		{
			TurboJumpUtility.SpawnMoteBurst(map, center, effectiveMoteDef, effectiveMoteCount, effectiveMoteScale, effectiveMoteSpread);
		}
		if (combatMode)
		{
			DoLandingImpact(flyingPawn);
		}
	}

	private void SyncPawnHeight(Pawn p)
	{
		if (p.apparel == null)
		{
			return;
		}
		foreach (Apparel item in p.apparel.WornApparel)
		{
			CompTurbojetFlight comp = item.GetComp<CompTurbojetFlight>();
			if (comp != null)
			{
				comp.ForceSetHeight(targetHeight);
				break;
			}
		}
	}

	private void DoLandingImpact(Pawn p)
	{
		float radius = cachedExtension?.landingDamageRadius ?? 3.9f;
		int num = cachedExtension?.damageAmount ?? 12;
		int num2 = cachedExtension?.stunAmount ?? 20;
		float num3 = cachedExtension?.pushDistance ?? 0f;
		SoundDef explosionSound = cachedExtension?.landingSound ?? DefDatabase<SoundDef>.GetNamedSilentFail("Explosion_Stun");
		Map map = p.Map;
		IntVec3 position = p.Position;
		List<Thing> list = new List<Thing> { p };
		foreach (Thing item in GenRadial.RadialDistinctThingsAround(position, map, radius, useCenter: true))
		{
			if (!(item is Pawn pawn) || pawn == p)
			{
				continue;
			}
			bool flag = pawn.HostileTo(p);
			bool flag2 = pawn.RaceProps.Animal && pawn.Faction == null;
			if (flag || flag2)
			{
				if (GenSight.LineOfSight(position, pawn.Position, map))
				{
					if (num > 0)
					{
						pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, num, 0f, -1f, p));
					}
					if (num2 > 0)
					{
						pawn.TakeDamage(new DamageInfo(DamageDefOf.Stun, num2, 0f, -1f, p));
					}
					ApplyMeleeImpact(p, pawn);
					if (num3 > 0f)
					{
						PerformPush(pawn, position, num3);
					}
					list.Add(pawn);
				}
			}
			else
			{
				list.Add(pawn);
			}
		}
		DamageDef blunt = DamageDefOf.Blunt;
		List<Thing> ignoredThings = list;
		GenExplosion.DoExplosion(position, map, radius, blunt, p, num, 0.5f, explosionSound, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, ignoredThings);
	}

	protected void ApplyMeleeImpact(Pawn attacker, Pawn victim)
	{
		if (attacker == null || victim == null)
		{
			return;
		}
		if (attacker.def.tools != null && attacker.def.tools.Count > 0)
		{
			Tool tool = attacker.def.tools.MaxBy((Tool t) => t.power);
			if (tool != null)
			{
				ApplyToolDamage(attacker, victim, tool, null);
			}
		}
		ThingWithComps thingWithComps = attacker.equipment?.Primary;
		if (thingWithComps != null && thingWithComps.def.IsMeleeWeapon && thingWithComps.def.tools != null && thingWithComps.def.tools.Count > 0)
		{
			Tool tool2 = thingWithComps.def.tools.MaxBy((Tool t) => t.power);
			if (tool2 != null)
			{
				ApplyToolDamage(attacker, victim, tool2, thingWithComps);
			}
		}
	}

	private void ApplyToolDamage(Pawn attacker, Pawn victim, Tool tool, Thing weapon)
	{
		float num = tool.power * attacker.GetStatValue(StatDefOf.MeleeDamageFactor);
		float num2 = tool.armorPenetration;
		if (num2 < 0f)
		{
			num2 = num * 0.015f;
		}
		DamageDef damageDef = DamageDefOf.Blunt;
		if (tool.capacities != null && tool.capacities.Count > 0)
		{
			ToolCapacityDef cap = tool.capacities[0];
			ManeuverDef maneuverDef = DefDatabase<ManeuverDef>.AllDefsListForReading.Find((ManeuverDef m) => m.requiredCapacity == cap);
			if (maneuverDef != null && maneuverDef.verb != null)
			{
				damageDef = maneuverDef.verb.meleeDamageDef ?? DamageDefOf.Blunt;
			}
		}
		DamageInfo dinfo = new DamageInfo(damageDef, num, num2, -1f, attacker, null, weapon?.def);
		dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
		victim.TakeDamage(dinfo);
	}

	protected void PerformPush(Pawn victim, IntVec3 center, float dist)
	{
		if (victim.Map == null)
		{
			return;
		}
		Vector3 vector = (victim.Position - center).ToVector3().normalized;
		if (vector == Vector3.zero)
		{
			vector = Gen.RandomHorizontalVector(1f);
		}
		IntVec3 intVec = victim.Position;
		Vector3 vector2 = victim.Position.ToVector3Shifted();
		for (int i = 1; i <= (int)dist; i++)
		{
			Vector3 vect = vector2 + vector * i;
			IntVec3 intVec2 = vect.ToIntVec3();
			if (!intVec2.InBounds(victim.Map) || !intVec2.Walkable(victim.Map))
			{
				break;
			}
			intVec = intVec2;
		}
		if (intVec != victim.Position)
		{
			victim.Position = intVec;
			victim.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
			FleckMaker.ThrowDustPuff(victim.DrawPos, victim.Map, 1f);
		}
	}

	protected override void Tick()
	{
		base.Tick();
		if (base.Spawned && base.Map != null)
		{
			lastDrawPos = DrawPos;
			TickMotes();
		}
	}

	protected virtual void TickMotes()
	{
		if (apparelExtension != null && base.FlyingPawn != null)
		{
			List<MoteSettings> overrideEffects = apparelExtension.jumpMoteEffects ?? apparelExtension.thrustMoteEffects;
			TurboJumpUtility.SpawnThrustMotes(base.Map, DrawPos, base.FlyingPawn.Rotation, apparelExtension, ticksFlying, overrideEffects);
		}
	}

	protected virtual void InterpolateFlecks(Vector3 start, Vector3 end)
	{
	}
}
