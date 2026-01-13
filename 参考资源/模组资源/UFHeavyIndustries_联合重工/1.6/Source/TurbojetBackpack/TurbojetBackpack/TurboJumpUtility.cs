using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TurbojetBackpack;

public static class TurboJumpUtility
{
	private static ThingDef _jumpFlyerDef;

	private static ThingDef _dashFlyerDef;

	private static readonly List<JetDef> DefaultJetsVertical = new List<JetDef>
	{
		new JetDef
		{
			offset = new Vector3(-0.25f, 0f, -0.1f),
			angle = 195f
		},
		new JetDef
		{
			offset = new Vector3(0.25f, 0f, -0.1f),
			angle = 165f
		}
	};

	private static readonly List<JetDef> DefaultJetsHorizontal = new List<JetDef>
	{
		new JetDef
		{
			offset = new Vector3(-0.2f, 0f, -0.1f),
			angle = 180f
		}
	};

	public static ThingDef JumpFlyerDef
	{
		get
		{
			object obj = _jumpFlyerDef;
			if (obj == null)
			{
				obj = DefDatabase<ThingDef>.GetNamedSilentFail("PawnFlyer_Turbojet_Jump") ?? ThingDefOf.PawnFlyer;
				_jumpFlyerDef = (ThingDef)obj;
			}
			return (ThingDef)obj;
		}
	}

	public static ThingDef DashFlyerDef
	{
		get
		{
			object obj = _dashFlyerDef;
			if (obj == null)
			{
				obj = DefDatabase<ThingDef>.GetNamedSilentFail("PawnFlyer_Turbojet_Dash") ?? ThingDefOf.PawnFlyer;
				_dashFlyerDef = (ThingDef)obj;
			}
			return (ThingDef)obj;
		}
	}

	public static void SpawnShadowMote(Pawn pawn, TurbojetExtension extension, float visualOffset)
	{
		if (pawn.Map == null || extension == null || extension.shadowMote == null)
		{
			return;
		}
		Vector3 drawPos = pawn.DrawPos;
		drawPos.z -= visualOffset;
		if (drawPos.ToIntVec3().InBounds(pawn.Map))
		{
			MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(extension.shadowMote);
			if (moteThrown != null)
			{
				moteThrown.Scale = 1f;
				moteThrown.exactPosition = drawPos;
				moteThrown.exactPosition.y = AltitudeLayer.Shadows.AltitudeFor();
				moteThrown.SetVelocity(0f, 0f);
				GenSpawn.Spawn(moteThrown, drawPos.ToIntVec3(), pawn.Map);
			}
		}
	}

