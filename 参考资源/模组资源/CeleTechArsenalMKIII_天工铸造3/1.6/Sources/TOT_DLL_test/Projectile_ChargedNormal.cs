using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Projectile_ChargedNormal : Bullet
{
	private int tickcount;

	public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue_Small");

	public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue_LongLasting_Small");

	public int Fleck_MakeFleckTickMax = 1;

	public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Scale = new FloatRange(1.6f, 1.7f);

	public FloatRange Fleck_Speed = new FloatRange(5f, 7f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	public Quaternion rotation;

	private static List<IntVec3> checkedCells = new List<IntVec3>();

	public override bool AnimalsFleeImpact => true;

	private Vector3 CurretPos(float t)
	{
		return origin + (destination - origin) * t;
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		Vector3 vector = CurretPos(base.DistanceCoveredFraction - 0.01f);
		position = CurretPos(base.DistanceCoveredFraction);
		rotation = Quaternion.LookRotation(position - vector);
		if (tickcount >= 4)
		{
			Vector3 position2 = position;
			position2.y = AltitudeLayer.Projectile.AltitudeFor();
			Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position2, rotation, DrawMat, 0);
			Comps_PostDraw();
		}
	}

	protected override void Tick()
	{
		tickcount++;
		base.Tick();
	}

	private bool CheckForFreeIntercept(IntVec3 c)
	{
		if (destination.ToIntVec3() == c)
		{
			return false;
		}
		float num = VerbUtility.InterceptChanceFactorFromDistance(origin, c);
		if (num <= 0f)
		{
			return false;
		}
		bool flag = false;
		List<Thing> thingList = c.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
			Thing thing = thingList[i];
			if (!CanHit(thing))
			{
				continue;
			}
			bool flag2 = false;
			if (thing.def.Fillage == FillCategory.Full)
			{
				if (!(thing is Building_Door { Open: not false }))
				{
					ThrowDebugText("int-wall", c);
					Impact(thing);
					return true;
				}
				flag2 = true;
			}
			float num2 = 0f;
			if (thing is Pawn pawn)
			{
				num2 = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
				if (pawn.GetPosture() != PawnPosture.Standing)
				{
					num2 *= 0.1f;
				}
				if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
				{
					if (preventFriendlyFire)
					{
						num2 = 0f;
						ThrowDebugText("ff-miss", c);
					}
					else
					{
						num2 *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
					}
				}
			}
			else if (thing.def.fillPercent > 0.2f)
			{
				num2 = (flag2 ? 0.05f : ((!base.DestinationCell.AdjacentTo8Way(c)) ? (thing.def.fillPercent * 0.15f) : (thing.def.fillPercent * 1f)));
			}
			num2 *= num;
			if (num2 > 1E-05f)
			{
				if (Rand.Chance(num2))
				{
					ThrowDebugText("int-" + num2.ToStringPercent(), c);
					Impact(thing);
					return true;
				}
				flag = true;
				ThrowDebugText(num2.ToStringPercent(), c);
			}
		}
		if (!flag)
		{
			ThrowDebugText("o", c);
		}
		return false;
	}

	private void ThrowDebugText(string text, IntVec3 c)
	{
		if (DebugViewSettings.drawShooting)
		{
			MoteMaker.ThrowText(c.ToVector3Shifted(), base.Map, text);
		}
	}

	public bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
	{
		if (lastExactPos == newExactPos)
		{
			return false;
		}
		List<Thing> list = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].TryGetComp<CompProjectileInterceptor>().CheckIntercept(this, lastExactPos, newExactPos))
			{
				Impact(null, blockedByShield: true);
				return true;
			}
		}
		IntVec3 intVec = lastExactPos.ToIntVec3();
		IntVec3 intVec2 = newExactPos.ToIntVec3();
		if (intVec2 == intVec)
		{
			return false;
		}
		if (!intVec.InBounds(base.Map) || !intVec2.InBounds(base.Map))
		{
			return false;
		}
		if (intVec2.AdjacentToCardinal(intVec))
		{
			return CheckForFreeIntercept(intVec2);
		}
		if (VerbUtility.InterceptChanceFactorFromDistance(origin, intVec2) <= 0f)
		{
			return false;
		}
		Vector3 vect = lastExactPos;
		Vector3 v = newExactPos - lastExactPos;
		Vector3 vector = v.normalized * 0.2f;
		int num = (int)(v.MagnitudeHorizontal() / 0.2f);
		checkedCells.Clear();
		int num2 = 0;
		while (true)
		{
			vect += vector;
			IntVec3 intVec3 = vect.ToIntVec3();
			if (!checkedCells.Contains(intVec3))
			{
				if (CheckForFreeIntercept(intVec3))
				{
					break;
				}
				checkedCells.Add(intVec3);
			}
			num2++;
			if (num2 > num)
			{
				return false;
			}
			if (intVec3 == intVec2)
			{
				return false;
			}
		}
		return true;
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		IntVec3 position = base.Position;
		base.Impact(hitThing, blockedByShield);
		BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
		Find.BattleLog.Add(battleLogEntry_RangedImpact);
		NotifyImpact(hitThing, map, position);
		if (hitThing != null)
		{
			bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
			Pawn pawn2 = hitThing as Pawn;
			float num = DamageAmount;
			if (Rand.Chance(0.05f) && launcher != null && launcher.Faction != null && launcher.Faction == Faction.OfPlayer)
			{
				num = (float)DamageAmount * 1.98f;
			}
			if (pawn2 != null && pawn2.RaceProps.IsMechanoid)
			{
				num *= 1.5f;
				if (Rand.Chance(0.2f))
				{
					DamageInfo dinfo = new DamageInfo(DamageDefOf.EMP, DamageAmount, 2f, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
					hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
				}
			}
			DamageInfo dinfo2 = new DamageInfo(def.projectile.damageDef, num, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
			hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
			if (pawn2 != null && pawn2.stances != null)
			{
				pawn2.stances.stagger.Notify_BulletImpact(this);
			}
			if (def.projectile.extraDamages == null)
			{
				return;
			}
			{
				foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
				{
					if (Rand.Chance(extraDamage.chance))
					{
						DamageInfo dinfo3 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
						hitThing.TakeDamage(dinfo3).AssociateWithLog(battleLogEntry_RangedImpact);
					}
				}
				return;
			}
		}
		if (!blockedByShield)
		{
			SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map));
			if (base.Position.GetTerrain(map).takeSplashes)
			{
				FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(DamageAmount) * 1f, 4f);
			}
			else
			{
				FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
			}
		}
	}

	private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
	{
		BulletImpactData impactData = new BulletImpactData
		{
			bullet = this,
			hitThing = hitThing,
			impactPosition = position
		};
		hitThing?.Notify_BulletImpactNearby(impactData);
		int num = 9;
		for (int i = 0; i < num; i++)
		{
			IntVec3 c = position + GenRadial.RadialPattern[i];
			if (!c.InBounds(map))
			{
				continue;
			}
			List<Thing> thingList = c.GetThingList(map);
			for (int j = 0; j < thingList.Count; j++)
			{
				if (thingList[j] != hitThing)
				{
					thingList[j].Notify_BulletImpactNearby(impactData);
				}
			}
		}
	}
}