	public static void SpawnMoteBurst(Map map, Vector3 center, ThingDef moteDef, int count, float scale, float spread)
	{
		if (map == null || moteDef == null || !center.ToIntVec3().InBounds(map))
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(moteDef);
			if (moteThrown != null)
			{
				moteThrown.Scale = scale * Rand.Range(0.8f, 1.2f);
				moteThrown.exactPosition = center + Gen.RandomHorizontalVector(spread);
				moteThrown.exactPosition.y = moteDef.altitudeLayer.AltitudeFor();
				moteThrown.rotationRate = Rand.Range(-50, 50);
				moteThrown.SetVelocity(Rand.Range(0, 360), Rand.Range(0.2f, 0.5f));
				if (moteThrown.exactPosition.ToIntVec3().InBounds(map))
				{
					GenSpawn.Spawn(moteThrown, moteThrown.exactPosition.ToIntVec3(), map);
				}
			}
		}
	}

	public static void SpawnStandbyMotes(Pawn pawn, TurbojetExtension extension)
	{
		if (pawn.Map == null || extension == null || extension.standbyMote == null)
		{
			return;
		}
		List<JetDef> list = null;
		bool flag = false;
		switch (pawn.Rotation.AsInt)
		{
		case 0:
		case 2:
			list = extension.jetsVertical;
			break;
		case 1:
			list = extension.jetsHorizontal;
			break;
		case 3:
			list = extension.jetsHorizontal;
			flag = true;
			break;
		}
		if (list == null || list.Count == 0)
		{
			return;
		}
		float y = AltitudeLayer.MoteOverhead.AltitudeFor();
		foreach (JetDef item in list)
		{
			Vector3 offset = item.offset;
			if (flag)
			{
				offset.x = 0f - offset.x;
			}
			Vector3 vector = pawn.DrawPos + offset;
			vector.y = y;
			if (vector.ToIntVec3().InBounds(pawn.Map))
			{
				MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(extension.standbyMote);
				if (moteThrown != null)
				{
					moteThrown.Scale = extension.standbyMoteScale.RandomInRange;
					moteThrown.exactPosition = vector;
					moteThrown.rotationRate = Rand.Range(-20, 20);
					moteThrown.SetVelocity(Rand.Range(-10f, 10f), Rand.Range(0.1f, 0.3f));
					GenSpawn.Spawn(moteThrown, vector.ToIntVec3(), pawn.Map);
				}
			}
		}
	}

	public static void SpawnTrailMotes(Map map, Vector3 drawPos, Rot4 rotation, TurbojetExtension extension, ThingDef moteDef, float scale)
	{
		if (map == null || extension == null || moteDef == null)
		{
			return;
		}
		List<JetDef> list = null;
		bool flag = false;
		switch (rotation.AsInt)
		{
		case 0:
		case 2:
			list = extension.jetsVertical;
			break;
		case 1:
			list = extension.jetsHorizontal;
			break;
		case 3:
			list = extension.jetsHorizontal;
			flag = true;
			break;
		}
		if (list == null || list.Count == 0)
		{
			return;
		}
		float y = AltitudeLayer.Pawn.AltitudeFor() - 0.05f;
		foreach (JetDef item in list)
		{
			Vector3 offset = item.offset;
			if (flag)
			{
				offset.x = 0f - offset.x;
			}
			Vector3 vector = drawPos + offset;
			vector.y = y;
			if (vector.ToIntVec3().InBounds(map))
			{
				MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(moteDef);
				if (moteThrown != null)
				{
					moteThrown.Scale = scale * Rand.Range(0.8f, 1.2f);
					moteThrown.exactPosition = vector;
					moteThrown.rotationRate = Rand.Range(-50, 50);
					moteThrown.SetVelocity(Rand.Range(0, 360), Rand.Range(0.2f, 0.5f));
					GenSpawn.Spawn(moteThrown, vector.ToIntVec3(), map);
				}
			}
		}
	}

	public static void DoWarmupBurst(Pawn pawn, Ability ability)
	{
		if (pawn.apparel == null)
		{
			return;
		}
		TurbojetAbilityExtension turbojetAbilityExtension = ability?.def.GetModExtension<TurbojetAbilityExtension>();
		if (turbojetAbilityExtension != null && turbojetAbilityExtension.moteDef != null)
		{
			if (turbojetAbilityExtension.warmupSound != null)
			{
				turbojetAbilityExtension.warmupSound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
			}
			SpawnMoteBurst(pawn.Map, pawn.DrawPos, turbojetAbilityExtension.moteDef, turbojetAbilityExtension.moteCount, turbojetAbilityExtension.moteScale, turbojetAbilityExtension.moteSpread);
		}
	}

	public static void SpawnThrustMotes(Map map, Vector3 drawPos, Rot4 rotation, TurbojetExtension extension, int tickCounter, List<MoteSettings> overrideEffects = null)
	{
		if (map == null || extension == null)
		{
			return;
		}
		List<MoteSettings> list = overrideEffects ?? extension.thrustMoteEffects;
		if (list.NullOrEmpty())
		{
			return;
		}
		List<JetDef> list2 = null;
		bool flag = false;
		switch (rotation.AsInt)
		{
		case 0:
		case 2:
			list2 = extension.jetsVertical;
			break;
		case 1:
			list2 = extension.jetsHorizontal;
			break;
		case 3:
			list2 = extension.jetsHorizontal;
			flag = true;
			break;
		}
		if (list2 == null || list2.Count == 0)
		{
			list2 = ((!rotation.IsHorizontal) ? DefaultJetsVertical : DefaultJetsHorizontal);
		}
		float y = AltitudeLayer.Pawn.AltitudeFor() - 0.05f;
		foreach (MoteSettings item in list)
		{
			if (tickCounter % item.interval != 0 || item.moteDef == null)
			{
				continue;
			}
			foreach (JetDef item2 in list2)
			{
				Vector3 offset = item2.offset;
				Vector3 offset2 = item.offset;
				float num = item2.angle;
				float num2 = item.angleOffset;
				if (flag)
				{
					offset.x = 0f - offset.x;
					offset2.x = 0f - offset2.x;
					num = 360f - num;
					num2 = 0f - num2;
				}
				Vector3 vector = drawPos + offset + offset2;
				vector.y = y;
				if (!vector.ToIntVec3().InBounds(map) || !typeof(MoteThrown).IsAssignableFrom(item.moteDef.thingClass))
				{
					continue;
				}
				MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(item.moteDef);
				if (moteThrown != null)
				{
					moteThrown.Scale = item.scale.RandomInRange;
					moteThrown.exactPosition = vector;
					if (item.color.HasValue)
					{
						moteThrown.instanceColor = item.color.Value;
					}
					float angle = (moteThrown.exactRotation = num + num2 + Rand.Range(0f - item.spread, item.spread));
					float randomInRange = item.speed.RandomInRange;
					moteThrown.SetVelocity(angle, randomInRange);
					IntVec3 intVec = vector.ToIntVec3();
					if (intVec.InBounds(map))
					{
						GenSpawn.Spawn(moteThrown, intVec, map);
					}
				}
			}
		}
	}

	public static bool IsValidTargetBase(Map map, IntVec3 cell)
	{
		if (!cell.IsValid || !cell.InBounds(map) || cell.Impassable(map) || cell.Fogged(map) || !cell.WalkableByAny(map))
		{
			return false;
		}
		Building edifice = cell.GetEdifice(map);
		if (edifice is Building_Door { Open: false })
		{
			return false;
		}
		return true;
	}

	public static bool SpawnFlyer(Pawn pawn, IntVec3 dest, VerbProperties verbProps, Ability ability, bool isDash)
	{
		Map map = pawn.Map;
		TurbojetExtension turbojetExtension = null;
		bool combatMode = false;
		CompTurbojetFlight compTurbojetFlight = null;
		if (pawn.apparel != null)
		{
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				TurbojetExtension modExtension = item.def.GetModExtension<TurbojetExtension>();
				if (modExtension != null)
				{
					turbojetExtension = modExtension;
				}
				CompTurbojetFlight comp = item.GetComp<CompTurbojetFlight>();
				if (comp != null)
				{
					compTurbojetFlight = comp;
					combatMode = comp.IsCombatMode;
				}
			}
		}
		float startHeight = 0f;
		float targetHeight = 0f;
		if (compTurbojetFlight != null)
		{
			startHeight = compTurbojetFlight.CurrentHeight;
			targetHeight = ((!compTurbojetFlight.ShouldBeFlying) ? 0f : (turbojetExtension?.flightHeight ?? 1.2f));
		}
		ThingDef flyingDef = (isDash ? DashFlyerDef : JumpFlyerDef);
		PawnFlyer pawnFlyer = PawnFlyer.MakeFlyer(flyingDef, pawn, dest, verbProps.flightEffecterDef, verbProps.soundLanding, verbProps.flyWithCarriedThing, null, ability);
		if (pawnFlyer != null)
		{
			if (pawnFlyer is PawnFlyer_Turbojet pawnFlyer_Turbojet)
			{
				pawnFlyer_Turbojet.combatMode = combatMode;
				if (turbojetExtension != null)
				{
					pawnFlyer_Turbojet.apparelExtension = turbojetExtension;
				}
				pawnFlyer_Turbojet.startHeight = startHeight;
				pawnFlyer_Turbojet.targetHeight = targetHeight;
				TurbojetAbilityExtension turbojetAbilityExtension = ability?.def.GetModExtension<TurbojetAbilityExtension>();
				if (turbojetAbilityExtension != null)
				{
					pawnFlyer_Turbojet.effectiveMoteDef = turbojetAbilityExtension.moteDef;
					pawnFlyer_Turbojet.effectiveMoteCount = turbojetAbilityExtension.moteCount;
					pawnFlyer_Turbojet.effectiveMoteScale = turbojetAbilityExtension.moteScale;
					pawnFlyer_Turbojet.effectiveMoteSpread = ((turbojetAbilityExtension.moteSpread > 0f) ? turbojetAbilityExtension.moteSpread : 0.5f);
				}
			}
			GenSpawn.Spawn(pawnFlyer, dest, map);
			return true;
		}
		return false;
	}

	public static void OrderJump(Pawn pawn, LocalTargetInfo target, Verb verb, float range)
	{
		if (!pawn.Map.roofGrid.Roofed(pawn.Position))
		{
			IntVec3 intVec = RCellFinder.BestOrderedGotoDestNear(target.Cell, pawn, (IntVec3 c) => IsValidTargetBase(pawn.Map, c) && !pawn.Map.roofGrid.Roofed(c) && (float)c.DistanceToSquared(pawn.Position) <= range * range, reachable: false);
			if (intVec.IsValid)
			{
				Job job = JobMaker.MakeJob(JobDefOf.CastJump, intVec);
				job.verbToUse = verb;
				pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				FleckMaker.Static(intVec, pawn.Map, FleckDefOf.FeedbackGoto);
			}
		}
	}
}
